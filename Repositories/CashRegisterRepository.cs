using Dapper;
using Microsoft.EntityFrameworkCore;
using SolisApi.Data;
using SolisApi.DTOs;
using SolisApi.Models;

namespace SolisApi.Repositories;

public class CashRegisterRepository : ICashRegisterRepository
{
    private readonly SolisDbContext _context;

    public CashRegisterRepository(SolisDbContext context)
    {
        _context = context;
    }

    private async Task EnsureOpenAsync(System.Data.IDbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            await ((System.Data.Common.DbConnection)connection).OpenAsync(cancellationToken);
    }

    public async Task<CashRegister?> GetByIdAsync(string tenantSchema, Guid id, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        var sql = $@"
            SELECT
                id AS Id, store_id AS StoreId, terminal_number AS TerminalNumber,
                operator_id AS OperatorId, operator_name AS OperatorName,
                opened_at AS OpenedAt, closed_at AS ClosedAt,
                opening_balance AS OpeningBalance, closing_balance AS ClosingBalance,
                total_sales AS TotalSales, total_cash AS TotalCash,
                total_debit AS TotalDebit, total_credit AS TotalCredit,
                total_pix AS TotalPix, total_others AS TotalOthers,
                quantity_sales AS QuantitySales,
                total_sangria AS TotalSangria, total_suprimento AS TotalSuprimento,
                expected_balance AS ExpectedBalance, difference AS Difference,
                status AS Status, notes AS Notes,
                created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM {tenantSchema}.cash_registers WHERE id = @Id";

        return await connection.QuerySingleOrDefaultAsync<CashRegister>(sql, new { Id = id });
    }

    public async Task<CashRegister?> GetOpenByTerminalAsync(string tenantSchema, Guid storeId, int terminalNumber, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        var sql = $@"
            SELECT
                id AS Id, store_id AS StoreId, terminal_number AS TerminalNumber,
                operator_id AS OperatorId, operator_name AS OperatorName,
                opened_at AS OpenedAt, closed_at AS ClosedAt,
                opening_balance AS OpeningBalance, closing_balance AS ClosingBalance,
                total_sales AS TotalSales, total_cash AS TotalCash,
                total_debit AS TotalDebit, total_credit AS TotalCredit,
                total_pix AS TotalPix, total_others AS TotalOthers,
                quantity_sales AS QuantitySales,
                total_sangria AS TotalSangria, total_suprimento AS TotalSuprimento,
                expected_balance AS ExpectedBalance, difference AS Difference,
                status AS Status, notes AS Notes,
                created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM {tenantSchema}.cash_registers
            WHERE store_id = @StoreId AND terminal_number = @TerminalNumber AND status = 'open'";

        return await connection.QuerySingleOrDefaultAsync<CashRegister>(sql, new { StoreId = storeId, TerminalNumber = terminalNumber });
    }

    public async Task<(List<CashRegister> Items, int TotalCount)> ListAsync(
        string tenantSchema,
        Guid? storeId = null,
        int? terminalNumber = null,
        string? status = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (storeId.HasValue) { conditions.Add("store_id = @StoreId"); parameters.Add("StoreId", storeId.Value); }
        if (terminalNumber.HasValue) { conditions.Add("terminal_number = @TerminalNumber"); parameters.Add("TerminalNumber", terminalNumber.Value); }
        if (!string.IsNullOrEmpty(status)) { conditions.Add("status = @Status"); parameters.Add("Status", status); }
        if (dateFrom.HasValue) { conditions.Add("opened_at >= @DateFrom"); parameters.Add("DateFrom", dateFrom.Value); }
        if (dateTo.HasValue) { conditions.Add("opened_at <= @DateTo"); parameters.Add("DateTo", dateTo.Value); }

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
        parameters.Add("Limit", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        var countSql = $"SELECT COUNT(*) FROM {tenantSchema}.cash_registers {where}";
        var dataSql = $@"
            SELECT
                id AS Id, store_id AS StoreId, terminal_number AS TerminalNumber,
                operator_id AS OperatorId, operator_name AS OperatorName,
                opened_at AS OpenedAt, closed_at AS ClosedAt,
                opening_balance AS OpeningBalance, closing_balance AS ClosingBalance,
                total_sales AS TotalSales, total_cash AS TotalCash,
                total_debit AS TotalDebit, total_credit AS TotalCredit,
                total_pix AS TotalPix, total_others AS TotalOthers,
                quantity_sales AS QuantitySales,
                total_sangria AS TotalSangria, total_suprimento AS TotalSuprimento,
                expected_balance AS ExpectedBalance, difference AS Difference,
                status AS Status, notes AS Notes,
                created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM {tenantSchema}.cash_registers {where}
            ORDER BY opened_at DESC
            LIMIT @Limit OFFSET @Offset";

        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var items = (await connection.QueryAsync<CashRegister>(dataSql, parameters)).ToList();

        return (items, total);
    }

    public async Task SaveAsync(string tenantSchema, CashRegister entity, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        var sql = $@"
            INSERT INTO {tenantSchema}.cash_registers
                (id, store_id, terminal_number, operator_id, operator_name,
                 opened_at, opening_balance, total_sangria, total_suprimento,
                 status, notes, created_at, updated_at)
            VALUES
                (@Id, @StoreId, @TerminalNumber, @OperatorId, @OperatorName,
                 @OpenedAt, @OpeningBalance, 0, 0,
                 @Status, @Notes, @CreatedAt, @UpdatedAt)";

        await connection.ExecuteAsync(sql, new
        {
            entity.Id, entity.StoreId, entity.TerminalNumber, entity.OperatorId,
            entity.OperatorName, entity.OpenedAt, entity.OpeningBalance,
            entity.Status, entity.Notes, entity.CreatedAt, entity.UpdatedAt
        });
    }

    public async Task UpdateAsync(string tenantSchema, CashRegister entity, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        var sql = $@"
            UPDATE {tenantSchema}.cash_registers SET
                closed_at = @ClosedAt,
                closing_balance = @ClosingBalance,
                total_sales = @TotalSales,
                total_cash = @TotalCash,
                total_debit = @TotalDebit,
                total_credit = @TotalCredit,
                total_pix = @TotalPix,
                total_others = @TotalOthers,
                quantity_sales = @QuantitySales,
                total_sangria = @TotalSangria,
                total_suprimento = @TotalSuprimento,
                expected_balance = @ExpectedBalance,
                difference = @Difference,
                status = @Status,
                notes = @Notes,
                updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, new
        {
            entity.ClosedAt, entity.ClosingBalance,
            entity.TotalSales, entity.TotalCash, entity.TotalDebit,
            entity.TotalCredit, entity.TotalPix, entity.TotalOthers,
            entity.QuantitySales, entity.TotalSangria, entity.TotalSuprimento,
            entity.ExpectedBalance, entity.Difference,
            entity.Status, entity.Notes, entity.UpdatedAt, entity.Id
        });
    }

