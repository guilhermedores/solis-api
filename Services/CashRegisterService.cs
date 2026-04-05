using SolisApi.DTOs;
using SolisApi.Models;
using SolisApi.Repositories;

namespace SolisApi.Services;

public interface ICashRegisterService
{
    Task<CashRegisterResponse> OpenAsync(string tenantSubdomain, OpenCashRegisterRequest request, CancellationToken cancellationToken = default);
    Task<CashRegisterResponse> CloseAsync(string tenantSubdomain, Guid id, CloseCashRegisterRequest request, CancellationToken cancellationToken = default);
    Task<CashRegisterResponse> RegisterSangriaAsync(string tenantSubdomain, Guid id, MovementRequest request, CancellationToken cancellationToken = default);
    Task<CashRegisterResponse> RegisterSuprimentoAsync(string tenantSubdomain, Guid id, MovementRequest request, CancellationToken cancellationToken = default);
    Task<CashRegisterResponse?> GetByIdAsync(string tenantSubdomain, Guid id, CancellationToken cancellationToken = default);
    Task<(List<CashRegisterResponse> Items, int Total)> ListAsync(
        string tenantSubdomain,
        Guid? storeId = null,
        int? terminalNumber = null,
        string? status = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
    Task<CashRegisterResponse> SyncFromAgentAsync(string tenantSubdomain, Guid id, SyncCashRegisterRequest request, CancellationToken cancellationToken = default);
    Task<List<CashRegisterMovementDto>> GetMovementsAsync(string tenantSubdomain, Guid id, CancellationToken cancellationToken = default);
}

public class CashRegisterService : ICashRegisterService
{
    private readonly ICashRegisterRepository _repository;
    private readonly ILogger<CashRegisterService> _logger;

