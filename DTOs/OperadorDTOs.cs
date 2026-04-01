namespace SolisApi.DTOs;

public class CreateOperadorRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int OperatorNumber { get; set; }
    public string Pin { get; set; } = string.Empty;
    public string Role { get; set; } = "operator"; // manager | operator
}

public class UpdatePinRequest
{
    public string NewPin { get; set; } = string.Empty;
}

public class ActivateOperadorRequest
{
    public bool Active { get; set; }
}

public class PinLoginRequest
{
    public int OperatorNumber { get; set; }
    public string Pin { get; set; } = string.Empty;
}

public class OperadorResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int OperatorNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
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
