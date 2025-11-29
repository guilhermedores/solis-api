using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SolisApi.Models.ValueObjects;

namespace SolisApi.Models;

/// <summary>
/// Model Company - stored in tenant schemas (tenant_*)
/// Company data for fiscal coupon
/// </summary>
[Table("empresas")]
public class Company : Entity
{

    // Dados de Identificação
    [Required]
    [MaxLength(255)]
    [Column("razao_social")]
    public string LegalName { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("nome_fantasia")]
    public string? TradeName { get; set; }

    [MaxLength(500)]
    [Column("logotipo_url")]
    public string? LogoUrl { get; set; }

    // Dados Fiscais
    [Required]
    [MaxLength(14)]
    [Column("cnpj")]
    public string Cnpj { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("inscricao_estadual")]
    public string StateRegistration { get; set; } = string.Empty;

    [MaxLength(50)]
    [Column("inscricao_municipal")]
    public string? CityRegistration { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("cnae")]
    public string Cnae { get; set; } = string.Empty;

    // Regime Tributário (FK)
    [Required]
    [Column("regime_tributario_id")]
    public Guid TaxRegimeId { get; set; }

    [ForeignKey("TaxRegimeId")]
    public TaxRegime TaxRegime { get; set; } = null!;

    // Regime Especial de Tributação (FK - opcional)
    [Column("regime_especial_tributacao_id")]
    public Guid? SpecialTaxRegimeId { get; set; }

    [ForeignKey("SpecialTaxRegimeId")]
    public SpecialTaxRegime? SpecialTaxRegime { get; set; }

    // Address (Value Object)
    public Address Address { get; set; } = new();

    // Contact (Value Object)
    public Contact Contact { get; set; } = new();

    // Controle
    [Column("active")]
    public bool Active { get; set; } = true;
}
