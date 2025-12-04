using SolisApi.DTOs;

namespace SolisApi.Services;

/// <summary>
/// Application Service interface for Sales
/// </summary>
public interface ISalesService
{
    Task<SaleResponse> CreateSaleAsync(string tenantSchema, CreateSaleRequest request, string? idempotencyKey = null, CancellationToken cancellationToken = default);
    Task<SaleResponse?> GetSaleByIdAsync(string tenantSchema, Guid saleId, CancellationToken cancellationToken = default);
    Task<SaleListResponse> GetSalesAsync(string tenantSchema, SalesQueryParameters query, CancellationToken cancellationToken = default);
    Task<SaleResponse> UpdateSaleAsync(string tenantSchema, Guid saleId, UpdateSaleRequest request, CancellationToken cancellationToken = default);
    Task<SaleResponse> AddPaymentAsync(string tenantSchema, Guid saleId, SalePaymentRequest request, CancellationToken cancellationToken = default);
    Task<SaleResponse> CancelSaleAsync(string tenantSchema, Guid saleId, CancelSaleRequest request, CancellationToken cancellationToken = default);
    Task<SyncStatusResponse> GetSyncStatusAsync(string tenantSchema, Guid clientSaleId, CancellationToken cancellationToken = default);
    Task<SaleSyncResponse> SyncSalesAsync(string tenantSchema, SaleSyncRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Query parameters for sales list
/// </summary>
public class SalesQueryParameters
{
    public Guid? StoreId { get; set; }
    public Guid? PosId { get; set; }
    public Guid? OperatorId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Status { get; set; }
    public Guid? ClientSaleId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
