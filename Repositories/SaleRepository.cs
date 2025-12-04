using Dapper;
using Microsoft.EntityFrameworkCore;
using SolisApi.Data;
using SolisApi.Models;
using System.Data;
using System.Reflection;

namespace SolisApi.Repositories;

/// <summary>
/// Repository for Sale aggregate persistence
/// Uses Dapper for clean SQL with dynamic tenant schemas
/// </summary>
public class SaleRepository : ISaleRepository
{
    private readonly SolisDbContext _context;

    public SaleRepository(SolisDbContext context)
    {
        _context = context;
    }

    public async Task<Sale?> GetByIdAsync(string tenantSchema, Guid id, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        var sql = $@"
            SELECT 
                id AS Id,
                client_sale_id AS ClientSaleId,
                store_id AS StoreId,
                pos_id AS PosId,
                operator_id AS OperatorId,
                sale_datetime AS SaleDateTime,
                status AS Status,
                subtotal AS Subtotal,
                discount_total AS DiscountTotal,
                tax_total AS TaxTotal,
                total AS Total,
                payment_status AS PaymentStatus,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM {tenantSchema}.sales WHERE id = @Id";
        var sale = await connection.QuerySingleOrDefaultAsync<Sale>(sql, new { Id = id });

        if (sale != null)
        {
            await LoadAggregateAsync(connection, tenantSchema, sale, cancellationToken);
        }

        return sale;
    }

    public async Task<Sale?> GetByClientSaleIdAsync(string tenantSchema, Guid clientSaleId, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        var sql = $"SELECT * FROM {tenantSchema}.sales WHERE client_sale_id = @ClientSaleId";
        var sale = await connection.QuerySingleOrDefaultAsync<Sale>(sql, new { ClientSaleId = clientSaleId });

        if (sale != null)
        {
            await LoadAggregateAsync(connection, tenantSchema, sale, cancellationToken);
        }

        return sale;
    }

