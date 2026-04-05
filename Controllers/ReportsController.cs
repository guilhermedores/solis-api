using Microsoft.AspNetCore.Mvc;
using SolisApi.Middleware;
using SolisApi.Services;

namespace SolisApi.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reportService;

    public ReportsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Get all available reports
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReports([FromQuery] string? category = null)
    {
        var reports = await _reportService.GetAllReportsAsync(category);

        return Ok(new
        {
            reports = reports.Select(r => new
            {
                r.Name,
                r.DisplayName,
                r.Description,
                r.Category
            })
        });
    }

    /// <summary>
    /// Get report metadata (fields, filters)
    /// </summary>
    [HttpGet("{reportName}/metadata")]
    public async Task<IActionResult> GetReportMetadata(string reportName)
    {
        var report = await _reportService.GetReportMetadataAsync(reportName);

        if (report == null)
            return NotFound(new { error = "Report not found" });

        return Ok(new
        {
            report.Name,
            report.DisplayName,
            report.Description,
            report.Category,
            fields = report.Fields.Select(f => new
            {
                f.Name,
                f.DisplayName,
                f.FieldType,
                f.FormatMask,
                f.Aggregation,
                f.Visible,
                f.Sortable,
                f.Filterable
            }),
            filters = report.Filters.Select(f => new
            {
                f.Name,
                f.DisplayName,
                f.FieldType,
                f.FilterType,
                f.DefaultValue,
                f.Required,
                options = f.Options.Select(o => new { o.Value, o.Label })
            })
        });
    }

    /// <summary>
    /// Execute report with filters
    /// </summary>
    [HttpPost("{reportName}/execute")]
    public async Task<IActionResult> ExecuteReport(string reportName, [FromBody] ReportExecutionRequest request)
    {
        try
        {
            var (data, totalRecords) = await _reportService.ExecuteReportAsync(
                reportName,
                request.Filters ?? new Dictionary<string, object>(),
                request.Page,
                request.PageSize,
                request.SortBy,
                request.SortDirection
            );

            return Ok(new
            {
                data,
                pagination = new
                {
                    page = request.Page,
                    pageSize = request.PageSize,
                    totalRecords,
                    totalPages = (int)Math.Ceiling(totalRecords / (double)request.PageSize)
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Export report to CSV
    /// </summary>
    [HttpPost("{reportName}/export")]
    public async Task<IActionResult> ExportReport(string reportName, [FromBody] ReportExecutionRequest request)
    {
        var (data, _) = await _reportService.ExecuteReportAsync(
            reportName,
            request.Filters ?? new Dictionary<string, object>(),
            1,
            10000,
            request.SortBy,
            request.SortDirection
        );

        var report = await _reportService.GetReportMetadataAsync(reportName);
        if (report == null)
            return NotFound(new { error = "Report not found" });

        var csv = GenerateCsv(data, report.Fields.Where(f => f.Visible).OrderBy(f => f.DisplayOrder).ToList());
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

        return File(bytes, "text/csv", $"{reportName}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    private string GenerateCsv(List<Dictionary<string, object>> data, List<Models.Reports.ReportField> fields)
    {
        var csv = new System.Text.StringBuilder();

        csv.AppendLine(string.Join(",", fields.Select(f => $"\"{f.DisplayName}\"")));

        foreach (var row in data)
        {
            var values = fields.Select(f =>
            {
                var value = row.ContainsKey(f.Name) ? row[f.Name] : null;
                if (value == null || value == DBNull.Value)
                    return string.Empty;

                var stringValue = value.ToString() ?? string.Empty;
                if (stringValue.Contains(',') || stringValue.Contains('"'))
                    return $"\"{stringValue.Replace("\"", "\"\"")}\"";

                return stringValue;
            });

            csv.AppendLine(string.Join(",", values));
        }

        return csv.ToString();
    }
}

public class ReportExecutionRequest
{
    public Dictionary<string, object>? Filters { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "ASC";
}
