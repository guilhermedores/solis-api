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
    private readonly ILogger<CashRegisterController> _logger;

    public CashRegisterController(ICashRegisterService service, ILogger<CashRegisterController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private string? GetTenantSubdomain() => HttpContext.Items["TenantSubdomain"]?.ToString();

    /// <summary>Abre um novo caixa</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CashRegisterResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CashRegisterResponse>> Open([FromBody] OpenCashRegisterRequest request)
    {
        try
        {
            var tenant = GetTenantSubdomain();
            if (string.IsNullOrEmpty(tenant)) return Unauthorized(new { error = "Tenant not found" });

            var result = await _service.OpenAsync(tenant, request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao abrir caixa");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>Sync completo do agente (upsert idempotente)</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CashRegisterResponse>> SyncFromAgent(Guid id, [FromBody] SyncCashRegisterRequest request)
    {
        try
        {
            var tenant = GetTenantSubdomain();
            if (string.IsNullOrEmpty(tenant)) return Unauthorized(new { error = "Tenant not found" });

            var result = await _service.SyncFromAgentAsync(tenant, id, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sincronizar caixa {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
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
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar caixas");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>Obtém caixa por ID com movimentações</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CashRegisterResponse>> GetById(Guid id)
    {
        try
        {
            var tenant = GetTenantSubdomain();
            if (string.IsNullOrEmpty(tenant)) return Unauthorized(new { error = "Tenant not found" });

            var result = await _service.GetByIdAsync(tenant, id);
            if (result == null) return NotFound(new { error = $"Caixa {id} não encontrado" });
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter caixa {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>Fecha um caixa</summary>
    [HttpPost("{id:guid}/fechar")]
    public async Task<ActionResult<CashRegisterResponse>> Close(Guid id, [FromBody] CloseCashRegisterRequest request)
    {
        try
        {
            var tenant = GetTenantSubdomain();
            if (string.IsNullOrEmpty(tenant)) return Unauthorized(new { error = "Tenant not found" });

            var result = await _service.CloseAsync(tenant, id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(new { error = $"Caixa {id} não encontrado" }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fechar caixa {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>Registra uma sangria</summary>
    [HttpPost("{id:guid}/sangria")]
    public async Task<ActionResult<CashRegisterResponse>> Sangria(Guid id, [FromBody] MovementRequest request)
    {
        try
        {
            var tenant = GetTenantSubdomain();
            if (string.IsNullOrEmpty(tenant)) return Unauthorized(new { error = "Tenant not found" });

            var result = await _service.RegisterSangriaAsync(tenant, id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(new { error = $"Caixa {id} não encontrado" }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar sangria no caixa {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>Registra um suprimento</summary>
    [HttpPost("{id:guid}/suprimento")]
    public async Task<ActionResult<CashRegisterResponse>> Suprimento(Guid id, [FromBody] MovementRequest request)
    {
        try
        {
            var tenant = GetTenantSubdomain();
            if (string.IsNullOrEmpty(tenant)) return Unauthorized(new { error = "Tenant not found" });

            var result = await _service.RegisterSuprimentoAsync(tenant, id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException) { return NotFound(new { error = $"Caixa {id} não encontrado" }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar suprimento no caixa {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>Lista movimentações de um caixa</summary>
    [HttpGet("{id:guid}/movimentacoes")]
    public async Task<ActionResult<List<CashRegisterMovementDto>>> GetMovements(Guid id)
    {
        try
        {
            var tenant = GetTenantSubdomain();
            if (string.IsNullOrEmpty(tenant)) return Unauthorized(new { error = "Tenant not found" });

            var result = await _service.GetMovementsAsync(tenant, id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter movimentações do caixa {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