    public async Task<(List<Sale> Sales, int TotalCount)> GetAllAsync(
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
        CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (storeId.HasValue)
        {
            conditions.Add("store_id = @StoreId");
            parameters.Add("StoreId", storeId.Value);
        }
        if (posId.HasValue)
        {
            conditions.Add("pos_id = @PosId");
            parameters.Add("PosId", posId.Value);
        }
        if (operatorId.HasValue)
        {
            conditions.Add("operator_id = @OperatorId");
            parameters.Add("OperatorId", operatorId.Value);
        }
        if (dateFrom.HasValue)
        {
            conditions.Add("sale_datetime >= @DateFrom");
            parameters.Add("DateFrom", dateFrom.Value);
        }
        if (dateTo.HasValue)
        {
            conditions.Add("sale_datetime <= @DateTo");
            parameters.Add("DateTo", dateTo.Value);
        }
        if (!string.IsNullOrEmpty(status))
        {
            conditions.Add("status = @Status");
            parameters.Add("Status", status);
        }
        if (clientSaleId.HasValue)
        {
            conditions.Add("client_sale_id = @ClientSaleId");
            parameters.Add("ClientSaleId", clientSaleId.Value);
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";

        // Count total
        var countSql = $"SELECT COUNT(*) FROM {tenantSchema}.sales {whereClause}";
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        // Get paginated data
        var offset = (page - 1) * pageSize;
        parameters.Add("Limit", pageSize);
        parameters.Add("Offset", offset);

        var dataSql = $@"
            SELECT 
                id AS Id,
                client_sale_id AS ClientSaleId,
                store_id AS StoreId,
                pos_id AS PosId,
                operator_id AS OperatorId,
                sale_datetime AS SaleDateTime,
                status AS Status,
                subtotal AS Subtotal,
                discount_total AS DiscountTotal,
                tax_total AS TaxTotal,
                total AS Total,
                payment_status AS PaymentStatus,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt
            FROM {tenantSchema}.sales 
            {whereClause}
            ORDER BY sale_datetime DESC 
            LIMIT @Limit OFFSET @Offset";

        var sales = (await connection.QueryAsync<Sale>(dataSql, parameters)).ToList();

        // Load aggregate data for each sale
        foreach (var sale in sales)
        {
            await LoadAggregateAsync(connection, tenantSchema, sale, cancellationToken);
        }

        return (sales, totalCount);
    }

    public async Task SaveAsync(string tenantSchema, Sale sale, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        System.Data.Common.DbTransaction? transaction = null;
        try
        {
            transaction = (connection as System.Data.Common.DbConnection)!.BeginTransaction();
            // Save sale
            var saleSql = $@"
                INSERT INTO {tenantSchema}.sales 
                (id, client_sale_id, store_id, pos_id, operator_id, sale_datetime, status, 
                 subtotal, discount_total, tax_total, total, payment_status, created_at, updated_at)
                VALUES (@Id, @ClientSaleId, @StoreId, @PosId, @OperatorId, @SaleDateTime, @Status, 
                        @Subtotal, @DiscountTotal, @TaxTotal, @Total, @PaymentStatus, @CreatedAt, @UpdatedAt)";

            await connection.ExecuteAsync(saleSql, new
            {
                sale.Id,
                sale.ClientSaleId,
                sale.StoreId,
                sale.PosId,
                sale.OperatorId,
                sale.SaleDateTime,
                sale.Status,
                sale.Subtotal,
                sale.DiscountTotal,
                sale.TaxTotal,
                sale.Total,
                sale.PaymentStatus,
                sale.CreatedAt,
                sale.UpdatedAt
            }, transaction);

            // Save items and taxes
            foreach (var item in sale.Items)
            {
                var itemSql = $@"
                    INSERT INTO {tenantSchema}.sale_items 
                    (id, sale_id, product_id, sku, description, quantity, unit_price, discount_amount, tax_amount, total, created_at)
                    VALUES (@Id, @SaleId, @ProductId, @Sku, @Description, @Quantity, @UnitPrice, @DiscountAmount, @TaxAmount, @Total, @CreatedAt)";

                await connection.ExecuteAsync(itemSql, new
                {
                    item.Id,
                    item.SaleId,
                    item.ProductId,
                    item.Sku,
                    item.Description,
                    item.Quantity,
                    item.UnitPrice,
                    item.DiscountAmount,
                    item.TaxAmount,
                    item.Total,
                    item.CreatedAt
                }, transaction);

                // Save taxes for item
                foreach (var tax in item.Taxes)
                {
                    var taxSql = $@"
                        INSERT INTO {tenantSchema}.sale_taxes 
                        (id, sale_item_id, tax_type_id, tax_rule_id, base_amount, rate, amount, created_at)
                        VALUES (@Id, @SaleItemId, @TaxTypeId, @TaxRuleId, @BaseAmount, @Rate, @Amount, @CreatedAt)";

                    await connection.ExecuteAsync(taxSql, new
                    {
                        tax.Id,
                        tax.SaleItemId,
                        tax.TaxTypeId,
                        tax.TaxRuleId,
                        tax.BaseAmount,
                        tax.Rate,
                        tax.Amount,
                        tax.CreatedAt
                    }, transaction);
                }
            }

            // Save payments
            foreach (var payment in sale.Payments)
            {
                var newPaymentSql = $@"
                    INSERT INTO {tenantSchema}.sale_payments 
                    (id, sale_id, payment_method_id, amount, acquirer_txn_id, authorization_code, change_amount, status, processed_at, created_at)
                    VALUES (@Id, @SaleId, @PaymentMethodId, @Amount, @AcquirerTxnId, @AuthorizationCode, @ChangeAmount, @Status, @ProcessedAt, @CreatedAt)";

                await connection.ExecuteAsync(newPaymentSql, new
                {
                    payment.Id,
                    payment.SaleId,
                    payment.PaymentMethodId,
                    payment.Amount,
                    payment.AcquirerTxnId,
                    payment.AuthorizationCode,
                    payment.ChangeAmount,
                    payment.Status,
                    payment.ProcessedAt,
                    payment.CreatedAt
                }, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction?.Rollback();
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    public async Task UpdateAsync(string tenantSchema, Sale sale, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        System.Data.Common.DbTransaction? transaction = null;
        try
        {
            transaction = (connection as System.Data.Common.DbConnection)!.BeginTransaction();
            // Update sale
            var updateSql = $@"
                UPDATE {tenantSchema}.sales SET 
                    status = @Status, 
                    payment_status = @PaymentStatus, 
                    subtotal = @Subtotal, 
                    discount_total = @DiscountTotal, 
                    tax_total = @TaxTotal, 
                    total = @Total, 
                    updated_at = @UpdatedAt 
                WHERE id = @Id";

            await connection.ExecuteAsync(updateSql, new
            {
                sale.Status,
                sale.PaymentStatus,
                sale.Subtotal,
                sale.DiscountTotal,
                sale.TaxTotal,
                sale.Total,
                sale.UpdatedAt,
                sale.Id
            }, transaction);

            // Get existing payment IDs
            var existingPaymentIdsSql = $"SELECT id FROM {tenantSchema}.sale_payments WHERE sale_id = @SaleId";
            var existingPaymentIds = (await connection.QueryAsync<Guid>(existingPaymentIdsSql, 
                new { SaleId = sale.Id }, transaction)).ToList();

            // Save new payments
            foreach (var payment in sale.Payments.Where(p => !existingPaymentIds.Contains(p.Id)))
            {
                var paymentSql = $@"
                    INSERT INTO {tenantSchema}.sale_payments 
                    (id, sale_id, payment_method_id, amount, acquirer_txn_id, authorization_code, change_amount, status, processed_at, created_at)
                    VALUES (@Id, @SaleId, @PaymentMethodId, @Amount, @AcquirerTxnId, @AuthorizationCode, @ChangeAmount, @Status, @ProcessedAt, @CreatedAt)";

                await connection.ExecuteAsync(paymentSql, new
                {
                    payment.Id,
                    payment.SaleId,
                    payment.PaymentMethodId,
                    payment.Amount,
                    payment.AcquirerTxnId,
                    payment.AuthorizationCode,
                    payment.ChangeAmount,
                    payment.Status,
                    payment.ProcessedAt,
                    payment.CreatedAt
                }, transaction);
            }

            // Save cancellation if exists and is new
            if (sale.Cancellation != null)
            {
                var checkCancellationSql = $"SELECT COUNT(*) FROM {tenantSchema}.sale_cancellations WHERE sale_id = @SaleId";
                var cancellationExists = await connection.ExecuteScalarAsync<int>(checkCancellationSql, 
                    new { SaleId = sale.Id }, transaction) > 0;

                if (!cancellationExists)
                {
                    var cancellationSql = $@"
                        INSERT INTO {tenantSchema}.sale_cancellations 
                        (id, sale_id, reason, canceled_at, source, cancellation_type, refund_amount, created_at)
                        VALUES (@Id, @SaleId, @Reason, @CanceledAt, @Source, @CancellationType, @RefundAmount, @CreatedAt)";

                    await connection.ExecuteAsync(cancellationSql, new
                    {
                        sale.Cancellation.Id,
                        sale.Cancellation.SaleId,
                        sale.Cancellation.Reason,
                        sale.Cancellation.CanceledAt,
                        sale.Cancellation.Source,
                        sale.Cancellation.CancellationType,
                        sale.Cancellation.RefundAmount,
                        sale.Cancellation.CreatedAt
                    }, transaction);
                }
            }

            transaction.Commit();
        }
        catch
        {
            transaction?.Rollback();
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    private async Task LoadAggregateAsync(IDbConnection connection, string tenantSchema, Sale sale, CancellationToken cancellationToken)
    {
        // Load items with unit of measure from products - explicit columns to avoid mapping issues
        var itemsSql = $@"
            SELECT 
                si.id AS Id,
                si.sale_id AS SaleId,
                si.product_id AS ProductId,
                si.sku AS Sku,
                si.description AS Description,
                si.quantity AS Quantity,
                si.unit_price AS UnitPrice,
                si.discount_amount AS DiscountAmount,
                si.tax_amount AS TaxAmount,
                si.total AS Total,
                si.created_at AS CreatedAt,
                u.code AS UnitOfMeasure
            FROM {tenantSchema}.sale_items si
            LEFT JOIN {tenantSchema}.products p ON si.product_id = p.id
            LEFT JOIN {tenantSchema}.unit_of_measures u ON p.unit_of_measure_id = u.id
            WHERE si.sale_id = @SaleId";
        var items = (await connection.QueryAsync<SaleItem>(itemsSql, new { SaleId = sale.Id })).ToList();

        foreach (var item in items)
        {
            // Load taxes for item
            var taxesSql = $"SELECT * FROM {tenantSchema}.sale_taxes WHERE sale_item_id = @SaleItemId";
            var taxes = (await connection.QueryAsync<SaleTax>(taxesSql, new { SaleItemId = item.Id })).ToList();

            // Use reflection to set private collection
            var taxesField = typeof(SaleItem).GetField("_taxes", BindingFlags.NonPublic | BindingFlags.Instance);
            if (taxesField != null)
            {
                var taxesList = (List<SaleTax>)taxesField.GetValue(item)!;
                taxesList.AddRange(taxes);
            }
        }

        // Load payments
        var paymentsSql = $"SELECT * FROM {tenantSchema}.sale_payments WHERE sale_id = @SaleId";
        var payments = (await connection.QueryAsync<SalePayment>(paymentsSql, new { SaleId = sale.Id })).ToList();

        // Load cancellation
        var cancellationSql = $"SELECT * FROM {tenantSchema}.sale_cancellations WHERE sale_id = @SaleId";
        var cancellation = await connection.QuerySingleOrDefaultAsync<SaleCancellation>(cancellationSql, new { SaleId = sale.Id });

        // Use reflection to populate aggregate
        var itemsField = typeof(Sale).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance);
        if (itemsField != null)
        {
            var itemsList = (List<SaleItem>)itemsField.GetValue(sale)!;
            itemsList.AddRange(items);
        }

        var paymentsField = typeof(Sale).GetField("_payments", BindingFlags.NonPublic | BindingFlags.Instance);
        if (paymentsField != null)
        {
            var paymentsList = (List<SalePayment>)paymentsField.GetValue(sale)!;
            paymentsList.AddRange(payments);
        }

        if (cancellation != null)
        {
            var cancellationProp = typeof(Sale).GetProperty("Cancellation")!;
            cancellationProp.SetValue(sale, cancellation);
        }
    }

    public async Task<ProductInfo?> GetProductByIdAsync(string tenantSchema, Guid productId, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        var sql = $@"
            SELECT 
                id as Id,
                internal_code as Sku,
                description as Description
            FROM {tenantSchema}.products 
            WHERE id = @ProductId";

        return await connection.QuerySingleOrDefaultAsync<ProductInfo>(sql, new { ProductId = productId });
    }

    public async Task<PaymentMethodInfo?> GetPaymentMethodByIdAsync(string tenantSchema, Guid paymentMethodId, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        var sql = $@"
            SELECT 
                pm.id as Id,
                pt.code as PaymentTypeCode,
                pm.description as Description
            FROM {tenantSchema}.payment_methods pm
            JOIN {tenantSchema}.payment_types pt ON pm.payment_type_id = pt.id
            WHERE pm.id = @PaymentMethodId";

        return await connection.QuerySingleOrDefaultAsync<PaymentMethodInfo>(sql, new { PaymentMethodId = paymentMethodId });
    }

    private static async Task EnsureOpenAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            if (connection is System.Data.Common.DbConnection dbConnection)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }
            else
            {
                connection.Open();
            }
        }
    }
}
