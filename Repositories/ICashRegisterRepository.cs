using SolisApi.DTOs;
using SolisApi.Models;

namespace SolisApi.Repositories;

public interface ICashRegisterRepository
{
    Task<CashRegister?> GetByIdAsync(string tenantSchema, Guid id, CancellationToken cancellationToken = default);
    Task<CashRegister?> GetOpenByTerminalAsync(string tenantSchema, Guid storeId, int terminalNumber, CancellationToken cancellationToken = default);
    Task<(List<CashRegister> Items, int TotalCount)> ListAsync(
        string tenantSchema,
        Guid? storeId = null,
        int? terminalNumber = null,
        string? status = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
    Task SaveAsync(string tenantSchema, CashRegister entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(string tenantSchema, CashRegister entity, CancellationToken cancellationToken = default);
    Task SaveMovementAsync(string tenantSchema, CashRegisterMovement movement, CancellationToken cancellationToken = default);
    Task<List<CashRegisterMovement>> GetMovementsAsync(string tenantSchema, Guid cashRegisterId, CancellationToken cancellationToken = default);
    Task UpsertFromAgentAsync(string tenantSchema, SyncCashRegisterRequest payload, CancellationToken cancellationToken = default);
}
