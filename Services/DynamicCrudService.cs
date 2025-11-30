using Dapper;
using Npgsql;
using SolisApi.Models.Metadata;
using System.Text;
using System.Text.Json;

namespace SolisApi.Services;

public class DynamicCrudService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DynamicCrudService> _logger;

    public DynamicCrudService(IConfiguration configuration, ILogger<DynamicCrudService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private string GetConnectionString(string? tenantSubdomain = null)
    {
        var baseConnectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
        
        if (!string.IsNullOrEmpty(tenantSubdomain))
        {
            var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
            {
                SearchPath = $"tenant_{tenantSubdomain},public"
            };
            return builder.ToString();
        }
        
        return baseConnectionString;
    }

    /// <summary>
    /// Convert JsonElement to primitive value for Dapper
    /// </summary>
    private static object? ConvertJsonElement(object? value)
    {
        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.ToString()
            };
        }
        return value;
    }

    /// <summary>
    /// Get all available entities
    /// </summary>
    public async Task<List<EntityMetadata>> GetAllEntitiesAsync(string tenantSubdomain)
    {
        var connectionString = GetConnectionString(tenantSubdomain);
        
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var schema = $"tenant_{tenantSubdomain}";
        
        // Get all active entities
        var entities = (await connection.QueryAsync<EntityMetadata>(
            $@"SELECT id as Id, name as Name, table_name as TableName, display_name as DisplayName, 
                     description as Description, icon as Icon, allow_create as AllowCreate, 
                     allow_read as AllowRead, allow_update as AllowUpdate, allow_delete as AllowDelete, 
                     is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt
              FROM {schema}.entities 
              WHERE is_active = true
              ORDER BY display_name"
        )).ToList();
        
        // Load permissions for each entity
        foreach (var entity in entities)
        {
            entity.Permissions = (await connection.QueryAsync<EntityPermission>(
                $@"SELECT id as Id, entity_id as EntityId, role as Role, 
                         can_create as CanCreate, can_read as CanRead, 
                         can_update as CanUpdate, can_delete as CanDelete, 
                         can_read_own_only as CanReadOwnOnly, field_permissions as FieldPermissions,
                         created_at as CreatedAt, updated_at as UpdatedAt
                  FROM {schema}.entity_permissions 
                  WHERE entity_id = @EntityId",
                new { EntityId = entity.Id }
            )).ToList();
        }
        
        return entities;
    }

    /// <summary>
    /// Get entity metadata by name
    /// </summary>
    public async Task<EntityMetadata?> GetEntityMetadataAsync(string tenantSubdomain, string entityName)
    {
        var connectionString = GetConnectionString(tenantSubdomain);
        
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var schema = $"tenant_{tenantSubdomain}";
        
        // Get entity
        var entity = await connection.QueryFirstOrDefaultAsync<EntityMetadata>(
            $@"SELECT id as Id, name as Name, table_name as TableName, display_name as DisplayName, 
                     description as Description, icon as Icon, allow_create as AllowCreate, 
                     allow_read as AllowRead, allow_update as AllowUpdate, allow_delete as AllowDelete, 
                     is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt
              FROM {schema}.entities 
              WHERE name = @EntityName AND is_active = true",
            new { EntityName = entityName }
        );
        
        if (entity == null)
            return null;
        
        // Get fields
        entity.Fields = (await connection.QueryAsync<EntityField>(
            $@"SELECT id as Id, entity_id as EntityId, name as Name, column_name as ColumnName, 
                     display_name as DisplayName, data_type as DataType, field_type as FieldType, 
                     is_required as IsRequired, is_system_field as IsSystemField, 
                     is_unique as IsUnique, is_readonly as IsReadonly, max_length as MaxLength, 
                     default_value as DefaultValue, show_in_list as ShowInList, list_order as ListOrder, 
                     show_in_create as ShowInCreate, show_in_update as ShowInUpdate, 
                     show_in_detail as ShowInDetail, form_order as FormOrder, 
                     validation_regex as ValidationRegex, validation_message as ValidationMessage,
                     help_text as HelpText, placeholder as Placeholder, 
                     created_at as CreatedAt, updated_at as UpdatedAt
              FROM {schema}.entity_fields 
              WHERE entity_id = @EntityId 
              ORDER BY list_order, form_order",
            new { EntityId = entity.Id }
        )).ToList();
        
        // Get field options
        foreach (var field in entity.Fields.Where(f => f.FieldType == "select" || f.FieldType == "multiselect"))
        {
            field.Options = (await connection.QueryAsync<EntityFieldOption>(
                $@"SELECT id as Id, field_id as FieldId, value as Value, label as Label, 
                         display_order as DisplayOrder, is_active as IsActive, 
                         created_at as CreatedAt
                  FROM {schema}.entity_field_options 
                  WHERE field_id = @FieldId AND is_active = true 
                  ORDER BY display_order",
                new { FieldId = field.Id }
            )).ToList();
        }
        
        // Get relationships
        entity.Relationships = (await connection.QueryAsync<EntityRelationship>(
            $@"SELECT er.id as Id, er.entity_id as EntityId, er.field_id as FieldId, 
                     er.related_entity_id as RelatedEntityId, er.relationship_type as RelationshipType,
                     er.foreign_key_column as ForeignKeyColumn, er.display_field as DisplayField,
                     er.cascade_delete as CascadeDelete, er.created_at as CreatedAt,
                     e.name as RelatedEntityName, e.display_name as RelatedEntityDisplayName, 
                     e.table_name as RelatedEntityTableName
              FROM {schema}.entity_relationships er
              JOIN {schema}.entities e ON er.related_entity_id = e.id
              WHERE er.entity_id = @EntityId",
            new { EntityId = entity.Id }
        )).ToList();
        
        // Map relationships to fields
        foreach (var rel in entity.Relationships)
        {
            var field = entity.Fields.FirstOrDefault(f => f.Id == rel.FieldId);
            if (field != null)
                field.Relationship = rel;
        }
        
        // Get permissions
        entity.Permissions = (await connection.QueryAsync<EntityPermission>(
            $@"SELECT id as Id, entity_id as EntityId, role as Role, 
                     can_create as CanCreate, can_read as CanRead, 
                     can_update as CanUpdate, can_delete as CanDelete, 
                     can_read_own_only as CanReadOwnOnly, field_permissions as FieldPermissions,
                     created_at as CreatedAt, updated_at as UpdatedAt
              FROM {schema}.entity_permissions 
              WHERE entity_id = @EntityId",
            new { EntityId = entity.Id }
        )).ToList();
        
        return entity;
    }

    /// <summary>
    /// List entities with pagination and filtering
    /// </summary>
    public async Task<(List<DynamicEntityData> Data, int TotalCount)> ListAsync(
        string tenantSubdomain,
        EntityMetadata entity,
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        Dictionary<string, object>? filters = null,
        string? orderBy = null,
        bool ascending = true)
    {
        var connectionString = GetConnectionString(tenantSubdomain);
        var schema = $"tenant_{tenantSubdomain}";
        
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        // Build select fields - ALWAYS include id field
        var idField = entity.Fields.FirstOrDefault(f => f.Name == "id");
        var fields = entity.Fields.Where(f => f.ShowInList).OrderBy(f => f.ListOrder).ToList();
        
        // Se nenhum campo está marcado como ShowInList, pegar pelo menos os campos essenciais
        if (!fields.Any())
        {
            fields = entity.Fields.Where(f => f.IsSystemField || f.Name == "id" || f.Name == "name").ToList();
        }
        
        // Se ainda não tem campos, pegar todos exceto password
        if (!fields.Any())
        {
            fields = entity.Fields.Where(f => !f.Name.Contains("password", StringComparison.OrdinalIgnoreCase)).ToList();
        }
        
        // Ensure id field is always included (at the beginning)
        if (idField != null && !fields.Any(f => f.Name == "id"))
        {
            fields.Insert(0, idField);
        }
        
        var selectFields = string.Join(", ", fields.Select(f => f.ColumnName));
        
        // Build WHERE clause
        var whereConditions = new List<string>();
        var parameters = new DynamicParameters();
        
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var searchFields = fields.Where(f => f.DataType == "string").ToList();
            if (searchFields.Any())
            {
                var searchConditions = searchFields.Select(f => $"{f.ColumnName}::text ILIKE @SearchTerm");
                whereConditions.Add($"({string.Join(" OR ", searchConditions)})");
                parameters.Add("SearchTerm", $"%{searchTerm}%");
            }
        }
        
        if (filters != null)
        {
            foreach (var filter in filters)
            {
                var field = fields.FirstOrDefault(f => f.Name == filter.Key);
                if (field != null)
                {
                    whereConditions.Add($"{field.ColumnName} = @{filter.Key}");
                    parameters.Add(filter.Key, filter.Value);
                }
            }
        }
        
        var whereClause = whereConditions.Any() ? $"WHERE {string.Join(" AND ", whereConditions)}" : "";
        
        // Build ORDER BY
        var orderByClause = "created_at DESC";
        if (!string.IsNullOrEmpty(orderBy))
        {
            var orderField = fields.FirstOrDefault(f => f.Name == orderBy);
            if (orderField != null)
            {
                orderByClause = $"{orderField.ColumnName} {(ascending ? "ASC" : "DESC")}";
            }
        }
        
        // Count total
        var countSql = $"SELECT COUNT(*) FROM {schema}.{entity.TableName} {whereClause}";
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        
        // Get data
        var offset = (page - 1) * pageSize;
        var dataSql = $@"
            SELECT {selectFields} 
            FROM {schema}.{entity.TableName} 
            {whereClause} 
            ORDER BY {orderByClause} 
            LIMIT @PageSize OFFSET @Offset";
        
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);
        
        var rows = await connection.QueryAsync(dataSql, parameters);
        
        var result = rows.Select(row =>
        {
            var data = new DynamicEntityData();
            var dict = (IDictionary<string, object>)row;
            foreach (var kvp in dict)
            {
                data.SetValue(kvp.Key, kvp.Value);
            }
            return data;
        }).ToList();
        
        return (result, totalCount);
    }

    /// <summary>
    /// Get single entity by ID
    /// </summary>
    public async Task<DynamicEntityData?> GetByIdAsync(string tenantSubdomain, EntityMetadata entity, Guid id)
    {
        var connectionString = GetConnectionString(tenantSubdomain);
        var schema = $"tenant_{tenantSubdomain}";
        
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var fields = entity.Fields.Where(f => f.ShowInDetail).ToList();
        var selectFields = string.Join(", ", fields.Select(f => f.ColumnName));
        
        var sql = $"SELECT {selectFields} FROM {schema}.{entity.TableName} WHERE id = @Id";
        var row = await connection.QueryFirstOrDefaultAsync(sql, new { Id = id });
        
        if (row == null)
            return null;
        
        var data = new DynamicEntityData();
        var dict = (IDictionary<string, object>)row;
        foreach (var kvp in dict)
        {
            data.SetValue(kvp.Key, kvp.Value);
        }
        
        return data;
    }

    /// <summary>
    /// Create new entity
    /// </summary>
    public async Task<Guid> CreateAsync(string tenantSubdomain, EntityMetadata entity, Dictionary<string, object?> values)
    {
        var connectionString = GetConnectionString(tenantSubdomain);
        var schema = $"tenant_{tenantSubdomain}";
        
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var fields = entity.Fields.Where(f => f.ShowInCreate && !f.IsSystemField).ToList();
        var columns = new List<string>();
        var paramNames = new List<string>();
        var parameters = new DynamicParameters();
        var id = Guid.NewGuid();
        
        columns.Add("id");
        paramNames.Add("@id");
        parameters.Add("id", id);
        
        foreach (var field in fields)
        {
            if (values.ContainsKey(field.Name))
            {
                columns.Add(field.ColumnName);
                paramNames.Add($"@{field.Name}");
                parameters.Add(field.Name, ConvertJsonElement(values[field.Name]));
            }
            else if (field.IsRequired && field.DefaultValue != null)
            {
                columns.Add(field.ColumnName);
                paramNames.Add($"@{field.Name}");
                parameters.Add(field.Name, field.DefaultValue);
            }
        }
        
        columns.Add("created_at");
        columns.Add("updated_at");
        paramNames.Add("@created_at");
        paramNames.Add("@updated_at");
        parameters.Add("created_at", DateTime.UtcNow);
        parameters.Add("updated_at", DateTime.UtcNow);
        
        var columnList = string.Join(", ", columns);
        var valueList = string.Join(", ", paramNames);
        
        var sql = $"INSERT INTO {schema}.{entity.TableName} ({columnList}) VALUES ({valueList})";
        
        await connection.ExecuteAsync(sql, parameters);
        
        _logger.LogInformation("Created {Entity} with ID {Id}", entity.Name, id);
        
        return id;
    }

    /// <summary>
    /// Update entity
    /// </summary>
    public async Task<bool> UpdateAsync(string tenantSubdomain, EntityMetadata entity, Guid id, Dictionary<string, object?> values)
    {
        var connectionString = GetConnectionString(tenantSubdomain);
        var schema = $"tenant_{tenantSubdomain}";
        
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        var fields = entity.Fields.Where(f => f.ShowInUpdate && !f.IsSystemField && !f.IsReadonly).ToList();
        var setFields = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("Id", id);
        
        foreach (var field in fields)
        {
            if (values.ContainsKey(field.Name))
            {
                setFields.Add($"{field.ColumnName} = @{field.Name}");
                parameters.Add(field.Name, ConvertJsonElement(values[field.Name]));
            }
        }
        
        if (!setFields.Any())
            return false;
        
        setFields.Add("updated_at = @UpdatedAt");
        parameters.Add("UpdatedAt", DateTime.UtcNow);
        
        var sql = $"UPDATE {schema}.{entity.TableName} SET {string.Join(", ", setFields)} WHERE id = @Id";
        
        var affected = await connection.ExecuteAsync(sql, parameters);
        
        _logger.LogInformation("Updated {Entity} with ID {Id}", entity.Name, id);
        
        return affected > 0;
    }

    /// <summary>
    /// Delete entity
    /// </summary>
    public async Task<bool> DeleteAsync(string tenantSubdomain, EntityMetadata entity, Guid id)
    {
        var connectionString = GetConnectionString(tenantSubdomain);
        var schema = $"tenant_{tenantSubdomain}";
        
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        
        // Check if entity has 'active' field (soft delete)
        var hasActiveField = entity.Fields.Any(f => f.Name == "active");
        
        string sql;
        if (hasActiveField)
        {
            sql = $"UPDATE {schema}.{entity.TableName} SET active = false, updated_at = @UpdatedAt WHERE id = @Id";
            var affected = await connection.ExecuteAsync(sql, new { Id = id, UpdatedAt = DateTime.UtcNow });
            _logger.LogInformation("Soft deleted {Entity} with ID {Id}", entity.Name, id);
            return affected > 0;
        }
        else
        {
            sql = $"DELETE FROM {schema}.{entity.TableName} WHERE id = @Id";
            var affected = await connection.ExecuteAsync(sql, new { Id = id });
            _logger.LogInformation("Hard deleted {Entity} with ID {Id}", entity.Name, id);
            return affected > 0;
        }
    }

    /// <summary>
    /// Check user permission for entity operation
    /// </summary>
    public bool HasPermission(EntityMetadata entity, string role, string operation)
    {
        var permission = entity.Permissions.FirstOrDefault(p => p.Role == role);
        if (permission == null)
            return false;
        
        return operation.ToLower() switch
        {
            "create" => permission.CanCreate && entity.AllowCreate,
            "read" => permission.CanRead && entity.AllowRead,
            "update" => permission.CanUpdate && entity.AllowUpdate,
            "delete" => permission.CanDelete && entity.AllowDelete,
            _ => false
        };
    }
}
