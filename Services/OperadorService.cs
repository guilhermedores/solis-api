using SolisApi.DTOs;
using SolisApi.Repositories;

namespace SolisApi.Services;

public interface IOperadorService
{
    Task<List<OperadorSyncItem>> ListForSyncAsync(string tenantSubdomain, CancellationToken ct = default);
    Task<OperadorResponse> CreateAsync(string tenantSubdomain, CreateOperadorRequest request, CancellationToken ct = default);
    Task<OperadorResponse> UpdatePinAsync(string tenantSubdomain, Guid userId, UpdatePinRequest request, CancellationToken ct = default);
    Task<OperadorResponse> SetActiveAsync(string tenantSubdomain, Guid userId, bool active, CancellationToken ct = default);
    Task<PinLoginResponse?> LoginByPinAsync(string tenantSubdomain, PinLoginRequest request, CancellationToken ct = default);
}

public class OperadorService : IOperadorService
{
    private readonly IOperadorRepository _repository;
    private readonly ILogger<OperadorService> _logger;

    public OperadorService(IOperadorRepository repository, ILogger<OperadorService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<OperadorSyncItem>> ListForSyncAsync(string tenantSubdomain, CancellationToken ct = default)
    {
        var schema = $"tenant_{tenantSubdomain}";
        return await _repository.ListSyncItemsAsync(schema, ct);
    }

    public async Task<OperadorResponse> CreateAsync(string tenantSubdomain, CreateOperadorRequest request, CancellationToken ct = default)
    {
        ValidatePin(request.Pin);

        var schema = $"tenant_{tenantSubdomain}";
        var pinHash = BCrypt.Net.BCrypt.HashPassword(request.Pin, workFactor: 10);

        _logger.LogInformation("Creating operator {OperatorNumber} in tenant {Tenant}", request.OperatorNumber, tenantSubdomain);

        return await _repository.CreateAsync(schema, request, pinHash, ct);
    }

    public async Task<OperadorResponse> UpdatePinAsync(string tenantSubdomain, Guid userId, UpdatePinRequest request, CancellationToken ct = default)
    {
        ValidatePin(request.NewPin);

        var schema = $"tenant_{tenantSubdomain}";
        var pinHash = BCrypt.Net.BCrypt.HashPassword(request.NewPin, workFactor: 10);

        await _repository.UpdatePinAsync(schema, userId, pinHash, ct);

        _logger.LogInformation("PIN updated for operator {UserId} in tenant {Tenant}", userId, tenantSubdomain);

        var operador = await _repository.GetByIdResponseAsync(schema, userId, ct)
            ?? throw new KeyNotFoundException($"Operator {userId} not found");

        return operador;
    }

    public async Task<OperadorResponse> SetActiveAsync(string tenantSubdomain, Guid userId, bool active, CancellationToken ct = default)
    {
        var schema = $"tenant_{tenantSubdomain}";
        await _repository.SetActiveAsync(schema, userId, active, ct);

        _logger.LogInformation("Operator {UserId} active={Active} in tenant {Tenant}", userId, active, tenantSubdomain);

        return await _repository.GetByIdResponseAsync(schema, userId, ct)
            ?? throw new KeyNotFoundException($"Operator {userId} not found");
    }

    public async Task<PinLoginResponse?> LoginByPinAsync(string tenantSubdomain, PinLoginRequest request, CancellationToken ct = default)
    {
        var schema = $"tenant_{tenantSubdomain}";
        var operador = await _repository.GetByOperatorNumberAsync(schema, request.OperatorNumber, ct);

        if (operador == null || !operador.Active)
        {
            _logger.LogWarning("PIN login failed: operator {OperatorNumber} not found or inactive in tenant {Tenant}", request.OperatorNumber, tenantSubdomain);
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Pin, operador.PinHash))
        {
            _logger.LogWarning("PIN login failed: wrong PIN for operator {OperatorNumber} in tenant {Tenant}", request.OperatorNumber, tenantSubdomain);
            return null;
        }

        _logger.LogInformation("PIN login success: operator {OperatorNumber} in tenant {Tenant}", request.OperatorNumber, tenantSubdomain);

        return new PinLoginResponse
        {
            Id = operador.Id,
            Name = operador.Name,
            OperatorNumber = operador.OperatorNumber,
            Role = operador.Role,
            PinHash = operador.PinHash,
            UpdatedAt = operador.UpdatedAt
        };
    }

    private static void ValidatePin(string pin)
    {
        if (string.IsNullOrEmpty(pin) || pin.Length < 4 || pin.Length > 8 || !pin.All(char.IsDigit))
            throw new ArgumentException("PIN must be 4 to 8 digits");
    }
}
