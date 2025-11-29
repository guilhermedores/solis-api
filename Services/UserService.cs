using Microsoft.EntityFrameworkCore;
using SolisApi.Data;
using SolisApi.DTOs;
using SolisApi.Models;
using BCrypt.Net;

namespace SolisApi.Services;

/// <summary>
/// Serviço de gerenciamento de usuários
/// </summary>
public class UserService
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ITenantDbContextFactory tenantDbContextFactory,
        ILogger<UserService> logger)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
        _logger = logger;
    }

    /// <summary>
    /// Criar DbContext do tenant usando factory
    /// </summary>
    private TenantDbContext CreateTenantContext(string tenantSubdomain)
    {
        return _tenantDbContextFactory.CreateDbContext(tenantSubdomain);
    }

    /// <summary>
    /// Listar todos os usuários do tenant
    /// </summary>
    public async Task<List<UsuarioResponse>> ListUsuariosAsync(string tenantSubdomain)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        var usuarios = await tenantContext.Usuarios
            .OrderBy(u => u.Nome)
            .ToListAsync();

        return usuarios.Select(u => new UsuarioResponse
        {
            Id = u.Id,
            Nome = u.Nome,
            Email = u.Email,
            Role = u.Role,
            Ativo = u.Ativo,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        }).ToList();
    }

    /// <summary>
    /// Buscar usuário por ID
    /// </summary>
    public async Task<UsuarioResponse?> GetUsuarioByIdAsync(string tenantSubdomain, Guid id)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        var usuario = await tenantContext.Usuarios.FindAsync(id);

        if (usuario == null)
        {
            return null;
        }

        return new UsuarioResponse
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Role = usuario.Role,
            Ativo = usuario.Ativo,
            CreatedAt = usuario.CreatedAt,
            UpdatedAt = usuario.UpdatedAt
        };
    }

    /// <summary>
    /// Criar novo usuário
    /// </summary>
    public async Task<UsuarioResponse?> CreateUsuarioAsync(string tenantSubdomain, CreateUsuarioRequest request)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        // Verificar se email já existe
        var emailExists = await tenantContext.Usuarios
            .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (emailExists)
        {
            _logger.LogWarning("Tentativa de criar usuário com email duplicado {Email} no tenant {Tenant}", 
                request.Email, tenantSubdomain);
            return null; // Email já cadastrado
        }

        // Hash da senha
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 10);

        var usuario = new Usuario
        {
            Nome = request.Nome,
            Email = request.Email.ToLower(),
            PasswordHash = passwordHash,
            Role = request.Role,
            Ativo = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        tenantContext.Usuarios.Add(usuario);
        await tenantContext.SaveChangesAsync();

        _logger.LogInformation("Usuário criado: {UserId} ({Email}) no tenant {Tenant}", 
            usuario.Id, usuario.Email, tenantSubdomain);

        return new UsuarioResponse
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Role = usuario.Role,
            Ativo = usuario.Ativo,
            CreatedAt = usuario.CreatedAt,
            UpdatedAt = usuario.UpdatedAt
        };
    }

    /// <summary>
    /// Atualizar usuário existente
    /// </summary>
    public async Task<UsuarioResponse?> UpdateUsuarioAsync(string tenantSubdomain, Guid id, UpdateUsuarioRequest request)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        var usuario = await tenantContext.Usuarios.FindAsync(id);

        if (usuario == null)
        {
            return null;
        }

        // Atualizar campos se fornecidos
        if (!string.IsNullOrWhiteSpace(request.Nome))
        {
            usuario.Nome = request.Nome;
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            // Verificar se novo email já existe em outro usuário
            var emailExists = await tenantContext.Usuarios
                .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.Id != id);

            if (emailExists)
            {
                return null; // Email já cadastrado
            }

            usuario.Email = request.Email.ToLower();
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 10);
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            usuario.Role = request.Role;
        }

        usuario.UpdatedAt = DateTime.UtcNow;

        await tenantContext.SaveChangesAsync();

        _logger.LogInformation("Usuário atualizado: {UserId} no tenant {Tenant}", id, tenantSubdomain);

        return new UsuarioResponse
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Role = usuario.Role,
            Ativo = usuario.Ativo,
            CreatedAt = usuario.CreatedAt,
            UpdatedAt = usuario.UpdatedAt
        };
    }

    /// <summary>
    /// Desativar usuário (soft delete)
    /// </summary>
    public async Task<bool> DeactivateUsuarioAsync(string tenantSubdomain, Guid id)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        var usuario = await tenantContext.Usuarios.FindAsync(id);

        if (usuario == null)
        {
            return false;
        }

        usuario.Ativo = false;
        usuario.UpdatedAt = DateTime.UtcNow;

        await tenantContext.SaveChangesAsync();

        _logger.LogInformation("Usuário desativado: {UserId} no tenant {Tenant}", id, tenantSubdomain);

        return true;
    }

    /// <summary>
    /// Reativar usuário
    /// </summary>
    public async Task<bool> ReactivateUsuarioAsync(string tenantSubdomain, Guid id)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        var usuario = await tenantContext.Usuarios.FindAsync(id);

        if (usuario == null)
        {
            return false;
        }

        usuario.Ativo = true;
        usuario.UpdatedAt = DateTime.UtcNow;

        await tenantContext.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Deletar usuário permanentemente
    /// </summary>
    public async Task<bool> DeleteUsuarioAsync(string tenantSubdomain, Guid id)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        var usuario = await tenantContext.Usuarios.FindAsync(id);

        if (usuario == null)
        {
            return false;
        }

        tenantContext.Usuarios.Remove(usuario);
        await tenantContext.SaveChangesAsync();

        return true;
    }
}
