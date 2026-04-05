using Microsoft.AspNetCore.Mvc;
using SolisApi.DTOs;
using SolisApi.Middleware;
using SolisApi.Services;

namespace SolisApi.Controllers;

[ApiController]
[Route("api/caixas")]
[RequireAuth]
public class CashRegisterController : ControllerBase
{
    private readonly ICashRegisterService _service;

    public CashRegisterController(ICashRegisterService service)
    {
        _service = service;
    }

    private string? GetTenantSubdomain() => HttpContext.Items["TenantSubdomain"]?.ToString();

    /// <summary>Abre um novo caixa</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CashRegisterResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CashRegisterResponse>> Open([FromBody] OpenCashRegisterRequest request)
    {
        var tenant = GetTenantSubdomain();
        if (string.IsNullOrEmpty(tenant)) return Unauthorized(new { error = "Tenant not found" });

        try
        {
            var result = await _service.OpenAsync(tenant, request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Sync completo do agente (upsert idempotente)</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CashRegisterResponse>> SyncFromAgent(Guid id, [FromBody] SyncCashRegisterRequest request)
    {
        var tenant = GetTenantSubdomain();
        if (string.IsNullOrEmpty(tenant)) return Unauthorized(new { error = "Tenant not found" });

        var result = await _service.SyncFromAgentAsync(tenant, id, request);
        return Ok(result);
    }

    /// <summary>Lista caixas com filtros</summary>
    [HttpGet]
    public async Task<ActionResult> List(
        [FromQuery] Guid? storeId,
        [FromQuery] int? terminalNumber,
        [FromQuery] string? status,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var tenant = GetTenantSubdomain();
        if (string.IsNullOrEmpty(tenant)) return Unauthorized(new { error = "Tenant not found" });

        var (items, total) = await _service.ListAsync(tenant, storeId, terminalNumber, status, dateFrom, dateTo, page, pageSize);
        return Ok(new
        {
            data = items,
            pagination = new { page, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }
        });
    }

    /// <summary>Obtém caixa por ID com movimentações</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CashRegisterResponse>> GetById(Guid id)
    {
        var tenant = GetTenantSubdomain();
        if (string.IsNullOrEmpty(tenant)) return Unauthorized(new { error = "Tenant not found" });

        var result = await _service.GetByIdAsync(tenant, id);
        if (result == null) return NotFound(new { error = $"Caixa {id} não encontrado" });
        return Ok(result);
    }

    /// <summary>Fecha um caixa</summary>
    [HttpPost("{id:guid}/fechar")]
    public async Task<ActionResult<CashRegisterResponse>> Close(Guid id, [FromBody] CloseCashRegisterRequest request)
    {
        var tenant = GetTenantSubdomain();
        if (string.IsNullOrEmpty(tenant)) return Unauthorized(new { error = "Tenant not found" });

        try
        {
            var result = await _service.CloseAsync(tenant, id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(new { error = $"Caixa {id} não encontrado" }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>Registra uma sangria</summary>
    [HttpPost("{id:guid}/sangria")]
    public async Task<ActionResult<CashRegisterResponse>> Sangria(Guid id, [FromBody] MovementRequest request)
    {
        var tenant = GetTenantSubdomain();
        if (string.IsNullOrEmpty(tenant)) return Unauthorized(new { error = "Tenant not found" });

        try
        {
            var result = await _service.RegisterSangriaAsync(tenant, id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(new { error = $"Caixa {id} não encontrado" }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>Registra um suprimento</summary>
    [HttpPost("{id:guid}/suprimento")]
    public async Task<ActionResult<CashRegisterResponse>> Suprimento(Guid id, [FromBody] MovementRequest request)
    {
        var tenant = GetTenantSubdomain();
        if (string.IsNullOrEmpty(tenant)) return Unauthorized(new { error = "Tenant not found" });

        try
        {
            var result = await _service.RegisterSuprimentoAsync(tenant, id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(new { error = $"Caixa {id} não encontrado" }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>Lista movimentações de um caixa</summary>
    [HttpGet("{id:guid}/movimentacoes")]
    public async Task<ActionResult<List<CashRegisterMovementDto>>> GetMovements(Guid id)
    {
        var tenant = GetTenantSubdomain();
        if (string.IsNullOrEmpty(tenant)) return Unauthorized(new { error = "Tenant not found" });

        var result = await _service.GetMovementsAsync(tenant, id);
        return Ok(result);
    }
}