    public CashRegisterService(ICashRegisterRepository repository, ILogger<CashRegisterService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CashRegisterResponse> OpenAsync(string tenantSubdomain, OpenCashRegisterRequest request, CancellationToken cancellationToken = default)
    {
        var schema = $"tenant_{tenantSubdomain}";

        var existing = await _repository.GetOpenByTerminalAsync(schema, request.StoreId, request.TerminalNumber, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Já existe um caixa aberto para o terminal {request.TerminalNumber} desta loja.");

        var now = DateTime.UtcNow;
        var entity = new CashRegister
        {
            Id = Guid.NewGuid(),
            StoreId = request.StoreId,
            TerminalNumber = request.TerminalNumber,
            OperatorId = request.OperatorId,
            OperatorName = request.OperatorName,
            OpenedAt = now,
            OpeningBalance = request.OpeningBalance,
            Status = "open",
            Notes = request.Notes,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.SaveAsync(schema, entity, cancellationToken);

        var openingMovement = new CashRegisterMovement
        {
            Id = Guid.NewGuid(),
            CashRegisterId = entity.Id,
            StoreId = entity.StoreId,
            Type = "opening",
            Amount = entity.OpeningBalance,
            OperatorId = entity.OperatorId,
            OperatorName = entity.OperatorName,
            Notes = request.Notes,
            OccurredAt = now,
            CreatedAt = now
        };
        await _repository.SaveMovementAsync(schema, openingMovement, cancellationToken);

        _logger.LogInformation("Caixa aberto: {Id}, Terminal: {Terminal}, Loja: {StoreId}", entity.Id, entity.TerminalNumber, entity.StoreId);

        var response = MapToResponse(entity);
        response.Movements.Add(MapMovement(openingMovement));
        return response;
    }

    public async Task<CashRegisterResponse> CloseAsync(string tenantSubdomain, Guid id, CloseCashRegisterRequest request, CancellationToken cancellationToken = default)
    {
        var schema = $"tenant_{tenantSubdomain}";
        var entity = await _repository.GetByIdAsync(schema, id, cancellationToken)
            ?? throw new KeyNotFoundException($"Caixa {id} não encontrado.");

        if (entity.Status != "open")
            throw new InvalidOperationException($"Caixa não pode ser fechado. Status atual: {entity.Status}");

        var now = DateTime.UtcNow;
        entity.ClosedAt = now;
        entity.ClosingBalance = request.ClosingBalance;
        entity.TotalSales = request.TotalSales;
        entity.TotalCash = request.TotalCash;
        entity.TotalDebit = request.TotalDebit;
        entity.TotalCredit = request.TotalCredit;
        entity.TotalPix = request.TotalPix;
        entity.TotalOthers = request.TotalOthers;
        entity.QuantitySales = request.QuantitySales;
        // expected = fundo inicial + total dinheiro - sangrias + suprimentos
        entity.ExpectedBalance = entity.OpeningBalance + entity.TotalCash - entity.TotalSangria + entity.TotalSuprimento;
        entity.Difference = request.ClosingBalance - entity.ExpectedBalance;
        entity.Status = "closed";
        if (!string.IsNullOrEmpty(request.Notes))
            entity.Notes = string.IsNullOrEmpty(entity.Notes) ? request.Notes : $"{entity.Notes}\n[Fechamento] {request.Notes}";
        entity.UpdatedAt = now;

        await _repository.UpdateAsync(schema, entity, cancellationToken);

        var movement = new CashRegisterMovement
        {
            Id = Guid.NewGuid(),
            CashRegisterId = entity.Id,
            StoreId = entity.StoreId,
            Type = "closing",
            Amount = request.ClosingBalance,
            OperatorId = entity.OperatorId,
            OperatorName = entity.OperatorName,
            Notes = request.Notes,
            OccurredAt = now,
            CreatedAt = now
        };
        await _repository.SaveMovementAsync(schema, movement, cancellationToken);

        _logger.LogInformation("Caixa fechado: {Id}, Diferença: {Diff}", entity.Id, entity.Difference);
        return await GetWithMovementsAsync(schema, entity, cancellationToken);
    }

    public async Task<CashRegisterResponse> RegisterSangriaAsync(string tenantSubdomain, Guid id, MovementRequest request, CancellationToken cancellationToken = default)
    {
        var schema = $"tenant_{tenantSubdomain}";
        var entity = await _repository.GetByIdAsync(schema, id, cancellationToken)
            ?? throw new KeyNotFoundException($"Caixa {id} não encontrado.");

        if (entity.Status != "open")
            throw new InvalidOperationException("Sangria só pode ser registrada em caixa aberto.");

        var now = DateTime.UtcNow;
        entity.TotalSangria += request.Amount;
        entity.UpdatedAt = now;
        await _repository.UpdateAsync(schema, entity, cancellationToken);

        var movement = new CashRegisterMovement
        {
            Id = Guid.NewGuid(),
            CashRegisterId = entity.Id,
            StoreId = entity.StoreId,
            Type = "sangria",
            Amount = request.Amount,
            OperatorId = request.OperatorId,
            OperatorName = request.OperatorName,
            Notes = request.Notes,
            OccurredAt = now,
            CreatedAt = now
        };
        await _repository.SaveMovementAsync(schema, movement, cancellationToken);

        _logger.LogInformation("Sangria registrada no caixa {Id}: R$ {Amount}", entity.Id, request.Amount);
        return await GetWithMovementsAsync(schema, entity, cancellationToken);
    }

    public async Task<CashRegisterResponse> RegisterSuprimentoAsync(string tenantSubdomain, Guid id, MovementRequest request, CancellationToken cancellationToken = default)
    {
        var schema = $"tenant_{tenantSubdomain}";
        var entity = await _repository.GetByIdAsync(schema, id, cancellationToken)
            ?? throw new KeyNotFoundException($"Caixa {id} não encontrado.");

        if (entity.Status != "open")
            throw new InvalidOperationException("Suprimento só pode ser registrado em caixa aberto.");

        var now = DateTime.UtcNow;
        entity.TotalSuprimento += request.Amount;
        entity.UpdatedAt = now;
        await _repository.UpdateAsync(schema, entity, cancellationToken);

        var movement = new CashRegisterMovement
        {
            Id = Guid.NewGuid(),
            CashRegisterId = entity.Id,
            StoreId = entity.StoreId,
            Type = "suprimento",
            Amount = request.Amount,
            OperatorId = request.OperatorId,
            OperatorName = request.OperatorName,
            Notes = request.Notes,
            OccurredAt = now,
            CreatedAt = now
        };
        await _repository.SaveMovementAsync(schema, movement, cancellationToken);

        _logger.LogInformation("Suprimento registrado no caixa {Id}: R$ {Amount}", entity.Id, request.Amount);
        return await GetWithMovementsAsync(schema, entity, cancellationToken);
    }

    public async Task<CashRegisterResponse?> GetByIdAsync(string tenantSubdomain, Guid id, CancellationToken cancellationToken = default)
    {
        var schema = $"tenant_{tenantSubdomain}";
        var entity = await _repository.GetByIdAsync(schema, id, cancellationToken);
        if (entity == null) return null;
        return await GetWithMovementsAsync(schema, entity, cancellationToken);
    }

    public async Task<(List<CashRegisterResponse> Items, int Total)> ListAsync(
        string tenantSubdomain,
        Guid? storeId = null,
        int? terminalNumber = null,
        string? status = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var schema = $"tenant_{tenantSubdomain}";
        var (items, total) = await _repository.ListAsync(schema, storeId, terminalNumber, status, dateFrom, dateTo, page, pageSize, cancellationToken);
        return (items.Select(MapToResponse).ToList(), total);
    }

    public async Task<CashRegisterResponse> SyncFromAgentAsync(string tenantSubdomain, Guid id, SyncCashRegisterRequest request, CancellationToken cancellationToken = default)
    {
        var schema = $"tenant_{tenantSubdomain}";
        request.Id = id;
        await _repository.UpsertFromAgentAsync(schema, request, cancellationToken);

        var entity = await _repository.GetByIdAsync(schema, id, cancellationToken);
        if (entity == null)
            throw new InvalidOperationException($"Falha ao recuperar caixa {id} após sync.");

        var existingMovements = await _repository.GetMovementsAsync(schema, entity.Id, cancellationToken);
        var now = DateTime.UtcNow;

        if (!existingMovements.Any(m => m.Type == "opening"))
        {
            var openingMovement = new CashRegisterMovement
            {
                Id = Guid.NewGuid(),
                CashRegisterId = entity.Id,
                StoreId = entity.StoreId,
                Type = "opening",
                Amount = entity.OpeningBalance,
                OperatorId = entity.OperatorId,
                OperatorName = entity.OperatorName,
                OccurredAt = entity.OpenedAt,
                CreatedAt = now
            };
            await _repository.SaveMovementAsync(schema, openingMovement, cancellationToken);
        }

        if (entity.Status == "closed" && !existingMovements.Any(m => m.Type == "closing"))
        {
            var closingMovement = new CashRegisterMovement
            {
                Id = Guid.NewGuid(),
                CashRegisterId = entity.Id,
                StoreId = entity.StoreId,
                Type = "closing",
                Amount = entity.ClosingBalance ?? 0,
                OperatorId = entity.OperatorId,
                OperatorName = entity.OperatorName,
                OccurredAt = entity.ClosedAt ?? now,
                CreatedAt = now
            };
            await _repository.SaveMovementAsync(schema, closingMovement, cancellationToken);
        }

        _logger.LogInformation("Caixa sincronizado do agente: {Id}", id);
        return MapToResponse(entity);
    }

    public async Task<List<CashRegisterMovementDto>> GetMovementsAsync(string tenantSubdomain, Guid id, CancellationToken cancellationToken = default)
    {
        var schema = $"tenant_{tenantSubdomain}";
        var movements = await _repository.GetMovementsAsync(schema, id, cancellationToken);
        return movements.Select(MapMovement).ToList();
    }

    private async Task<CashRegisterResponse> GetWithMovementsAsync(string schema, CashRegister entity, CancellationToken cancellationToken)
    {
        var movements = await _repository.GetMovementsAsync(schema, entity.Id, cancellationToken);
        var response = MapToResponse(entity);
        response.Movements = movements.Select(MapMovement).ToList();
        return response;
    }

    private static CashRegisterResponse MapToResponse(CashRegister e) => new()
    {
        Id = e.Id, StoreId = e.StoreId, TerminalNumber = e.TerminalNumber,
        OperatorId = e.OperatorId, OperatorName = e.OperatorName,
        OpenedAt = e.OpenedAt, ClosedAt = e.ClosedAt,
        OpeningBalance = e.OpeningBalance, ClosingBalance = e.ClosingBalance,
        TotalSales = e.TotalSales, TotalCash = e.TotalCash,
        TotalDebit = e.TotalDebit, TotalCredit = e.TotalCredit,
        TotalPix = e.TotalPix, TotalOthers = e.TotalOthers,
        QuantitySales = e.QuantitySales,
        TotalSangria = e.TotalSangria, TotalSuprimento = e.TotalSuprimento,
        ExpectedBalance = e.ExpectedBalance, Difference = e.Difference,
        Status = e.Status, Notes = e.Notes,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };

    private static CashRegisterMovementDto MapMovement(CashRegisterMovement m) => new()
    {
        Id = m.Id, CashRegisterId = m.CashRegisterId, Type = m.Type,
        Amount = m.Amount, OperatorId = m.OperatorId, OperatorName = m.OperatorName,
        Notes = m.Notes, OccurredAt = m.OccurredAt
    };
}
