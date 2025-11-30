using Microsoft.AspNetCore.Mvc;
using SolisApi.Middleware;
using SolisApi.Services;

namespace SolisApi.Controllers;

[ApiController]
[Route("api/entities")]
[RequireAuth]
public class EntitiesController : ControllerBase
{
    private readonly DynamicCrudService _dynamicCrudService;
    private readonly ILogger<EntitiesController> _logger;

    public EntitiesController(
        DynamicCrudService dynamicCrudService,
        ILogger<EntitiesController> logger)
    {
        _dynamicCrudService = dynamicCrudService;
        _logger = logger;
    }

    private string GetTenantSubdomain()
    {
        return HttpContext.Items["TenantSubdomain"]?.ToString() ?? "demo";
    }

    private string GetUserRole()
    {
        return HttpContext.Items["UserRole"]?.ToString() ?? "operator";
    }

    /// <summary>
    /// Get all available entities
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllEntities()
    {
        try
        {
            var tenant = GetTenantSubdomain();
            var role = GetUserRole();
            
            var entities = await _dynamicCrudService.GetAllEntitiesAsync(tenant);
            
            // Filter entities based on user permissions
            var accessibleEntities = entities
                .Where(e => _dynamicCrudService.HasPermission(e, role, "read"))
                .Select(e => new
                {
                    name = e.Name,
                    displayName = e.DisplayName,
                    icon = e.Icon,
                    description = e.Description,
                    allowCreate = e.AllowCreate && _dynamicCrudService.HasPermission(e, role, "create"),
                    allowRead = e.AllowRead && _dynamicCrudService.HasPermission(e, role, "read"),
                    allowUpdate = e.AllowUpdate && _dynamicCrudService.HasPermission(e, role, "update"),
                    allowDelete = e.AllowDelete && _dynamicCrudService.HasPermission(e, role, "delete")
                })
                .ToList();
            
            return Ok(new { entities = accessibleEntities });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entities list");
            return StatusCode(500, new { success = false, error = "Erro ao buscar lista de entidades" });
        }
    }
}
