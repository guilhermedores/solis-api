using System.Text.Json;

namespace SolisApi.Models.Metadata;

/// <summary>
/// Entity metadata model
/// </summary>
public class EntityMetadata
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AllowCreate { get; set; } = true;
    public bool AllowRead { get; set; } = true;
    public bool AllowUpdate { get; set; } = true;
    public bool AllowDelete { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public List<EntityField> Fields { get; set; } = new();
    public List<EntityRelationship> Relationships { get; set; } = new();
    public List<EntityPermission> Permissions { get; set; } = new();
}

/// <summary>
/// Entity field metadata model
/// </summary>
public class EntityField
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string FieldType { get; set; } = "text";
    public int? MaxLength { get; set; }
    public bool IsRequired { get; set; }
    public bool IsUnique { get; set; }
    public bool IsReadonly { get; set; }
    public bool IsSystemField { get; set; }
    public bool ShowInList { get; set; } = true;
    public bool ShowInDetail { get; set; } = true;
    public bool ShowInCreate { get; set; } = true;
    public bool ShowInUpdate { get; set; } = true;
    public int ListOrder { get; set; }
    public int FormOrder { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRegex { get; set; }
    public string? ValidationMessage { get; set; }
    public string? HelpText { get; set; }
    public string? Placeholder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public List<EntityFieldOption> Options { get; set; } = new();
    public EntityRelationship? Relationship { get; set; }
}

/// <summary>
/// Entity relationship metadata model
/// </summary>
public class EntityRelationship
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public Guid FieldId { get; set; }
    public Guid RelatedEntityId { get; set; }
    public string RelationshipType { get; set; } = string.Empty;
    public string? ForeignKeyColumn { get; set; }
    public string? DisplayField { get; set; }
    public bool CascadeDelete { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public string? RelatedEntityName { get; set; }
    public string? RelatedEntityDisplayName { get; set; }
    public string? RelatedEntityTableName { get; set; }
}

/// <summary>
/// Entity field option model (for select/multiselect)
/// </summary>
public class EntityFieldOption
{
    public Guid Id { get; set; }
    public Guid FieldId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Entity permission model
/// </summary>
public class EntityPermission
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool CanCreate { get; set; }
    public bool CanRead { get; set; } = true;
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanReadOwnOnly { get; set; }
    public JsonDocument? FieldPermissions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Dynamic entity data (generic record)
/// </summary>
public class DynamicEntityData
{
    public Dictionary<string, object?> Data { get; set; } = new();
    
    public T? GetValue<T>(string fieldName)
    {
        if (Data.TryGetValue(fieldName, out var value) && value != null)
        {
            if (value is T typedValue)
                return typedValue;
            
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }
    
    public void SetValue(string fieldName, object? value)
    {
        Data[fieldName] = value;
    }
}
