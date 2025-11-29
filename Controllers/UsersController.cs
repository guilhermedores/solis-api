using Microsoft.AspNetCore.Mvc;
using SolisApi.DTOs;
using SolisApi.Services;
using SolisApi.Middleware;

namespace SolisApi.Controllers;

/// <summary>
/// Controller de gerenciamento de usuários
/// </summary>
[ApiController]
[Route("api/users")]
[RequireAuth]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Listar todos os usuários do tenant
    /// </summary>
    [HttpGet]
    [RequireManager]
    public async Task<IActionResult> ListUsers()
    {
        var tenant = User.FindFirst("tenant")?.Value;

        if (string.IsNullOrEmpty(tenant))
        {
            return Unauthorized(new ErrorResponse { Error = "Tenant não identificado" });
        }

        var users = await _userService.ListUsuariosAsync(tenant);
        return Ok(users);
    }

    /// <summary>
    /// Buscar usuário por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
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

        var user = await _userService.GetUsuarioByIdAsync(tenant, id);

        if (user == null)
        {
            return NotFound(new ErrorResponse { Error = "Usuário não encontrado" });
        }

        return Ok(user);
    }

    /// <summary>
    /// Criar novo usuário
    /// </summary>
    [HttpPost]
    [RequireManager]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var tenant = User.FindFirst("tenant")?.Value;

        if (string.IsNullOrEmpty(tenant))
        {
            return Unauthorized(new ErrorResponse { Error = "Tenant não identificado" });
        }

        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ErrorResponse { Error = "Nome, email e senha são obrigatórios" });
        }

        var user = await _userService.CreateUsuarioAsync(tenant, request);

        if (user == null)
        {
            return BadRequest(new ErrorResponse { Error = "Email já cadastrado" });
        }

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    /// <summary>
    /// Atualizar usuário existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
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

        var user = await _userService.UpdateUsuarioAsync(tenant, id, request);

        if (user == null)
        {
            return NotFound(new ErrorResponse { Error = "Usuário não encontrado ou email já cadastrado" });
        }

        return Ok(user);
    }

    /// <summary>
    /// Deletar usuário (desativar)
    /// </summary>
    [HttpDelete("{id}")]
    [RequireManager]
    public async Task<IActionResult> DeleteUser(Guid id)
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
    public async Task<IActionResult> ReactivateUser(Guid id)
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
