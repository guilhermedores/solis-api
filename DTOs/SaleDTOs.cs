namespace SolisApi.DTOs;

// ==================== REQUEST DTOs ====================

public class CreateSaleRequest
{
    public Guid? ClientSaleId { get; set; } // For offline sync and idempotency
    public Guid StoreId { get; set; }
    public Guid? PosId { get; set; }
    public Guid? OperatorId { get; set; }
    public DateTime? SaleDateTime { get; set; }
    public List<SaleItemRequest> Items { get; set; } = new();
    public List<SalePaymentRequest>? Payments { get; set; }
}

public class SaleItemRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
}

public class SalePaymentRequest
{
    public Guid PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public string? AcquirerTxnId { get; set; }
    public string? AuthorizationCode { get; set; }
    public decimal? ChangeAmount { get; set; }
}

public class UpdateSaleRequest
{
    public string? Status { get; set; }
    public string? PaymentStatus { get; set; }
}

public class CancelSaleRequest
{
    public string? Reason { get; set; }
    public string? Source { get; set; } // api, pos, system, admin
    public string CancellationType { get; set; } = "total"; // total, partial
    public decimal? RefundAmount { get; set; }
}

public class SaleSyncRequest
{
    public List<CreateSaleRequest> Sales { get; set; } = new();
}

// ==================== RESPONSE DTOs ====================

public class SaleResponse
{
    public Guid Id { get; set; }
    public Guid? ClientSaleId { get; set; }
    public Guid StoreId { get; set; }
    public Guid? PosId { get; set; }
    public Guid? OperatorId { get; set; }
    public DateTime SaleDateTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<SaleItemDto> Items { get; set; } = new();
    public List<SalePaymentDto> Payments { get; set; } = new();
    public SaleCancellationDto? Cancellation { get; set; }
}

public class SaleItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public List<SaleTaxDto> Taxes { get; set; } = new();
}

public class SalePaymentDto
{
    public Guid Id { get; set; }
    public Guid PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public string? AcquirerTxnId { get; set; }
    public string? AuthorizationCode { get; set; }
    public decimal? ChangeAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SaleTaxDto
{
    public Guid Id { get; set; }
    public Guid TaxTypeId { get; set; }
    public string TaxTypeCode { get; set; } = string.Empty;
    public Guid? TaxRuleId { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}

public class SaleCancellationDto
{
    public Guid Id { get; set; }
    public Guid? OperatorId { get; set; }
    public string? Reason { get; set; }
    public DateTime CanceledAt { get; set; }
    public string? Source { get; set; }
    public string CancellationType { get; set; } = string.Empty;
    public decimal? RefundAmount { get; set; }
}

public class SaleListResponse
{
    public List<SaleResponse> Data { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
}

public class PaginationMetadata
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

// ==================== SYNC RESPONSE DTOs ====================

public class SaleSyncResponse
{
    public List<SaleSyncResult> Results { get; set; } = new();
    public SyncSummary Summary { get; set; } = new();
}

public class SaleSyncResult
{
    public Guid? ClientSaleId { get; set; }
    public Guid? SaleId { get; set; }
    public string Status { get; set; } = string.Empty; // created, ignored, conflict, error
    public string? Message { get; set; }
    public int HttpStatusCode { get; set; }
}

public class SyncSummary
{
    public int Total { get; set; }
    public int Created { get; set; }
    public int Ignored { get; set; }
    public int Conflicts { get; set; }
    public int Errors { get; set; }
}

// ==================== SYNC STATUS RESPONSE ====================

public class SyncStatusResponse
{
    public Guid ClientSaleId { get; set; }
    public Guid? SaleId { get; set; }
    public bool Exists { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
}
