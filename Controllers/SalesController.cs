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

    public SalesController(ISalesService salesService, ILogger<SalesController> logger)
    {
        _salesService = salesService;
        _logger = logger;
    }

    private string? GetTenantSubdomain()
    {
        return HttpContext.Items["TenantSubdomain"]?.ToString();
    }

    /// <summary>Create a new sale</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SaleResponse>> CreateSale(
        [FromBody] CreateSaleRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
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

    /// <summary>Sync multiple sales (batch operation for offline POS)</summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(SaleSyncResponse), StatusCodes.Status207MultiStatus)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SaleSyncResponse>> SyncSales([FromBody] SaleSyncRequest request)
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

        return StatusCode(207, result);
    }

    /// <summary>Get sales with filters and pagination</summary>
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
        [FromQuery] Guid? cashRegisterId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
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
            CashRegisterId = cashRegisterId,
            Page = page,
            PageSize = pageSize
        };

        var result = await _salesService.GetSalesAsync(tenantSubdomain, query);
        return Ok(result);
    }

    /// <summary>Get sale by ID with full details</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SaleResponse>> GetSaleById(Guid id)
    {
        var tenantSubdomain = GetTenantSubdomain();
        if (string.IsNullOrEmpty(tenantSubdomain))
            return Unauthorized(new { error = "Tenant not found" });

        var sale = await _salesService.GetSaleByIdAsync(tenantSubdomain, id);
        if (sale == null)
            return NotFound(new { error = "Sale not found", saleId = id });

        return Ok(sale);
    }

    /// <summary>Update sale (limited fields: status, payment_status)</summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequireRole("admin", "manager")]
    public async Task<ActionResult<SaleResponse>> UpdateSale(Guid id, [FromBody] UpdateSaleRequest request)
    {
        var tenantSubdomain = GetTenantSubdomain();
        if (string.IsNullOrEmpty(tenantSubdomain))
            return Unauthorized(new { error = "Tenant not found" });

        try
        {
            var sale = await _salesService.UpdateSaleAsync(tenantSubdomain, id, request);
            return Ok(sale);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Sale not found", saleId = id });
        }
    }

    /// <summary>Add payment to sale</summary>
    [HttpPost("{id}/payments")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SaleResponse>> AddPayment(Guid id, [FromBody] SalePaymentRequest request)
    {
        var tenantSubdomain = GetTenantSubdomain();
        if (string.IsNullOrEmpty(tenantSubdomain))
            return Unauthorized(new { error = "Tenant not found" });

        try
        {
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
    }

    /// <summary>Cancel sale</summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequireRole("admin", "manager")]
    public async Task<ActionResult<SaleResponse>> CancelSale(Guid id, [FromBody] CancelSaleRequest request)
    {
        var tenantSubdomain = GetTenantSubdomain();
        if (string.IsNullOrEmpty(tenantSubdomain))
            return Unauthorized(new { error = "Tenant not found" });

        try
        {
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
    }

    /// <summary>Cancel sale by ClientSaleId (for offline POS where server-side ID is unknown)</summary>
    [HttpPost("cancel-by-client/{clientSaleId:guid}")]
    [ProducesResponseType(typeof(SaleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SaleResponse>> CancelByClientSaleId(
        Guid clientSaleId,
        [FromBody] CancelSaleRequest request)
    {
        var tenantSubdomain = GetTenantSubdomain();
        if (string.IsNullOrEmpty(tenantSubdomain))
            return Unauthorized(new { error = "Tenant not found" });

        try
        {
            var sale = await _salesService.CancelByClientSaleIdAsync(tenantSubdomain, clientSaleId, request);
            _logger.LogInformation("Sale canceled by ClientSaleId: {ClientSaleId}", clientSaleId);
            return Ok(sale);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Sale not found", clientSaleId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Get sync status by ClientSaleId (for offline POS reconciliation)</summary>
    [HttpGet("sync/status")]
    [ProducesResponseType(typeof(SyncStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SyncStatusResponse>> GetSyncStatus([FromQuery] Guid clientSaleId)
    {
        var tenantSubdomain = GetTenantSubdomain();
        if (string.IsNullOrEmpty(tenantSubdomain))
            return Unauthorized(new { error = "Tenant not found" });

        var status = await _salesService.GetSyncStatusAsync(tenantSubdomain, clientSaleId);
        return Ok(status);
    }
}
