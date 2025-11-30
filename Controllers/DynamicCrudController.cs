using Microsoft.AspNetCore.Mvc;
using SolisApi.Middleware;
using SolisApi.Services;

namespace SolisApi.Controllers;

[ApiController]
[Route("api/dynamic/{entityName}")]
[RequireAuth]
public class DynamicCrudController : ControllerBase
{
    private readonly DynamicCrudService _dynamicCrudService;
    private readonly ILogger<DynamicCrudController> _logger;

    public DynamicCrudController(
        DynamicCrudService dynamicCrudService,
        ILogger<DynamicCrudController> logger)
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
    /// Get entity metadata
    /// </summary>
    [HttpGet("_metadata")]
    public async Task<IActionResult> GetMetadata(string entityName)
    {
        try
        {
            var tenant = GetTenantSubdomain();
            var metadata = await _dynamicCrudService.GetEntityMetadataAsync(tenant, entityName);
            
            if (metadata == null)
                return NotFound(new { error = $"Entity '{entityName}' not found" });
            
            var role = GetUserRole();
            if (!_dynamicCrudService.HasPermission(metadata, role, "read"))
                return StatusCode(403, new { success = false, error = "Acesso negado. Permissão insuficiente." });
            
            return Ok(metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata for entity {EntityName}", entityName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// List entities with pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        string entityName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? orderBy = null,
        [FromQuery] bool ascending = true)
    {
        try
        {
            var tenant = GetTenantSubdomain();
            var metadata = await _dynamicCrudService.GetEntityMetadataAsync(tenant, entityName);
            
            if (metadata == null)
                return NotFound(new { error = $"Entity '{entityName}' not found" });
            
            var role = GetUserRole();
            if (!_dynamicCrudService.HasPermission(metadata, role, "read"))
                return StatusCode(403, new { success = false, error = "Acesso negado. Permissão insuficiente." });
            
            var (data, totalCount) = await _dynamicCrudService.ListAsync(
                tenant, metadata, page, pageSize, search, null, orderBy, ascending);
            
            return Ok(new
            {
                data,
                pagination = new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing {EntityName}", entityName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get entity by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string entityName, Guid id)
    {
        try
        {
            var tenant = GetTenantSubdomain();
            var metadata = await _dynamicCrudService.GetEntityMetadataAsync(tenant, entityName);
            
            if (metadata == null)
                return NotFound(new { error = $"Entity '{entityName}' not found" });
            
            var role = GetUserRole();
            if (!_dynamicCrudService.HasPermission(metadata, role, "read"))
                return StatusCode(403, new { success = false, error = "Acesso negado. Permissão insuficiente." });
            
            var data = await _dynamicCrudService.GetByIdAsync(tenant, metadata, id);
            
            if (data == null)
                return NotFound(new { error = $"{metadata.DisplayName} not found" });
            
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {EntityName} by ID {Id}", entityName, id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Create new entity
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(string entityName, [FromBody] Dictionary<string, object?> data)
    {
        try
        {
            var tenant = GetTenantSubdomain();
            var metadata = await _dynamicCrudService.GetEntityMetadataAsync(tenant, entityName);
            
            if (metadata == null)
                return NotFound(new { error = $"Entity '{entityName}' not found" });
            
            var role = GetUserRole();
            if (!_dynamicCrudService.HasPermission(metadata, role, "create"))
                return StatusCode(403, new { success = false, error = "Acesso negado. Permissão insuficiente." });
            
            // Validate required fields
            var requiredFields = metadata.Fields.Where(f => f.IsRequired && f.ShowInCreate && !f.IsSystemField).ToList();
            var missingFields = requiredFields.Where(f => !data.ContainsKey(f.Name) || data[f.Name] == null).ToList();
            
            if (missingFields.Any())
            {
                return BadRequest(new
                {
                    error = "Missing required fields",
                    fields = missingFields.Select(f => f.Name)
                });
            }
            
            var id = await _dynamicCrudService.CreateAsync(tenant, metadata, data);
            
            return CreatedAtAction(nameof(GetById), new { entityName, id }, new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating {EntityName}", entityName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update entity
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string entityName, Guid id, [FromBody] Dictionary<string, object?> data)
    {
        try
        {
            var tenant = GetTenantSubdomain();
            var metadata = await _dynamicCrudService.GetEntityMetadataAsync(tenant, entityName);
            
            if (metadata == null)
                return NotFound(new { error = $"Entity '{entityName}' not found" });
            
            var role = GetUserRole();
            if (!_dynamicCrudService.HasPermission(metadata, role, "update"))
                return StatusCode(403, new { success = false, error = "Acesso negado. Permissão insuficiente." });
            
            var success = await _dynamicCrudService.UpdateAsync(tenant, metadata, id, data);
            
            if (!success)
                return NotFound(new { error = $"{metadata.DisplayName} not found" });
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating {EntityName} with ID {Id}", entityName, id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete entity
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string entityName, Guid id)
    {
        try
        {
            var tenant = GetTenantSubdomain();
            var metadata = await _dynamicCrudService.GetEntityMetadataAsync(tenant, entityName);
            
            if (metadata == null)
                return NotFound(new { error = $"Entity '{entityName}' not found" });
            
            var role = GetUserRole();
            if (!_dynamicCrudService.HasPermission(metadata, role, "delete"))
                return StatusCode(403, new { success = false, error = "Acesso negado. Permissão insuficiente." });
            
            var success = await _dynamicCrudService.DeleteAsync(tenant, metadata, id);
            
            if (!success)
                return NotFound(new { error = $"{metadata.DisplayName} not found" });
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {EntityName} with ID {Id}", entityName, id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get options for related entity (for select fields)
    /// </summary>
    [HttpGet("{id}/options/{fieldName}")]
    public async Task<IActionResult> GetFieldOptions(string entityName, Guid id, string fieldName)
    {
        try
        {
            var tenant = GetTenantSubdomain();
            var metadata = await _dynamicCrudService.GetEntityMetadataAsync(tenant, entityName);
            
            if (metadata == null)
                return NotFound(new { error = $"Entity '{entityName}' not found" });
            
            var field = metadata.Fields.FirstOrDefault(f => f.Name == fieldName);
            if (field == null)
                return NotFound(new { error = $"Field '{fieldName}' not found" });
            
            // If field has static options
            if (field.Options.Any())
            {
                return Ok(field.Options);
            }
            
            // If field has relationship (dynamic options)
            if (field.Relationship != null)
            {
                var relatedMetadata = await _dynamicCrudService.GetEntityMetadataAsync(tenant, field.Relationship.RelatedEntityName!);
                if (relatedMetadata != null)
                {
                    var (data, _) = await _dynamicCrudService.ListAsync(tenant, relatedMetadata, 1, 1000);
                    
                    var options = data.Select(d => new
                    {
                        value = d.GetValue<Guid>("id").ToString(),
                        label = d.GetValue<string>(field.Relationship.DisplayField ?? "name")
                    }).ToList();
                    
                    return Ok(options);
                }
            }
            
            return Ok(Array.Empty<object>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting options for {EntityName}.{FieldName}", entityName, fieldName);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
