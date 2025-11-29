namespace SolisApi.DTOs;

/// <summary>
/// Payload do JWT Token
/// </summary>
public class TokenPayload
{
    public Guid UserId { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid TenantId { get; set; }
    public string Tenant { get; set; } = string.Empty; // subdomain
    public string Role { get; set; } = string.Empty; // admin, manager, operator
    public string Type { get; set; } = string.Empty; // user, agent
}

/// <summary>
/// DTO de Login Request
/// </summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// DTO de Login Response
/// </summary>
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserResponse User { get; set; } = null!;
}

/// <summary>
/// DTO de resposta do User (sem senha)
/// </summary>
public class UserResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO para criar User
/// </summary>
public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "operator"; // admin, manager, operator
}

/// <summary>
/// DTO para atualizar User
/// </summary>
public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Role { get; set; }
}

/// <summary>
/// DTO de resposta padrão de sucesso
/// </summary>
public class SuccessResponse
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
}

/// <summary>
/// DTO de resposta de erro
/// </summary>
public class ErrorResponse
{
    public bool Success { get; set; } = false;
    public string Error { get; set; } = string.Empty;
}
