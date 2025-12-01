using Microsoft.AspNetCore.Mvc;
using SolisApi.Data;
using Microsoft.EntityFrameworkCore;

namespace SolisApi.Controllers;

/// <summary>
/// Controller público para verificação de tenants
/// </summary>
[ApiController]
[Route("api/tenants")]
public class TenantsController : ControllerBase
{
    private readonly SolisDbContext _context;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(SolisDbContext context, ILogger<TenantsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Verifica se um tenant existe pelo subdomain
    /// Endpoint público (sem autenticação)
    /// </summary>
    /// <param name="subdomain">Subdomain do tenant (ex: demo, acme)</param>
    /// <returns>Status do tenant</returns>
    [HttpGet("check/{subdomain}")]
    public async Task<IActionResult> CheckTenant(string subdomain)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(subdomain))
            {
                return BadRequest(new { 
                    exists = false, 
                    error = "Subdomain é obrigatório" 
                });
            }

            // Normalizar subdomain (lowercase, trim)
            subdomain = subdomain.Trim().ToLowerInvariant();

            // Verificar se tenant existe e está ativo
            var tenant = await _context.Tenants
                .Where(t => t.Subdomain == subdomain)
                .Select(t => new { 
                    t.Subdomain, 
                    t.TradeName, 
                    t.Active 
                })
                .FirstOrDefaultAsync();

            if (tenant == null)
            {
                _logger.LogInformation("Tenant check: subdomain '{Subdomain}' não encontrado", subdomain);
                return Ok(new { 
                    exists = false,
                    subdomain = subdomain
                });
            }

            if (!tenant.Active)
            {
                _logger.LogInformation("Tenant check: subdomain '{Subdomain}' existe mas está inativo", subdomain);
                return Ok(new { 
                    exists = true,
                    active = false,
                    subdomain = tenant.Subdomain,
                    tradeName = tenant.TradeName
                });
            }

            _logger.LogInformation("Tenant check: subdomain '{Subdomain}' encontrado e ativo", subdomain);
            return Ok(new { 
                exists = true,
                active = true,
                subdomain = tenant.Subdomain,
                tradeName = tenant.TradeName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar tenant {Subdomain}", subdomain);
            return StatusCode(500, new { 
                exists = false, 
                error = "Erro ao verificar tenant" 
            });
        }
    }
}
