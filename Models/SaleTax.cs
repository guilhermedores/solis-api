using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolisApi.Models;

/// <summary>
/// Tax calculation - Part of SaleItem
/// </summary>
[Table("sale_taxes")]
public class SaleTax
{
    [Key]
    [Column("id")]
    public Guid Id { get; private set; } = Guid.NewGuid();

    [Required]
    [Column("sale_item_id")]
    public Guid SaleItemId { get; internal set; }

    [Required]
    [Column("tax_type_id")]
    public Guid TaxTypeId { get; private set; }
    // TaxType é gerenciado via CRUD genérico: /api/dynamic/tax_type/{id}

    [Column("tax_rule_id")]
    public Guid? TaxRuleId { get; private set; } // NULL if manual or fallback
    // TaxRule é gerenciado via CRUD genérico: /api/dynamic/tax_rule/{id}

    [Required]
    [Column("base_amount", TypeName = "numeric(12,2)")]
    public decimal BaseAmount { get; private set; }

    [Required]
    [Column("rate", TypeName = "numeric(10,4)")]
    public decimal Rate { get; private set; }

    [Required]
    [Column("amount", TypeName = "numeric(12,2)")]
    public decimal Amount { get; private set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("SaleItemId")]
    public SaleItem SaleItem { get; private set; } = null!;

    // Factory method
    public static SaleTax Create(
        Guid taxTypeId,
        Guid? taxRuleId,
        decimal baseAmount,
        decimal rate,
        decimal amount)
    {
        if (baseAmount < 0)
            throw new ArgumentException("Base amount cannot be negative", nameof(baseAmount));

        if (rate < 0)
            throw new ArgumentException("Tax rate cannot be negative", nameof(rate));

        if (amount < 0)
            throw new ArgumentException("Tax amount cannot be negative", nameof(amount));

        return new SaleTax
        {
            Id = Guid.NewGuid(),
            TaxTypeId = taxTypeId,
            TaxRuleId = taxRuleId,
            BaseAmount = baseAmount,
            Rate = rate,
            Amount = amount,
            CreatedAt = DateTime.UtcNow
        };
    }

    // For EF Core deserialization
    private SaleTax() { }
}
