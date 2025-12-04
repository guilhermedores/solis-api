using SolisApi.Models;

namespace SolisApi.Services.Domain;

/// <summary>
/// Domain Service for tax calculation
/// Encapsulates complex tax business rules
/// </summary>
public interface ITaxDomainService
{
    Task<List<SaleTax>> CalculateTaxesForItemAsync(
        string tenantSchema,
        Guid productId,
        decimal baseAmount,
        string state,
        CancellationToken cancellationToken = default);
}
