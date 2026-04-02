using Microsoft.AspNetCore.Mvc;
using SolisApi.DTOs;
using SolisApi.Middleware;
using SolisApi.Services;

namespace SolisApi.Controllers;

[ApiController]
[Route("api/operadores")]
[RequireAuth]
public class OperadoresController : ControllerBase
{
    private readonly IOperadorService _service;
    private readonly ILogger<OperadoresController> _logger;

    public OperadoresController(IOperadorService service, ILogger<OperadoresController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private string? GetTenantSubdomain() => HttpContext.Items["TenantSubdomain"]?.ToString();

    private bool IsAgentToken() =>
        HttpContext.User.FindFirst("type")?.Value == "agent";

    /// <summary>
    /// Lista operadores para sync do agente — inclui PinHash.
    /// Requer agent token (type=agent).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<OperadorSyncItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<OperadorSyncItem>>> ListForSync(CancellationToken ct)
    {
        if (!IsAgentToken())
            return StatusCode(403, new { error = "Endpoint exclusivo para agent tokens" });

        var tenant = GetTenantSubdomain();
        if (string.IsNullOrEmpty(tenant))
            return Unauthorized(new { error = "Tenant not found" });

        try
        {
            var items = await _service.ListForSyncAsync(tenant, ct);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing operators for sync, tenant={Tenant}", tenant);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Fallback online: agente valida credenciais do operador quando não há dados locais.
    /// Requer agent token (type=agent).
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(PinLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PinLoginResponse>> LoginByPin([FromBody] PinLoginRequest request, CancellationToken ct)
    {
        if (!IsAgentToken())
            return StatusCode(403, new { error = "Endpoint exclusivo para agent tokens" });

        var tenant = GetTenantSubdomain();
        if (string.IsNullOrEmpty(tenant))
            return Unauthorized(new { error = "Tenant not found" });

        try
        {
            var result = await _service.LoginByPinAsync(tenant, request, ct);
            if (result == null)
                return Unauthorized(new { error = "Operador não encontrado ou PIN inválido" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error on PIN login for operator {OperatorNumber}, tenant={Tenant}", request.OperatorNumber, tenant);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
