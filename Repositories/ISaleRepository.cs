using SolisApi.Models;

namespace SolisApi.Repositories;

/// <summary>
/// Repository interface for Sale aggregate
/// </summary>
public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(string tenantSchema, Guid id, CancellationToken cancellationToken = default);
    Task<Sale?> GetByClientSaleIdAsync(string tenantSchema, Guid clientSaleId, CancellationToken cancellationToken = default);
    Task<(List<Sale> Sales, int TotalCount)> GetAllAsync(
        string tenantSchema,
        Guid? storeId = null,
        Guid? posId = null,
        Guid? operatorId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? status = null,
        Guid? clientSaleId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
    Task SaveAsync(string tenantSchema, Sale sale, CancellationToken cancellationToken = default);
    Task UpdateAsync(string tenantSchema, Sale sale, CancellationToken cancellationToken = default);
    Task<ProductInfo?> GetProductByIdAsync(string tenantSchema, Guid productId, CancellationToken cancellationToken = default);
    Task<PaymentMethodInfo?> GetPaymentMethodByIdAsync(string tenantSchema, Guid paymentMethodId, CancellationToken cancellationToken = default);
}

// DTOs para dados auxiliares
public record ProductInfo(Guid Id, string? Sku, string Description);
public record PaymentMethodInfo(Guid Id, string PaymentTypeCode, string Description);