    public async Task SaveMovementAsync(string tenantSchema, CashRegisterMovement movement, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        var sql = $@"
            INSERT INTO {tenantSchema}.cash_register_movements
                (id, cash_register_id, store_id, type, amount, operator_id, operator_name, notes, occurred_at, created_at)
            VALUES
                (@Id, @CashRegisterId, @StoreId, @Type, @Amount, @OperatorId, @OperatorName, @Notes, @OccurredAt, @CreatedAt)";

        await connection.ExecuteAsync(sql, new
        {
            movement.Id, movement.CashRegisterId, movement.StoreId,
            movement.Type, movement.Amount, movement.OperatorId,
            movement.OperatorName, movement.Notes, movement.OccurredAt, movement.CreatedAt
        });
    }

    public async Task<List<CashRegisterMovement>> GetMovementsAsync(string tenantSchema, Guid cashRegisterId, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        var sql = $@"
            SELECT
                id AS Id, cash_register_id AS CashRegisterId, store_id AS StoreId,
                type AS Type, amount AS Amount, operator_id AS OperatorId,
                operator_name AS OperatorName, notes AS Notes,
                occurred_at AS OccurredAt, created_at AS CreatedAt
            FROM {tenantSchema}.cash_register_movements
            WHERE cash_register_id = @CashRegisterId
            ORDER BY occurred_at ASC";

        return (await connection.QueryAsync<CashRegisterMovement>(sql, new { CashRegisterId = cashRegisterId })).ToList();
    }

    public async Task UpsertFromAgentAsync(string tenantSchema, SyncCashRegisterRequest payload, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        var status = payload.Status?.ToLower() == "fechado" ? "closed" : "open";

        var sql = $@"
            INSERT INTO {tenantSchema}.cash_registers
                (id, store_id, terminal_number, operator_id, operator_name,
                 opened_at, closed_at, opening_balance, closing_balance,
                 total_sales, total_cash, total_debit, total_credit, total_pix, total_others,
                 quantity_sales, total_sangria, total_suprimento,
                 difference, status, notes, created_at, updated_at)
            VALUES
                (@Id, @StoreId, @TerminalNumber, @OperatorId, @OperatorName,
                 @OpenedAt, @ClosedAt, @OpeningBalance, @ClosingBalance,
                 @TotalSales, @TotalCash, @TotalDebit, @TotalCredit, @TotalPix, @TotalOthers,
                 @QuantitySales, @TotalSangria, @TotalSuprimento,
                 @Difference, @Status, @Notes, NOW(), NOW())
            ON CONFLICT (id) DO UPDATE SET
                closed_at = EXCLUDED.closed_at,
                closing_balance = EXCLUDED.closing_balance,
                total_sales = EXCLUDED.total_sales,
                total_cash = EXCLUDED.total_cash,
                total_debit = EXCLUDED.total_debit,
                total_credit = EXCLUDED.total_credit,
                total_pix = EXCLUDED.total_pix,
                total_others = EXCLUDED.total_others,
                quantity_sales = EXCLUDED.quantity_sales,
                total_sangria = EXCLUDED.total_sangria,
                total_suprimento = EXCLUDED.total_suprimento,
                difference = EXCLUDED.difference,
                status = EXCLUDED.status,
                notes = EXCLUDED.notes,
                updated_at = NOW()";

        await connection.ExecuteAsync(sql, new
        {
            payload.Id,
            payload.StoreId,
            TerminalNumber = payload.TerminalNumber,
            OperatorId = payload.OperatorId,
            OperatorName = payload.OperatorName,
            OpenedAt = payload.OpenedAt,
            ClosedAt = payload.ClosedAt,
            OpeningBalance = payload.OpeningBalance,
            ClosingBalance = payload.ClosingBalance,
            TotalSales = payload.TotalSales,
            TotalCash = payload.TotalCash,
            TotalDebit = payload.TotalDebit,
            TotalCredit = payload.TotalCredit,
            TotalPix = payload.TotalPix,
            TotalOthers = payload.TotalOthers,
            QuantitySales = payload.QuantitySales,
            TotalSangria = payload.TotalSangria,
            TotalSuprimento = payload.TotalSuprimento,
            Difference = payload.Difference,
            Status = status,
            Notes = payload.Notes
        });
    }
}
