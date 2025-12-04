using SolisApi.DTOs;
using SolisApi.Models;
using SolisApi.Repositories;
using SolisApi.Services.Domain;

namespace SolisApi.Services;

/// <summary>
/// Application Service for Sales
/// Coordinates domain objects and repositories
/// Thin layer - business logic is in the domain model
/// </summary>
public class SalesApplicationService : ISalesService
{
    private readonly ISaleRepository _saleRepository;
    private readonly ITaxDomainService _taxDomainService;
    private readonly ILogger<SalesApplicationService> _logger;
    private const string DEFAULT_STATE = "MG"; // TODO: Get from store/company config

    public SalesApplicationService(
        ISaleRepository saleRepository,
        ITaxDomainService taxDomainService,
        ILogger<SalesApplicationService> logger)
    {
        _saleRepository = saleRepository;
        _taxDomainService = taxDomainService;
        _logger = logger;
    }

    public async Task<SaleResponse> CreateSaleAsync(
        string tenantSubdomain,
        CreateSaleRequest request,
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        // Add tenant_ prefix to match database schema naming
        var tenantSchema = $"tenant_{tenantSubdomain}";
        
        // Check for duplicate by ClientSaleId (offline sync idempotency)
        if (request.ClientSaleId.HasValue)
        {
            var existing = await _saleRepository.GetByClientSaleIdAsync(tenantSchema, request.ClientSaleId.Value, cancellationToken);
            if (existing != null)
            {
                _logger.LogInformation("Sale already exists with ClientSaleId={ClientSaleId}", request.ClientSaleId);
                return MapToResponse(existing);
            }
        }

        // TODO: Check idempotency key in cache/database if provided

        // Create sale aggregate using factory method
        var sale = Sale.Create(
            request.StoreId,
            request.PosId,
            request.OperatorId,
            request.ClientSaleId,
            request.SaleDateTime);

        // Add items with taxes
        foreach (var itemRequest in request.Items)
        {
            // Buscar dados do produto via ProductId
            var product = await _saleRepository.GetProductByIdAsync(tenantSchema, itemRequest.ProductId, cancellationToken);
            if (product == null)
                throw new InvalidOperationException($"Product {itemRequest.ProductId} not found");

            var item = SaleItem.Create(
                itemRequest.ProductId,
                itemRequest.Quantity,
                itemRequest.UnitPrice,
                itemRequest.DiscountAmount,
                product.Sku,
                product.Description);

            // Calculate taxes using domain service
            var baseAmount = (item.Quantity * item.UnitPrice) - item.DiscountAmount;
            var taxes = await _taxDomainService.CalculateTaxesForItemAsync(
                tenantSchema,
                item.ProductId,
                baseAmount,
                DEFAULT_STATE,
                cancellationToken);

            // Add taxes to item
            foreach (var tax in taxes)
            {
                item.AddTax(tax);
            }

            // Add item to sale (aggregate handles totals calculation)
            sale.AddItem(item);
        }

        // Add payments
        if (request.Payments != null)
        {
            foreach (var paymentRequest in request.Payments)
            {
                var payment = SalePayment.Create(
                    paymentRequest.PaymentMethodId,
                    paymentRequest.Amount,
                    paymentRequest.AcquirerTxnId,
                    paymentRequest.AuthorizationCode,
                    paymentRequest.ChangeAmount);

                sale.AddPayment(payment); // Aggregate handles payment status update
            }
        }

        // Save aggregate
        await _saleRepository.SaveAsync(tenantSchema, sale, cancellationToken);

        _logger.LogInformation("Sale created successfully: SaleId={SaleId}, Total={Total}", sale.Id, sale.Total);

        return MapToResponse(sale);
    }

    public async Task<SaleResponse?> GetSaleByIdAsync(
        string tenantSubdomain,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var tenantSchema = $"tenant_{tenantSubdomain}";
        var sale = await _saleRepository.GetByIdAsync(tenantSchema, saleId, cancellationToken);
        return sale != null ? MapToResponse(sale) : null;
    }

