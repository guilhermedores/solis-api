using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolisApi.Models;

/// <summary>
/// Sale item - Part of Sale aggregate
/// </summary>
[Table("sale_items")]
public class SaleItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; private set; } = Guid.NewGuid();

    [Required]
    [Column("sale_id")]
    public Guid SaleId { get; set; }

    [Required]
    [Column("product_id")]
    public Guid ProductId { get; set; }

    [MaxLength(100)]
    [Column("sku")]
    public string? Sku { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [MaxLength(10)]
    [Column("unit_of_measure")]
    public string? UnitOfMeasure { get; set; }

    [Required]
    [Column("quantity", TypeName = "numeric(12,4)")]
    public decimal Quantity { get; set; }

    [Required]
    [Column("unit_price", TypeName = "numeric(12,4)")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Column("discount_amount", TypeName = "numeric(12,2)")]
    public decimal DiscountAmount { get; set; }

    [Required]
    [Column("tax_amount", TypeName = "numeric(12,2)")]
    public decimal TaxAmount { get; set; }

    [Required]
    [Column("total", TypeName = "numeric(12,2)")]
    public decimal Total { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("SaleId")]
    public Sale Sale { get; private set; } = null!;

    private readonly List<SaleTax> _taxes = new();
    public IReadOnlyCollection<SaleTax> Taxes => _taxes.AsReadOnly();

    // Factory method
    public static SaleItem Create(
        Guid productId,
        decimal quantity,
        decimal unitPrice,
        decimal discountAmount = 0,
        string? sku = null,
        string? description = null)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));

        if (discountAmount < 0)
            throw new ArgumentException("Discount cannot be negative", nameof(discountAmount));

        // Calculate initial total (without taxes)
        var initialTotal = (quantity * unitPrice) - discountAmount;

        var item = new SaleItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountAmount = discountAmount,
            TaxAmount = 0,
            Total = initialTotal,
            Sku = sku,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        return item;
    }

    // Add tax to item
    public void AddTax(SaleTax tax)
    {
        tax.SaleItemId = Id;
        _taxes.Add(tax);
        RecalculateTotal();
    }

    // Recalculate total including taxes
    private void RecalculateTotal()
    {
        TaxAmount = _taxes.Sum(t => t.Amount);
        Total = (Quantity * UnitPrice) - DiscountAmount + TaxAmount;
    }

    // For Dapper/EF Core deserialization
    private SaleItem() { }
}
