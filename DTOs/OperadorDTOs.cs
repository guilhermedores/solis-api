namespace SolisApi.DTOs;

public class PinLoginRequest
{
    public int OperatorNumber { get; set; }
    public string Pin { get; set; } = string.Empty;
}

/// <summary>
/// Item retornado ao agente para sync local — inclui PinHash.
/// Enviado APENAS para tokens do tipo "agent".
/// </summary>
public class OperadorSyncItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int OperatorNumber { get; set; }
    public string PinHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PinLoginResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OperatorNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public string PinHash { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
