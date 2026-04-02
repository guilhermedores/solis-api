using Dapper;
using Microsoft.EntityFrameworkCore;
using SolisApi.Data;
using SolisApi.DTOs;

namespace SolisApi.Repositories;

public class OperadorRepository : IOperadorRepository
{
    private readonly SolisDbContext _context;

    public OperadorRepository(SolisDbContext context)
    {
        _context = context;
    }

    private async Task EnsureOpenAsync(System.Data.IDbConnection connection, CancellationToken ct)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            await ((System.Data.Common.DbConnection)connection).OpenAsync(ct);
    }

    public async Task<OperadorSyncItem?> GetByOperatorNumberAsync(string tenantSchema, int operatorNumber, CancellationToken ct = default)
    {
        var conn = _context.Database.GetDbConnection();
        await EnsureOpenAsync(conn, ct);

        var sql = $@"
            SELECT id AS Id, name AS Name, email AS Email,
                   operator_number AS OperatorNumber, pin_hash AS PinHash,
                   role AS Role, active AS Active, updated_at AS UpdatedAt
            FROM {tenantSchema}.users
            WHERE operator_number = @OperatorNumber AND pin_hash IS NOT NULL";

        return await conn.QuerySingleOrDefaultAsync<OperadorSyncItem>(sql, new { OperatorNumber = operatorNumber });
    }

    public async Task<List<OperadorSyncItem>> ListSyncItemsAsync(string tenantSchema, CancellationToken ct = default)
    {
        var conn = _context.Database.GetDbConnection();
        await EnsureOpenAsync(conn, ct);

        var sql = $@"
            SELECT id AS Id, name AS Name, email AS Email,
                   operator_number AS OperatorNumber, pin_hash AS PinHash,
                   role AS Role, active AS Active, updated_at AS UpdatedAt
            FROM {tenantSchema}.users
            WHERE operator_number IS NOT NULL AND pin_hash IS NOT NULL
            ORDER BY operator_number";

        var result = await conn.QueryAsync<OperadorSyncItem>(sql);
        return result.ToList();
    }
}
