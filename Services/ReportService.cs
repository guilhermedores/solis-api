using Microsoft.EntityFrameworkCore;
using Npgsql;
using SolisApi.Data;
using SolisApi.Models.Reports;
using System.Data;
using System.Text;
using Dapper;

namespace SolisApi.Services;

public class ReportService
{
    private readonly SolisDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ReportService(SolisDbContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    private string GetTenantSubdomain()
    {
        return _httpContextAccessor.HttpContext?.Items["TenantSubdomain"]?.ToString() 
            ?? throw new InvalidOperationException("Tenant subdomain not found in context");
    }

    private string GetConnectionString()
    {
        var baseConnectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
        
        var tenantSubdomain = GetTenantSubdomain();
        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            SearchPath = $"tenant_{tenantSubdomain},public"
        };
        return builder.ToString();
    }

    public async Task<Report?> GetReportMetadataAsync(string reportName)
    {
        using var connection = new NpgsqlConnection(GetConnectionString());
        
        // Get report
        var report = await connection.QueryFirstOrDefaultAsync<Report>(@"
            SELECT id as Id, name as Name, display_name as DisplayName, description as Description, 
                   category as Category, base_table as BaseTable, base_query as BaseQuery, 
                   active as Active
            FROM reports 
            WHERE name = @Name AND active = true",
            new { Name = reportName });
        
        if (report == null) return null;
        
        // Get fields
        var fields = await connection.QueryAsync<ReportField>(@"
            SELECT id as Id, report_id as ReportId, name as Name, display_name as DisplayName, 
                   field_type as FieldType, data_source as DataSource, format_mask as FormatMask, 
                   aggregation as Aggregation, display_order as DisplayOrder, visible as Visible, 
                   sortable as Sortable, filterable as Filterable
            FROM report_fields 
            WHERE report_id = @ReportId 
            ORDER BY display_order",
            new { ReportId = report.Id });
        
        report.Fields = fields.ToList();
        
        // Get filters
        var filters = await connection.QueryAsync<ReportFilter>(@"
            SELECT id as Id, report_id as ReportId, name as Name, display_name as DisplayName, 
                   field_type as FieldType, filter_type as FilterType, data_source as DataSource, 
                   default_value as DefaultValue, required as Required, display_order as DisplayOrder
            FROM report_filters 
            WHERE report_id = @ReportId 
            ORDER BY display_order",
            new { ReportId = report.Id });
        
        report.Filters = filters.ToList();
        
        // Get filter options for each filter
        foreach (var filter in report.Filters)
        {
            var options = await connection.QueryAsync<ReportFilterOption>(@"
                SELECT id as Id, filter_id as FilterId, value as Value, label as Label, 
                       display_order as DisplayOrder
                FROM report_filter_options 
                WHERE filter_id = @FilterId 
                ORDER BY display_order",
                new { FilterId = filter.Id });
            
            filter.Options = options.ToList();
        }
        
        return report;
    }

    public async Task<List<Report>> GetAllReportsAsync(string? category = null)
    {
        using var connection = new NpgsqlConnection(GetConnectionString());
        
        var sql = @"SELECT id as Id, name as Name, display_name as DisplayName, description as Description, 
                           category as Category, base_table as BaseTable, base_query as BaseQuery, active as Active
                    FROM reports 
                    WHERE active = true";
        
        if (!string.IsNullOrEmpty(category))
        {
            sql += " AND category = @Category";
        }
        
        sql += " ORDER BY category, display_name";
        
        var reports = await connection.QueryAsync<Report>(sql, new { Category = category });
        
        return reports.ToList();
    }

    public async Task<(List<Dictionary<string, object>> Data, int TotalRecords)> ExecuteReportAsync(
        string reportName,
        Dictionary<string, object> filters,
        int page = 1,
        int pageSize = 50,
        string? sortBy = null,
        string sortDirection = "ASC")
    {
        var report = await GetReportMetadataAsync(reportName);
        if (report == null)
        {
            throw new InvalidOperationException($"Report '{reportName}' not found or inactive");
        }

        using var connection = new NpgsqlConnection(GetConnectionString());

        // Build SELECT clause with fields
        var selectFields = string.Join(", ", report.Fields
            .Where(f => f.Visible)
            .OrderBy(f => f.DisplayOrder)
            .Select(f => $"{f.DataSource} AS \"{f.Name}\""));

        // Use base_query directly if provided, otherwise use base_table
        var fromClause = !string.IsNullOrEmpty(report.BaseQuery)
            ? $"FROM ({report.BaseQuery}) AS base_data"
            : $"FROM {report.BaseTable}";

        // Build WHERE clause
        var whereConditions = new List<string>();
        var parameters = new DynamicParameters();
        var paramIndex = 1;

        foreach (var kvp in filters)
        {
            var filter = report.Filters.FirstOrDefault(f => f.Name == kvp.Key);
            if (filter == null || kvp.Value == null || string.IsNullOrEmpty(kvp.Value.ToString())) continue;

            var condition = BuildWhereCondition(filter, kvp.Value, parameters, ref paramIndex);
            if (!string.IsNullOrEmpty(condition))
            {
                whereConditions.Add(condition);
            }
        }

        var whereClause = whereConditions.Any() ? "WHERE " + string.Join(" AND ", whereConditions) : "";

        // Build ORDER BY
        var orderByClause = !string.IsNullOrEmpty(sortBy)
            ? $"ORDER BY {sortBy} {sortDirection}"
            : "";

        // Count query
        var countSql = $"SELECT COUNT(*) {fromClause} {whereClause}";
        var totalRecords = await connection.QuerySingleAsync<int>(countSql, parameters);

        // Data query with pagination
        var offset = (page - 1) * pageSize;
        var dataSql = $"SELECT {selectFields} {fromClause} {whereClause} {orderByClause} LIMIT {pageSize} OFFSET {offset}";
        
        var rows = await connection.QueryAsync(dataSql, parameters);
        var data = rows.Select(row => ((IDictionary<string, object>)row).ToDictionary(k => k.Key, v => v.Value)).ToList();

        return (data, totalRecords);
    }

