using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolisApi.Models;

/// <summary>
/// Model User - stored in tenant schemas (tenant_*)
/// Represents system users in each tenant
/// </summary>
[Table("users")]
public class User : Entity
{

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("role")]
    public string Role { get; set; } = "operator"; // admin, manager, operator

    [Column("active")]
    public bool Active { get; set; } = true;

    // Notmapped - usado apenas para contexto de autenticação
    [NotMapped]
    public string? TenantId { get; set; }
}
