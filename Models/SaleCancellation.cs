using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolisApi.Models;

/// <summary>
/// Sale cancellation record
/// </summary>
[Table("sale_cancellations")]
public class SaleCancellation
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("sale_id")]
    public Guid SaleId { get; set; }

    [Column("operator_id")]
    public Guid? OperatorId { get; set; }

    [Column("reason")]
    public string? Reason { get; set; }

    [Required]
    [Column("canceled_at")]
    public DateTime CanceledAt { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    [Column("source")]
    public string? Source { get; set; } // api, pos, system, admin

    [Required]
    [MaxLength(50)]
    [Column("cancellation_type")]
    public string CancellationType { get; set; } = "total"; // total, partial

    [Column("refund_amount", TypeName = "numeric(12,2)")]
    public decimal? RefundAmount { get; set; }

    [Column("payment_reversal_id")]
    public Guid? PaymentReversalId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("SaleId")]
    public Sale Sale { get; set; } = null!;
}
