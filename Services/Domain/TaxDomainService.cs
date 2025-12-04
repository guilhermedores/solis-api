using Microsoft.EntityFrameworkCore;
using Npgsql;
using SolisApi.Data;
using SolisApi.Models;
using System.Data;

namespace SolisApi.Services.Domain;

/// <summary>
/// Domain Service for tax calculation
/// Encapsulates complex tax business rules
/// </summary>
public class TaxDomainService : ITaxDomainService
{
    private readonly SolisDbContext _context;
    private readonly ILogger<TaxDomainService> _logger;

    public TaxDomainService(
        SolisDbContext context,
        ILogger<TaxDomainService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SaleTax>> CalculateTaxesForItemAsync(
        string tenantSchema,
        Guid productId,
        decimal baseAmount,
        string state,
        CancellationToken cancellationToken = default)
    {
        var taxes = new List<SaleTax>();

        // tax_types e tax_rules são gerenciados via CRUD genérico
        // Buscar via SQL direto
        var sql = $@"
            SELECT 
                tt.id as tax_type_id,
                tt.code,
                tt.calculation_type,
                tr.id as tax_rule_id,
                tr.rate,
                tr.base_reduction_rate,
                tr.mva_rate
            FROM {tenantSchema}.tax_types tt
            LEFT JOIN LATERAL (
                SELECT id, rate, base_reduction_rate, mva_rate
                FROM {tenantSchema}.tax_rules
                WHERE tax_type_id = tt.id
                  AND active = true
                  AND active_from <= @now
                  AND (active_to IS NULL OR active_to >= @now)
                  AND (
                      (product_id = @productId AND state = @state) OR
                      (product_id IS NULL AND state = @state) OR
                      (product_id IS NULL AND state IS NULL)
                  )
                ORDER BY 
                    CASE WHEN product_id IS NOT NULL THEN 2 ELSE 0 END DESC,
                    CASE WHEN state IS NOT NULL THEN 1 ELSE 0 END DESC,
                    active_from DESC
                LIMIT 1
            ) tr ON true
            WHERE tt.active = true";

        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            using var cmd = _context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new NpgsqlParameter("@now", DateTime.UtcNow.Date));
            cmd.Parameters.Add(new NpgsqlParameter("@productId", productId));
            cmd.Parameters.Add(new NpgsqlParameter("@state", state));

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var taxTypeId = reader.GetGuid(reader.GetOrdinal("tax_type_id"));
                var code = reader.GetString(reader.GetOrdinal("code"));
                var calculationType = reader.GetString(reader.GetOrdinal("calculation_type"));
                
                var taxRuleIdOrdinal = reader.GetOrdinal("tax_rule_id");
                Guid? taxRuleId = reader.IsDBNull(taxRuleIdOrdinal) ? null : reader.GetGuid(taxRuleIdOrdinal);
                
                if (taxRuleId == null)
                {
                    _logger.LogWarning("No tax rule found for {Code} in state {State} for product {ProductId}", 
                        code, state, productId);
                    continue; // Skip if no rule found
                }

                var rate = reader.GetDecimal(reader.GetOrdinal("rate"));
                
                var baseReductionOrdinal = reader.GetOrdinal("base_reduction_rate");
                decimal? baseReductionRate = reader.IsDBNull(baseReductionOrdinal) ? null : reader.GetDecimal(baseReductionOrdinal);
                
                var mvaOrdinal = reader.GetOrdinal("mva_rate");
                decimal? mvaRate = reader.IsDBNull(mvaOrdinal) ? null : reader.GetDecimal(mvaOrdinal);

                // Calculate tax amount based on calculation type
                var taxAmount = CalculateTaxAmount(baseAmount, rate, baseReductionRate, mvaRate, calculationType);

                taxes.Add(SaleTax.Create(taxTypeId, taxRuleId, baseAmount, rate, taxAmount));
            }
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }

        return taxes;
    }

    private decimal CalculateTaxAmount(
        decimal baseAmount,
        decimal rate,
        decimal? baseReductionRate,
        decimal? mvaRate,
        string calculationType)
    {
        return calculationType switch
        {
            "percentage" => decimal.Round(baseAmount * rate / 100, 2),
            
            "fixed" => rate,
            
            "mva" when mvaRate.HasValue =>
                decimal.Round(baseAmount * (1 + mvaRate.Value / 100) * rate / 100, 2),
            
            "reduced_base" when baseReductionRate.HasValue =>
                decimal.Round(baseAmount * (1 - baseReductionRate.Value / 100) * rate / 100, 2),
            
            _ => decimal.Round(baseAmount * rate / 100, 2) // Default to percentage
        };
    }
}
