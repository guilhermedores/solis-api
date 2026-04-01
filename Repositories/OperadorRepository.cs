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

    public async Task<OperadorResponse> CreateAsync(string tenantSchema, CreateOperadorRequest request, string pinHash, CancellationToken ct = default)
    {
        var conn = _context.Database.GetDbConnection();
        await EnsureOpenAsync(conn, ct);

        var sql = $@"
            INSERT INTO {tenantSchema}.users
                (id, name, email, password_hash, role, operator_number, pin_hash, active, created_at, updated_at)
            VALUES
                (uuid_generate_v4(), @Name, @Email, @PasswordHash, @Role, @OperatorNumber, @PinHash, true, NOW(), NOW())
            RETURNING id AS Id, name AS Name, email AS Email, role AS Role,
                      active AS Active, created_at AS CreatedAt, updated_at AS UpdatedAt,
                      operator_number AS OperatorNumber";

        return await conn.QuerySingleAsync<OperadorResponse>(sql, new
        {
            request.Name,
            request.Email,
            PasswordHash = pinHash, // Reuse password_hash field for BCrypt compat; operators use PIN
            request.Role,
            request.OperatorNumber,
            PinHash = pinHash
        });
    }

    public async Task UpdatePinAsync(string tenantSchema, Guid userId, string pinHash, CancellationToken ct = default)
    {
        var conn = _context.Database.GetDbConnection();
        await EnsureOpenAsync(conn, ct);

        var sql = $@"
            UPDATE {tenantSchema}.users
            SET pin_hash = @PinHash, updated_at = NOW()
            WHERE id = @Id";

        await conn.ExecuteAsync(sql, new { PinHash = pinHash, Id = userId });
    }

    public async Task SetActiveAsync(string tenantSchema, Guid userId, bool active, CancellationToken ct = default)
    {
        var conn = _context.Database.GetDbConnection();
        await EnsureOpenAsync(conn, ct);

        var sql = $@"
            UPDATE {tenantSchema}.users
            SET active = @Active, updated_at = NOW()
            WHERE id = @Id";

        await conn.ExecuteAsync(sql, new { Active = active, Id = userId });
    }

    public async Task<OperadorResponse?> GetByIdResponseAsync(string tenantSchema, Guid userId, CancellationToken ct = default)
    {
        var conn = _context.Database.GetDbConnection();
        await EnsureOpenAsync(conn, ct);

        var sql = $@"
            SELECT id AS Id, name AS Name, email AS Email,
                   operator_number AS OperatorNumber, role AS Role,
                   active AS Active, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM {tenantSchema}.users
            WHERE id = @Id AND operator_number IS NOT NULL";

        return await conn.QuerySingleOrDefaultAsync<OperadorResponse>(sql, new { Id = userId });
    }
}
