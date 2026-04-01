using System.ComponentModel.DataAnnotations;

namespace SolisApi.DTOs;

public class OpenCashRegisterRequest
{
    [Required]
    public Guid StoreId { get; set; }

    [Required]
    public int TerminalNumber { get; set; }

    public Guid? OperatorId { get; set; }

    [Required]
    [MaxLength(200)]
    public string OperatorName { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal OpeningBalance { get; set; }

    public string? Notes { get; set; }
}

public class CloseCashRegisterRequest
{
    [Required]
    [Range(0, double.MaxValue)]
    public decimal ClosingBalance { get; set; }

    public decimal TotalSales { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalPix { get; set; }
    public decimal TotalOthers { get; set; }
    public int QuantitySales { get; set; }
    public string? Notes { get; set; }
}

public class MovementRequest
{
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public Guid? OperatorId { get; set; }

    [Required]
    [MaxLength(200)]
    public string OperatorName { get; set; } = string.Empty;

    public string? Notes { get; set; }
}

/// <summary>
/// Payload enviado pelo outbox do solis-agente via PUT /api/caixas/{id}
/// </summary>
public class SyncCashRegisterRequest
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
    public decimal? Difference { get; set; }
    public string Status { get; set; } = "open";
    public string? Notes { get; set; }

    // Campos do modelo do agente (nomes em português para compatibilidade com JSON do outbox)
    public int NumeroTerminal { get => TerminalNumber; set => TerminalNumber = value; }
    public string OperadorNome { get => OperatorName; set => OperatorName = value; }
    public string? OperadorId { get => OperatorId?.ToString(); set => OperatorId = value != null ? Guid.TryParse(value, out var g) ? g : null : null; }
    public DateTime DataAbertura { get => OpenedAt; set => OpenedAt = value; }
    public DateTime? DataFechamento { get => ClosedAt; set => ClosedAt = value; }
    public decimal ValorAbertura { get => OpeningBalance; set => OpeningBalance = value; }
    public decimal? ValorFechamento { get => ClosingBalance; set => ClosingBalance = value; }
    public decimal TotalVendas { get => TotalSales; set => TotalSales = value; }
    public decimal TotalDinheiro { get => TotalCash; set => TotalCash = value; }
    public decimal TotalDebito { get => TotalDebit; set => TotalDebit = value; }
    public decimal TotalCredito { get => TotalCredit; set => TotalCredit = value; }
    public int QuantidadeVendas { get => QuantitySales; set => QuantitySales = value; }
    public decimal? Diferenca { get => Difference; set => Difference = value; }
}

public class CashRegisterResponse
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
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CashRegisterMovementDto> Movements { get; set; } = new();
}

public class CashRegisterMovementDto
{
    public Guid Id { get; set; }
    public Guid CashRegisterId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime OccurredAt { get; set; }
}
