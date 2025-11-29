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
    /// <param name="tenantSubdomain">Subdomain do tenant (ex: demo, cliente1)</param>
    /// <param name="request">Credenciais de login (email e senha)</param>
    /// <returns>Token JWT e dados do usuário</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromQuery] string tenantSubdomain,
        [FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(tenantSubdomain))
        {
            return BadRequest(new ErrorResponse
            {
                Error = "Subdomain do tenant é obrigatório"
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
