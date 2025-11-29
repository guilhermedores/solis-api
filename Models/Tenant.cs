using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolisApi.Models;

/// <summary>
/// Model Tenant - armazenado no schema public
/// Representa um cliente/empresa no sistema multi-tenant
/// </summary>
[Table("tenants", Schema = "public")]
public class Tenant
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("subdomain")]
    public string Subdomain { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("company_name")]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(18)]
    [Column("cnpj")]
    public string? Cnpj { get; set; }

    [Column("active")]
    public bool Active { get; set; } = true;

    [MaxLength(50)]
    [Column("plan")]
    public string Plan { get; set; } = "basic";

    [Column("max_terminals")]
    public int MaxTerminals { get; set; } = 1;

    [Column("max_users")]
    public int MaxUsers { get; set; } = 5;

    [Column("features", TypeName = "jsonb")]
    public string Features { get; set; } = "{}";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}
