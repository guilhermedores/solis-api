using SolisApi.DTOs;
using SolisApi.Repositories;

namespace SolisApi.Services;

public interface IOperadorService
{
    Task<List<OperadorSyncItem>> ListForSyncAsync(string tenantSubdomain, CancellationToken ct = default);
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
}
