using Microsoft.AspNetCore.Mvc;
using SolisApi.DTOs;
using SolisApi.Services;
using SolisApi.Middleware;

namespace SolisApi.Controllers;

/// <summary>
/// Controller de gerenciamento de usuários
/// </summary>
[ApiController]
[Route("api/usuarios")]
[RequireAuth]
public class UsuariosController : ControllerBase
{
    private readonly UserService _userService;

    public UsuariosController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Listar todos os usuários do tenant
    /// </summary>
    [HttpGet]
    [RequireManager]
    public async Task<IActionResult> ListUsuarios()
    {
        var tenant = User.FindFirst("tenant")?.Value;

        if (string.IsNullOrEmpty(tenant))
        {
            return Unauthorized(new ErrorResponse { Error = "Tenant não identificado" });
        }

        var usuarios = await _userService.ListUsuariosAsync(tenant);
        return Ok(usuarios);
    }

    /// <summary>
    /// Buscar usuário por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUsuario(Guid id)
    {
        var tenant = User.FindFirst("tenant")?.Value;
        var userRole = User.FindFirst("role")?.Value;
        var currentUserId = User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(tenant))
        {
            return Unauthorized(new ErrorResponse { Error = "Tenant não identificado" });
        }

        // Operadores só podem ver seu próprio usuário
        if (userRole == "operator" && currentUserId != id.ToString())
        {
            return Forbid();
        }

        var usuario = await _userService.GetUsuarioByIdAsync(tenant, id);

        if (usuario == null)
        {
            return NotFound(new ErrorResponse { Error = "Usuário não encontrado" });
        }

        return Ok(usuario);
    }

    /// <summary>
    /// Criar novo usuário
    /// </summary>
    [HttpPost]
    [RequireManager]
    public async Task<IActionResult> CreateUsuario([FromBody] CreateUsuarioRequest request)
    {
        var tenant = User.FindFirst("tenant")?.Value;

        if (string.IsNullOrEmpty(tenant))
        {
            return Unauthorized(new ErrorResponse { Error = "Tenant não identificado" });
        }

        if (string.IsNullOrWhiteSpace(request.Nome) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ErrorResponse { Error = "Nome, email e senha são obrigatórios" });
        }

        var usuario = await _userService.CreateUsuarioAsync(tenant, request);

        if (usuario == null)
        {
            return BadRequest(new ErrorResponse { Error = "Email já cadastrado" });
        }

        return CreatedAtAction(nameof(GetUsuario), new { id = usuario.Id }, usuario);
    }

    /// <summary>
    /// Atualizar usuário existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUsuario(Guid id, [FromBody] UpdateUsuarioRequest request)
    {
        var tenant = User.FindFirst("tenant")?.Value;
        var userRole = User.FindFirst("role")?.Value;
        var currentUserId = User.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(tenant))
        {
            return Unauthorized(new ErrorResponse { Error = "Tenant não identificado" });
        }

        // Operadores só podem editar seu próprio usuário e não podem mudar role
        if (userRole == "operator")
        {
            if (currentUserId != id.ToString())
            {
                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                return Forbid();
            }
        }

        var usuario = await _userService.UpdateUsuarioAsync(tenant, id, request);

        if (usuario == null)
        {
            return NotFound(new ErrorResponse { Error = "Usuário não encontrado ou email já cadastrado" });
        }

        return Ok(usuario);
    }

    /// <summary>
    /// Deletar usuário (desativar)
    /// </summary>
    [HttpDelete("{id}")]
    [RequireManager]
    public async Task<IActionResult> DeleteUsuario(Guid id)
    {
        var tenant = User.FindFirst("tenant")?.Value;

        if (string.IsNullOrEmpty(tenant))
        {
            return Unauthorized(new ErrorResponse { Error = "Tenant não identificado" });
        }

        var success = await _userService.DeactivateUsuarioAsync(tenant, id);

        if (!success)
        {
            return NotFound(new ErrorResponse { Error = "Usuário não encontrado" });
        }

        return Ok(new SuccessResponse { Message = "Usuário desativado com sucesso" });
    }

    /// <summary>
    /// Reativar usuário
    /// </summary>
    [HttpPost("{id}/reactivate")]
    [RequireManager]
    public async Task<IActionResult> ReactivateUsuario(Guid id)
    {
        var tenant = User.FindFirst("tenant")?.Value;

        if (string.IsNullOrEmpty(tenant))
        {
            return Unauthorized(new ErrorResponse { Error = "Tenant não identificado" });
        }

        var success = await _userService.ReactivateUsuarioAsync(tenant, id);

        if (!success)
        {
            return NotFound(new ErrorResponse { Error = "Usuário não encontrado" });
        }

        return Ok(new SuccessResponse { Message = "Usuário reativado com sucesso" });
    }
}
