using Microsoft.AspNetCore.Mvc;
using SolisApi.DTOs;
using SolisApi.Services;

namespace SolisApi.Controllers;

/// <summary>
/// Controller de autenticação
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Login - autenticar usuário e retornar token
    /// </summary>
    /// <param name="request">Credenciais de login (email e senha)</param>
    /// <returns>Token JWT e dados do usuário</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Obter tenant do header
        var tenantSubdomain = Request.Headers["X-Tenant-Subdomain"].FirstOrDefault();
        
        if (string.IsNullOrWhiteSpace(tenantSubdomain))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Header X-Tenant-Subdomain é obrigatório"
            });
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Email e senha são obrigatórios"
            });
        }

        var result = await _authService.LoginAsync(tenantSubdomain, request);

        if (result == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Credenciais inválidas ou tenant não encontrado"
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Me - obter dados do usuário autenticado
    /// </summary>
    /// <returns>Dados do usuário autenticado</returns>
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        // Obter token do header Authorization
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Token de autenticação não fornecido"
            });
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        
        // Validar token primeiro para extrair o tenant
        var payload = _authService.ValidateToken(token);
        if (payload == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Token inválido ou expirado"
            });
        }

        // Usar o tenant extraído do token
        var result = await _authService.GetUserFromTokenAsync(token, payload.Tenant);

        if (result == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "Token inválido ou usuário não encontrado"
            });
        }

        return Ok(result);
    }
}