    public async Task<SaleListResponse> GetSalesAsync(
        string tenantSubdomain,
        SalesQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var tenantSchema = $"tenant_{tenantSubdomain}";
        var (sales, total) = await _saleRepository.GetAllAsync(
            tenantSchema,
            query.StoreId,
            query.PosId,
            query.OperatorId,
            query.DateFrom,
            query.DateTo,
            query.Status,
            query.ClientSaleId,
            query.Page,
            query.PageSize,
            cancellationToken);

        return new SaleListResponse
        {
            Data = sales.Select(MapToResponse).ToList(),
            Pagination = new PaginationMetadata
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = total,
                TotalPages = (int)Math.Ceiling(total / (double)query.PageSize)
            }
        };
    }

    public async Task<SaleResponse> UpdateSaleAsync(
        string tenantSubdomain,
        Guid saleId,
        UpdateSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantSchema = $"tenant_{tenantSubdomain}";
        var sale = await _saleRepository.GetByIdAsync(tenantSchema, saleId, cancellationToken);
        if (sale == null)
            throw new KeyNotFoundException($"Sale {saleId} not found");

        // Update status using domain method (validates business rules)
        sale.UpdateStatus(request.Status!);

        await _saleRepository.UpdateAsync(tenantSchema, sale, cancellationToken);

        _logger.LogInformation("Sale updated: SaleId={SaleId}, Status={Status}", saleId, request.Status);

        return MapToResponse(sale);
    }

    public async Task<SaleResponse> AddPaymentAsync(
        string tenantSubdomain,
        Guid saleId,
        SalePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantSchema = $"tenant_{tenantSubdomain}";
        var sale = await _saleRepository.GetByIdAsync(tenantSchema, saleId, cancellationToken);
        if (sale == null)
            throw new KeyNotFoundException($"Sale {saleId} not found");

        // Create payment using factory
        var payment = SalePayment.Create(
            request.PaymentMethodId,
            request.Amount,
            request.AcquirerTxnId,
            request.AuthorizationCode,
            request.ChangeAmount);

        // Add payment (aggregate handles payment status)
        sale.AddPayment(payment);

        await _saleRepository.UpdateAsync(tenantSchema, sale, cancellationToken);

        _logger.LogInformation("Payment added to sale: SaleId={SaleId}, Amount={Amount}", saleId, request.Amount);

        return MapToResponse(sale);
    }

    public async Task<SaleResponse> CancelSaleAsync(
        string tenantSubdomain,
        Guid saleId,
        CancelSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantSchema = $"tenant_{tenantSubdomain}";
        var sale = await _saleRepository.GetByIdAsync(tenantSchema, saleId, cancellationToken);
        if (sale == null)
            throw new KeyNotFoundException($"Sale {saleId} not found");

        // Cancel using domain method (validates business rules)
        sale.Cancel(
            request.Reason!,
            request.Source!,
            request.CancellationType!,
            request.RefundAmount);

        await _saleRepository.UpdateAsync(tenantSchema, sale, cancellationToken);

        _logger.LogInformation("Sale canceled: SaleId={SaleId}, Reason={Reason}", saleId, request.Reason);

        return MapToResponse(sale);
    }

    public async Task<SyncStatusResponse> GetSyncStatusAsync(
        string tenantSubdomain,
        Guid clientSaleId,
        CancellationToken cancellationToken = default)
    {
        var tenantSchema = $"tenant_{tenantSubdomain}";
        var sale = await _saleRepository.GetByClientSaleIdAsync(tenantSchema, clientSaleId, cancellationToken);

        return new SyncStatusResponse
        {
            ClientSaleId = clientSaleId,
            Exists = sale != null,
            SaleId = sale?.Id,
            Status = sale?.Status,
            CreatedAt = sale?.CreatedAt
        };
    }

    public async Task<SaleSyncResponse> SyncSalesAsync(
        string tenantSubdomain,
        SaleSyncRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantSchema = $"tenant_{tenantSubdomain}";
        var results = new List<SaleSyncResult>();

        foreach (var saleRequest in request.Sales)
        {
            try
            {
                var result = await CreateSaleAsync(tenantSchema, saleRequest, null, cancellationToken);
                results.Add(new SaleSyncResult
                {
                    ClientSaleId = saleRequest.ClientSaleId,
                    SaleId = result.Id,
                    Status = "created",
                    Message = "Sale synced successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync sale with ClientSaleId={ClientSaleId}", saleRequest.ClientSaleId);
                results.Add(new SaleSyncResult
                {
                    ClientSaleId = saleRequest.ClientSaleId,
                    Status = "failed",
                    Message = ex.Message
                });
            }
        }

        return new SaleSyncResponse
        {
            Results = results,
            Summary = new SyncSummary
            {
                Total = request.Sales.Count,
                Created = results.Count(r => r.Status == "created"),
                Errors = results.Count(r => r.Status == "failed")
            }
        };
    }

    // Mapping method
    private SaleResponse MapToResponse(Sale sale)
    {
        return new SaleResponse
        {
            Id = sale.Id,
            ClientSaleId = sale.ClientSaleId,
            StoreId = sale.StoreId,
            PosId = sale.PosId,
            OperatorId = sale.OperatorId,
            SaleDateTime = sale.SaleDateTime,
            Status = sale.Status,
            PaymentStatus = sale.PaymentStatus,
            Subtotal = sale.Subtotal,
            DiscountTotal = sale.DiscountTotal,
            TaxTotal = sale.TaxTotal,
            Total = sale.Total,
            CreatedAt = sale.CreatedAt,
            UpdatedAt = sale.UpdatedAt,
            OrderNumber = sale.OrderNumber,
            Items = sale.Items.Select(i => new SaleItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                Sku = i.Sku,
                Description = i.Description,
                UnitOfMeasure = i.UnitOfMeasure,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountAmount = i.DiscountAmount,
                TaxAmount = i.TaxAmount,
                Total = i.Total,
                Taxes = i.Taxes.Select(t => new SaleTaxDto
                {
                    Id = t.Id,
                    TaxTypeId = t.TaxTypeId,
                    TaxTypeCode = "", // TaxType gerenciado por CRUD genérico
                    TaxRuleId = t.TaxRuleId,
                    BaseAmount = t.BaseAmount,
                    Rate = t.Rate,
                    Amount = t.Amount
                }).ToList()
            }).ToList(),
            Payments = sale.Payments.Select(p => new SalePaymentDto
            {
                Id = p.Id,
                PaymentMethodId = p.PaymentMethodId,
                Amount = p.Amount,
                AcquirerTxnId = p.AcquirerTxnId,
                AuthorizationCode = p.AuthorizationCode,
                ChangeAmount = p.ChangeAmount,
                Status = p.Status,
                ProcessedAt = p.ProcessedAt
            }).ToList(),
            Cancellation = sale.Cancellation != null ? new SaleCancellationDto
            {
                Id = sale.Cancellation.Id,
                Reason = sale.Cancellation.Reason,
                Source = sale.Cancellation.Source,
                CancellationType = sale.Cancellation.CancellationType,
                RefundAmount = sale.Cancellation.RefundAmount,
                CanceledAt = sale.Cancellation.CanceledAt
            } : null
        };
    }
}