    private string BuildWhereCondition(ReportFilter filter, object value, DynamicParameters parameters, ref int paramIndex)
    {
        var fieldName = filter.DataSource;
        var paramName = $"p{paramIndex}";

        // Convert string values to appropriate types based on field type
        var convertedValue = filter.FieldType switch
        {
            "boolean" => value.ToString()?.ToLower() == "true",
            "number" => int.TryParse(value.ToString(), out var intVal) ? intVal : value,
            "decimal" => decimal.TryParse(value.ToString(), out var decVal) ? decVal : value,
            _ => value
        };

        switch (filter.FilterType)
        {
            case "equals":
                parameters.Add(paramName, convertedValue);
                paramIndex++;
                return $"{fieldName} = @{paramName}";

            case "contains":
                parameters.Add(paramName, $"%{value}%");
                paramIndex++;
                return $"{fieldName} ILIKE @{paramName}";

            case "greater_than":
                parameters.Add(paramName, convertedValue);
                paramIndex++;
                return $"{fieldName} > @{paramName}";

            case "less_than":
                parameters.Add(paramName, convertedValue);
                paramIndex++;
                return $"{fieldName} < @{paramName}";

            case "between":
                // Expecting value to be a Dictionary or object with from/to
                if (value is Dictionary<string, object> range && range.ContainsKey("from") && range.ContainsKey("to"))
                {
                    var paramFrom = $"p{paramIndex}";
                    var paramTo = $"p{paramIndex + 1}";
                    parameters.Add(paramFrom, range["from"]);
                    parameters.Add(paramTo, range["to"]);
                    paramIndex += 2;
                    return $"{fieldName} BETWEEN @{paramFrom} AND @{paramTo}";
                }
                return "";

            default:
                return "";
        }
    }

    public async Task<byte[]> ExportReportToCsvAsync(
        string reportName,
        Dictionary<string, object> filters)
    {
        var report = await GetReportMetadataAsync(reportName);
        if (report == null)
        {
            throw new InvalidOperationException($"Report '{reportName}' not found or inactive");
        }

        using var connection = new NpgsqlConnection(GetConnectionString());

        // Build SELECT clause
        var selectFields = string.Join(", ", report.Fields
            .Where(f => f.Visible)
            .OrderBy(f => f.DisplayOrder)
            .Select(f => $"{f.DataSource} AS \"{f.Name}\""));

        // Use base_query directly if provided, otherwise use base_table
        var fromClause = !string.IsNullOrEmpty(report.BaseQuery)
            ? $"FROM ({report.BaseQuery}) AS base_data"
            : $"FROM {report.BaseTable}";

        // Build WHERE clause
        var whereConditions = new List<string>();
        var parameters = new DynamicParameters();
        var paramIndex = 1;

        foreach (var kvp in filters)
        {
            var filter = report.Filters.FirstOrDefault(f => f.Name == kvp.Key);
            if (filter == null || kvp.Value == null || string.IsNullOrEmpty(kvp.Value.ToString())) continue;

            var condition = BuildWhereCondition(filter, kvp.Value, parameters, ref paramIndex);
            if (!string.IsNullOrEmpty(condition))
            {
                whereConditions.Add(condition);
            }
        }

        var whereClause = whereConditions.Any() ? "WHERE " + string.Join(" AND ", whereConditions) : "";

        // Execute query without pagination
        var dataSql = $"SELECT {selectFields} {fromClause} {whereClause}";
        var rows = await connection.QueryAsync(dataSql, parameters);
        var data = rows.Select(row => ((IDictionary<string, object>)row).ToDictionary(k => k.Key, v => v.Value)).ToList();

        // Generate CSV
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, System.Text.Encoding.UTF8);

        // Write header
        var visibleFields = report.Fields.Where(f => f.Visible).OrderBy(f => f.DisplayOrder).ToList();
        var header = string.Join(",", visibleFields.Select(f => EscapeCsvValue(f.DisplayName)));
        await writer.WriteLineAsync(header);

        // Write data rows
        foreach (var row in data)
        {
            var values = visibleFields.Select(f => 
            {
                var value = row.ContainsKey(f.Name) ? row[f.Name] : null;
                return EscapeCsvValue(value?.ToString() ?? "");
            });
            await writer.WriteLineAsync(string.Join(",", values));
        }

        await writer.FlushAsync();
        return memoryStream.ToArray();
    }

    private string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Escape quotes and wrap in quotes if contains comma, quote, or newline
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
