using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SolisApi.Data;
using SolisApi.DTOs;
using SolisApi.Models;
using BCrypt.Net;

namespace SolisApi.Services;

/// <summary>
/// Serviço de autenticação - geração e validação de tokens JWT
/// </summary>
public class AuthService
{
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly SolisDbContext _context;
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IConfiguration configuration, 
        SolisDbContext context,
        ITenantDbContextFactory tenantDbContextFactory,
        ILogger<AuthService> logger)
    {
        _jwtSecret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret não configurado");
        _jwtIssuer = configuration["Jwt:Issuer"] ?? "SolisApi";
        _jwtAudience = configuration["Jwt:Audience"] ?? "SolisApi";
        _context = context;
        _tenantDbContextFactory = tenantDbContextFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gerar token JWT para usuário (30 dias de validade)
    /// </summary>
    public string GenerateToken(TokenPayload payload)
    {
        return GenerateTokenWithExpiry(payload, TimeSpan.FromDays(30));
    }

    /// <summary>
    /// Gerar token JWT para agente (10 anos de validade)
    /// </summary>
    public string GenerateAgentToken(TokenPayload payload)
    {
        payload.Type = "agent";
        return GenerateTokenWithExpiry(payload, TimeSpan.FromDays(3650)); // 10 anos
    }

    /// <summary>
    /// Gerar token com tempo de expiração customizado
    /// </summary>
    private string GenerateTokenWithExpiry(TokenPayload payload, TimeSpan expiry)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSecret);

        var claims = new List<Claim>
        {
            new("userId", payload.UserId.ToString()),
            new("empresaId", payload.EmpresaId.ToString()),
            new("tenantId", payload.TenantId.ToString()),
            new("tenant", payload.Tenant),
            new("role", payload.Role),
            new("type", payload.Type)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(expiry),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Validar token JWT e retornar o payload
    /// </summary>
    public TokenPayload? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer,
                ValidateAudience = true,
                ValidAudience = _jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            return new TokenPayload
            {
                UserId = Guid.Parse(principal.FindFirst("userId")?.Value ?? string.Empty),
                EmpresaId = Guid.Parse(principal.FindFirst("empresaId")?.Value ?? string.Empty),
                TenantId = Guid.Parse(principal.FindFirst("tenantId")?.Value ?? string.Empty),
                Tenant = principal.FindFirst("tenant")?.Value ?? string.Empty,
                Role = principal.FindFirst("role")?.Value ?? string.Empty,
                Type = principal.FindFirst("type")?.Value ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao validar token JWT");
            return null;
        }
    }

    /// <summary>
    /// Login - validar credenciais e retornar token
    /// </summary>
    public async Task<LoginResponse?> LoginAsync(string tenantSubdomain, LoginRequest request)
    {
        _logger.LogInformation("Tentativa de login para tenant {Tenant}, email {Email}", tenantSubdomain, request.Email);

        // Buscar tenant
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Subdomain == tenantSubdomain && t.Active);

        if (tenant == null)
        {
            _logger.LogWarning("Tenant {Tenant} não encontrado ou inativo", tenantSubdomain);
            return null;
        }

        // Usar factory para criar DbContext do tenant
        using var tenantContext = _tenantDbContextFactory.CreateDbContext(tenantSubdomain);

        // Buscar usuário por email
        var usuario = await tenantContext.Usuarios
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.Ativo);

        if (usuario == null)
        {
            _logger.LogWarning("Usuário {Email} não encontrado ou inativo no tenant {Tenant}", request.Email, tenantSubdomain);
            return null;
        }

        // Verificar senha
        if (!BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
        {
            _logger.LogWarning("Senha inválida para usuário {Email} no tenant {Tenant}", request.Email, tenantSubdomain);
            return null;
        }

        // Buscar primeira empresa ativa (default)
        var empresa = await tenantContext.Empresas
            .FirstOrDefaultAsync(e => e.Ativo);

        if (empresa == null)
        {
            _logger.LogWarning("Nenhuma empresa ativa encontrada no tenant {Tenant}", tenantSubdomain);
            return null;
        }

        // Gerar token
        var payload = new TokenPayload
        {
            UserId = usuario.Id,
            EmpresaId = empresa.Id,
            TenantId = tenant.Id,
            Tenant = tenant.Subdomain,
            Role = usuario.Role,
            Type = "user"
        };

        var token = GenerateToken(payload);

        _logger.LogInformation("Login bem-sucedido para usuário {UserId} ({Email}) no tenant {Tenant}", 
            usuario.Id, usuario.Email, tenantSubdomain);

        return new LoginResponse
        {
            Token = token,
            Usuario = new UsuarioResponse
            {
                Id = usuario.Id,
                Nome = usuario.Nome,
                Email = usuario.Email,
                Role = usuario.Role,
                Ativo = usuario.Ativo,
                CreatedAt = usuario.CreatedAt,
                UpdatedAt = usuario.UpdatedAt
            }
        };
    }
}
