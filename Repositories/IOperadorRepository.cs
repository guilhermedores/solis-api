using SolisApi.DTOs;

namespace SolisApi.Repositories;

public interface IOperadorRepository
{
    Task<OperadorSyncItem?> GetByOperatorNumberAsync(string tenantSchema, int operatorNumber, CancellationToken ct = default);
    Task<List<OperadorSyncItem>> ListSyncItemsAsync(string tenantSchema, CancellationToken ct = default);
    Task<OperadorResponse> CreateAsync(string tenantSchema, CreateOperadorRequest request, string pinHash, CancellationToken ct = default);
    Task UpdatePinAsync(string tenantSchema, Guid userId, string pinHash, CancellationToken ct = default);
    Task SetActiveAsync(string tenantSchema, Guid userId, bool active, CancellationToken ct = default);
    Task<OperadorResponse?> GetByIdResponseAsync(string tenantSchema, Guid userId, CancellationToken ct = default);
}
