namespace SolisApi.Models;

public class CashRegister
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public int TerminalNumber { get; set; }
    public Guid? OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal? ClosingBalance { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalPix { get; set; }
    public decimal TotalOthers { get; set; }
    public int QuantitySales { get; set; }
    public decimal TotalSangria { get; set; }
    public decimal TotalSuprimento { get; set; }
    public decimal? ExpectedBalance { get; set; }
    public decimal? Difference { get; set; }
    public string Status { get; set; } = "open";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
