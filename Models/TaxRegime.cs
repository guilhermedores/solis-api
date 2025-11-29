using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolisApi.Models;

/// <summary>
/// Model TaxRegime - Company tax regime
/// </summary>
[Table("regimes_tributarios")]
public class TaxRegime : Entity
{

    [Required]
    [MaxLength(10)]
    [Column("codigo")]
    public string Code { get; set; } = string.Empty; // 1, 2, 3, etc.

    [Required]
    [MaxLength(100)]
    [Column("descricao")]
    public string Description { get; set; } = string.Empty; // Simples Nacional, Lucro Presumido, etc.

    [Column("active")]
    public bool Active { get; set; } = true;

    // Relacionamento
    public ICollection<Company> Companies { get; set; } = new List<Company>();
}
