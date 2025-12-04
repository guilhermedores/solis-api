using Microsoft.AspNetCore.Mvc;
using SolisApi.DTOs;
using SolisApi.Middleware;
using SolisApi.Services;

namespace SolisApi.Controllers;

[ApiController]
[Route("api/sales")]
[RequireAuth]
public class SalesController : ControllerBase
{
    private readonly ISalesService _salesService;
    private readonly ILogger<SalesController> _logger;

    public SalesController(
        ISalesService salesService,
        ILogger<SalesController> logger)
    {
        _salesService = salesService;
        _logger = logger;
    }

    private string? GetTenantSubdomain()
    {
        return HttpContext.Items["TenantSubdomain"]?.ToString();
    }

    /// <summary>
    /// Create a new sale
    /// </summary>
    /// <param name="request">Sale creation request</param>
    /// <param name="idempotencyKey">Idempotency key (optional header: Idempotency-Key)</param>
    /// <returns>Created sale</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SaleResponse>> CreateSale(
        [FromBody] CreateSaleRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
        try
        {
            var tenantSubdomain = GetTenantSubdomain();
            if (string.IsNullOrEmpty(tenantSubdomain))
                return Unauthorized(new { error = "Tenant not found" });

            var requestId = HttpContext.Items["RequestId"]?.ToString();
            _logger.LogInformation(
                "Creating sale: RequestId={RequestId}, StoreId={StoreId}, ItemCount={ItemCount}",
                requestId, request.StoreId, request.Items.Count);

            var sale = await _salesService.CreateSaleAsync(tenantSubdomain, request, idempotencyKey);

            _logger.LogInformation(
                "Sale created: RequestId={RequestId}, SaleId={SaleId}, Total={Total}",
                requestId, sale.Id, sale.Total);

            return CreatedAtAction(nameof(GetSaleById), new { id = sale.Id }, sale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sale");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Sync multiple sales (batch operation for offline POS)
    /// </summary>
    /// <param name="request">Batch of sales to sync</param>
    /// <returns>Sync results with 207 Multi-Status</returns>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(SaleSyncResponse), StatusCodes.Status207MultiStatus)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SaleSyncResponse>> SyncSales([FromBody] SaleSyncRequest request)
    {
        try
        {
            var tenantSubdomain = GetTenantSubdomain();
            if (string.IsNullOrEmpty(tenantSubdomain))
                return Unauthorized(new { error = "Tenant not found" });

            var requestId = HttpContext.Items["RequestId"]?.ToString();
            _logger.LogInformation(
                "Syncing sales: RequestId={RequestId}, Count={Count}",
                requestId, request.Sales.Count);

            var result = await _salesService.SyncSalesAsync(tenantSubdomain, request);

            _logger.LogInformation(
                "Sales synced: RequestId={RequestId}, Total={Total}, Created={Created}, Errors={Errors}",
                requestId, result.Summary.Total, result.Summary.Created, result.Summary.Errors);

            return StatusCode(207, result); // 207 Multi-Status
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing sales");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Get sales with filters and pagination
    /// </summary>
    /// <param name="storeId">Filter by store</param>
    /// <param name="posId">Filter by POS</param>
    /// <param name="operatorId">Filter by operator</param>
    /// <param name="dateFrom">Filter by date from</param>
    /// <param name="dateTo">Filter by date to</param>
    /// <param name="status">Filter by status</param>
    /// <param name="clientSaleId">Filter by client sale ID (offline sync)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <returns>List of sales with pagination</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SaleListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SaleListResponse>> GetSales(
        [FromQuery] Guid? storeId,
        [FromQuery] Guid? posId,
        [FromQuery] Guid? operatorId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? status,
        [FromQuery] Guid? clientSaleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var tenantSubdomain = GetTenantSubdomain();
            if (string.IsNullOrEmpty(tenantSubdomain))
                return Unauthorized(new { error = "Tenant not found" });

            var query = new SalesQueryParameters
            {
                StoreId = storeId,
                PosId = posId,
                OperatorId = operatorId,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Status = status,
                ClientSaleId = clientSaleId,
                Page = page,
                PageSize = pageSize
            };

            var result = await _salesService.GetSalesAsync(tenantSubdomain, query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Get sale by ID with full details
    /// </summary>
    /// <param name="id">Sale ID</param>
    /// <returns>Sale details including items, payments, taxes, and cancellation</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SaleResponse>> GetSaleById(Guid id)
    {
        try
        {
            var tenantSubdomain = GetTenantSubdomain();
            if (string.IsNullOrEmpty(tenantSubdomain))
                return Unauthorized(new { error = "Tenant not found" });

            var sale = await _salesService.GetSaleByIdAsync(tenantSubdomain, id);
            if (sale == null)
                return NotFound(new { error = "Sale not found", saleId = id });

            return Ok(sale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sale: SaleId={SaleId}", id);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Update sale (limited fields: status, payment_status)
    /// </summary>
    /// <param name="id">Sale ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated sale</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequireRole("admin", "manager")]
    public async Task<ActionResult<SaleResponse>> UpdateSale(Guid id, [FromBody] UpdateSaleRequest request)
    {
        try
        {
            var tenantSubdomain = GetTenantSubdomain();
            if (string.IsNullOrEmpty(tenantSubdomain))
                return Unauthorized(new { error = "Tenant not found" });

            var sale = await _salesService.UpdateSaleAsync(tenantSubdomain, id, request);
            return Ok(sale);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Sale not found", saleId = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sale: SaleId={SaleId}", id);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Add payment to sale
    /// </summary>
    /// <param name="id">Sale ID</param>
    /// <param name="request">Payment request</param>
    /// <returns>Updated sale with new payment</returns>
    [HttpPost("{id}/payments")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SaleResponse>> AddPayment(Guid id, [FromBody] SalePaymentRequest request)
    {
        try
        {
            var tenantSubdomain = GetTenantSubdomain();
            if (string.IsNullOrEmpty(tenantSubdomain))
                return Unauthorized(new { error = "Tenant not found" });

            var result = await _salesService.AddPaymentAsync(tenantSubdomain, id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Sale not found", saleId = id });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding payment: SaleId={SaleId}", id);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Cancel sale
    /// </summary>
    /// <param name="id">Sale ID</param>
    /// <param name="request">Cancellation request</param>
    /// <returns>Canceled sale</returns>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequireRole("admin", "manager")]
    public async Task<ActionResult<SaleResponse>> CancelSale(Guid id, [FromBody] CancelSaleRequest request)
    {
        try
        {
            var tenantSubdomain = GetTenantSubdomain();
            if (string.IsNullOrEmpty(tenantSubdomain))
                return Unauthorized(new { error = "Tenant not found" });

            var sale = await _salesService.CancelSaleAsync(tenantSubdomain, id, request);

            _logger.LogInformation("Sale canceled: SaleId={SaleId}, Reason={Reason}", id, request.Reason);

            return Ok(sale);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Sale not found", saleId = id });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling sale: SaleId={SaleId}", id);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Get sync status by ClientSaleId (for offline POS reconciliation)
    /// </summary>
    /// <param name="clientSaleId">Client sale ID generated by POS</param>
    /// <returns>Sync status</returns>
    [HttpGet("sync/status")]
    [ProducesResponseType(typeof(SyncStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SyncStatusResponse>> GetSyncStatus([FromQuery] Guid clientSaleId)
    {
        try
        {
            var tenantSubdomain = GetTenantSubdomain();
            if (string.IsNullOrEmpty(tenantSubdomain))
                return Unauthorized(new { error = "Tenant not found" });

            var status = await _salesService.GetSyncStatusAsync(tenantSubdomain, clientSaleId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync status: ClientSaleId={ClientSaleId}", clientSaleId);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
}
