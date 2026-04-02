using SolisApi.DTOs;

namespace SolisApi.Repositories;

public interface IOperadorRepository
{
    Task<OperadorSyncItem?> GetByOperatorNumberAsync(string tenantSchema, int operatorNumber, CancellationToken ct = default);
    Task<List<OperadorSyncItem>> ListSyncItemsAsync(string tenantSchema, CancellationToken ct = default);
}
