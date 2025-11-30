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
    [MaxLength(50)]
    [Column("subdomain")]
    public string Subdomain { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("legal_name")]
    public string LegalName { get; set; } = string.Empty;

    [MaxLength(200)]
    [Column("trade_name")]
    public string? TradeName { get; set; }

    [MaxLength(14)]
    [Column("cnpj")]
    public string? Cnpj { get; set; }

    [Required]
    [MaxLength(63)]
    [Column("schema_name")]
    public string SchemaName { get; set; } = string.Empty;

    [Column("active")]
    public bool Active { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
