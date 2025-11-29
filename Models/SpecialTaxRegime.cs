using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolisApi.Models;

/// <summary>
/// Model SpecialTaxRegime - Special tax regime
/// </summary>
[Table("regimes_especiais_tributacao")]
public class SpecialTaxRegime : Entity
{

    [Required]
    [MaxLength(10)]
    [Column("codigo")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("descricao")]
    public string Description { get; set; } = string.Empty; // Sociedade Cooperativa, MEI, etc.

    [Column("ativo")]
    public bool Active { get; set; } = true;

    // Relacionamento
    public ICollection<Company> Companies { get; set; } = new List<Company>();
}
