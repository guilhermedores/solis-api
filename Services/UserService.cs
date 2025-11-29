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
    public async Task<List<UserResponse>> ListUsuariosAsync(string tenantSubdomain)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        var users = await tenantContext.Users
            .OrderBy(u => u.Name)
            .ToListAsync();

        return users.Select(u => new UserResponse
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role,
            Active = u.Active,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        }).ToList();
    }

    /// <summary>
    /// Buscar usuário por ID
    /// </summary>
    public async Task<UserResponse?> GetUsuarioByIdAsync(string tenantSubdomain, Guid id)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        var user = await tenantContext.Users.FindAsync(id);

        if (user == null)
        {
            return null;
        }

        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Active = user.Active,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    /// <summary>
    /// Criar novo usuário
    /// </summary>
    public async Task<UserResponse?> CreateUsuarioAsync(string tenantSubdomain, CreateUserRequest request)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        // Verificar se email já existe
        var emailExists = await tenantContext.Users
            .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (emailExists)
        {
            _logger.LogWarning("Tentativa de criar usuário com email duplicado {Email} no tenant {Tenant}", 
                request.Email, tenantSubdomain);
            return null; // Email já cadastrado
        }

        // Hash da senha
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 10);

        var user = new User
        {
            Name = request.Name,
            Email = request.Email.ToLower(),
            PasswordHash = passwordHash,
            Role = request.Role,
            Active = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        tenantContext.Users.Add(user);
        await tenantContext.SaveChangesAsync();

        _logger.LogInformation("Usuário criado: {UserId} ({Email}) no tenant {Tenant}", 
            user.Id, user.Email, tenantSubdomain);

        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Active = user.Active,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    /// <summary>
    /// Atualizar usuário existente
    /// </summary>
    public async Task<UserResponse?> UpdateUsuarioAsync(string tenantSubdomain, Guid id, UpdateUserRequest request)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        var user = await tenantContext.Users.FindAsync(id);

        if (user == null)
        {
            return null;
        }

        // Atualizar campos se fornecidos
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            user.Name = request.Name;
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            // Verificar se novo email já existe em outro usuário
            var emailExists = await tenantContext.Users
                .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.Id != id);

            if (emailExists)
            {
                return null; // Email já cadastrado
            }

            user.Email = request.Email.ToLower();
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 10);
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            user.Role = request.Role;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await tenantContext.SaveChangesAsync();

        _logger.LogInformation("Usuário atualizado: {UserId} no tenant {Tenant}", id, tenantSubdomain);

        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            Active = user.Active,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    /// <summary>
    /// Desativar usuário (soft delete)
    /// </summary>
    public async Task<bool> DeactivateUsuarioAsync(string tenantSubdomain, Guid id)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        var user = await tenantContext.Users.FindAsync(id);

        if (user == null)
        {
            return false;
        }

        user.Active = false;
        user.UpdatedAt = DateTime.UtcNow;

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

        var user = await tenantContext.Users.FindAsync(id);

        if (user == null)
        {
            return false;
        }

        user.Active = true;
        user.UpdatedAt = DateTime.UtcNow;

        await tenantContext.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Deletar usuário permanentemente
    /// </summary>
    public async Task<bool> DeleteUsuarioAsync(string tenantSubdomain, Guid id)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        var user = await tenantContext.Users.FindAsync(id);

        if (user == null)
        {
            return false;
        }

        tenantContext.Users.Remove(user);
        await tenantContext.SaveChangesAsync();

        return true;
    }
}
