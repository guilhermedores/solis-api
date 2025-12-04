using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolisApi.Models;

/// <summary>
/// Sale payment - Part of Sale aggregate
/// </summary>
[Table("sale_payments")]
public class SalePayment
{
    [Key]
    [Column("id")]
    public Guid Id { get; private set; } = Guid.NewGuid();

    [Required]
    [Column("sale_id")]
    public Guid SaleId { get; internal set; }

    [Required]
    [MaxLength(50)]
    [Column("payment_type")]
    public string PaymentType { get; private set; } = string.Empty; // cash, card, pix, voucher

    [Required]
    [Column("amount", TypeName = "numeric(12,2)")]
    public decimal Amount { get; private set; }

    [MaxLength(200)]
    [Column("acquirer_txn_id")]
    public string? AcquirerTxnId { get; private set; }

    [MaxLength(200)]
    [Column("authorization_code")]
    public string? AuthorizationCode { get; private set; }

    [Column("change_amount", TypeName = "numeric(12,2)")]
    public decimal? ChangeAmount { get; private set; }

    [Required]
    [MaxLength(50)]
    [Column("status")]
    public string Status { get; private set; } = "processed"; // pending, processed, failed, reversed

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; private set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("SaleId")]
    public Sale Sale { get; private set; } = null!;

    // Factory method
    public static SalePayment Create(
        string paymentType,
        decimal amount,
        string? acquirerTxnId = null,
        string? authorizationCode = null,
        decimal? changeAmount = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero", nameof(amount));

        var validTypes = new[] { "cash", "card", "pix", "voucher" };
        if (!validTypes.Contains(paymentType))
            throw new ArgumentException($"Invalid payment type: {paymentType}", nameof(paymentType));

        return new SalePayment
        {
            Id = Guid.NewGuid(),
            PaymentType = paymentType,
            Amount = amount,
            AcquirerTxnId = acquirerTxnId,
            AuthorizationCode = authorizationCode,
            ChangeAmount = changeAmount,
            Status = "processed",
            ProcessedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    // For EF Core deserialization
    private SalePayment() { }
}
