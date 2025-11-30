using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Dapper;
using Npgsql;
using SolisApi.Data;
using SolisApi.DTOs;
using BCrypt.Net;

namespace SolisApi.Services;

/// <summary>
/// DTO interno para query de usuário
/// </summary>
internal class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password_Hash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime Created_At { get; set; }
    public DateTime Updated_At { get; set; }
}

/// <summary>
/// DTO interno para query de empresa
/// </summary>
internal class CompanyDto
{
    public Guid Id { get; set; }
}

/// <summary>
/// Serviço de autenticação - geração e validação de tokens JWT
/// </summary>
public class AuthService
{
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly SolisDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IConfiguration configuration, 
        SolisDbContext context,
        ILogger<AuthService> logger)
    {
        _jwtSecret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret não configurado");
        _jwtIssuer = configuration["Jwt:Issuer"] ?? "SolisApi";
        _jwtAudience = configuration["Jwt:Audience"] ?? "SolisApi";
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetConnectionString(string tenantSubdomain)
    {
        var baseConnectionString = _configuration.GetConnectionString("DefaultConnection");
        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            SearchPath = $"tenant_{tenantSubdomain},public"
        };
        return builder.ToString();
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

            var userId = principal.FindFirst("userId")?.Value;
            var empresaId = principal.FindFirst("empresaId")?.Value;
            var tenantId = principal.FindFirst("tenantId")?.Value;
            var tenant = principal.FindFirst("tenant")?.Value;
            var role = principal.FindFirst(ClaimTypes.Role)?.Value; // Use ClaimTypes.Role instead of "role"
            var type = principal.FindFirst("type")?.Value;

            return new TokenPayload
            {
                UserId = Guid.Parse(userId ?? string.Empty),
                EmpresaId = Guid.Parse(empresaId ?? string.Empty),
                TenantId = Guid.Parse(tenantId ?? string.Empty),
                Tenant = tenant ?? string.Empty,
                Role = role ?? string.Empty,
                Type = type ?? string.Empty
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

        // Conectar ao schema do tenant usando Dapper
        using var connection = new NpgsqlConnection(GetConnectionString(tenantSubdomain));
        await connection.OpenAsync();

        // Buscar usuário por email
        var user = await connection.QueryFirstOrDefaultAsync<UserDto>(@"
            SELECT id, name, email, password_hash, role, active, created_at, updated_at
            FROM users
            WHERE LOWER(email) = LOWER(@Email) AND active = true
        ", new { request.Email });

        if (user == null)
        {
            _logger.LogWarning("Usuário {Email} não encontrado ou inativo no tenant {Tenant}", request.Email, tenantSubdomain);
            return null;
        }

        // Verificar senha
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password_Hash))
        {
            _logger.LogWarning("Senha inválida para usuário {Email} no tenant {Tenant}", request.Email, tenantSubdomain);
            return null;
        }

        // Buscar primeira empresa ativa (default)
        var company = await connection.QueryFirstOrDefaultAsync<CompanyDto>(@"
            SELECT id
            FROM companies
            WHERE active = true
            LIMIT 1
        ");

        if (company == null)
        {
            _logger.LogWarning("Nenhuma empresa ativa encontrada no tenant {Tenant}", tenantSubdomain);
            return null;
        }

        // Gerar token
        var payload = new TokenPayload
        {
            UserId = user.Id,
            EmpresaId = company.Id,
            TenantId = tenant.Id,
            Tenant = tenant.Subdomain,
            Role = user.Role,
            Type = "user"
        };

        var token = GenerateToken(payload);

        _logger.LogInformation("Login bem-sucedido para usuário {UserId} ({Email}) no tenant {Tenant}", 
            user.Id, user.Email, tenantSubdomain);

        return new LoginResponse
        {
            Token = token,
            User = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                Active = user.Active,
                CreatedAt = user.Created_At,
                UpdatedAt = user.Updated_At
            }
        };
    }
}
