using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolisApi.Models;

/// <summary>
/// Model Usuario - armazenado nos schemas dos tenants (tenant_*)
/// Representa usuários do sistema em cada tenant
/// </summary>
[Table("users")]
public class Usuario
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    [Column("name")]
    public string Nome { get; set; } = string.Empty;

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
    public bool Ativo { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Notmapped - usado apenas para contexto de autenticação
    [NotMapped]
    public string? TenantId { get; set; }
}
