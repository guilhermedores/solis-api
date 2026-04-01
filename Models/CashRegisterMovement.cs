namespace SolisApi.Models;

public class CashRegisterMovement
{
    public Guid Id { get; set; }
    public Guid CashRegisterId { get; set; }
    public Guid StoreId { get; set; }
    public string Type { get; set; } = string.Empty; // opening, closing, sangria, suprimento
    public decimal Amount { get; set; }
    public Guid? OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
