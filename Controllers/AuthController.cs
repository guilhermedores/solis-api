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
}
