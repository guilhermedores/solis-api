using SolisApi.DTOs;
using SolisApi.Services;
using System.Security.Claims;

namespace SolisApi.Middleware;

/// <summary>
/// Middleware de autenticação JWT
/// Valida o token e adiciona claims ao HttpContext
/// </summary>
public class JwtAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public JwtAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context, AuthService authService)
    {
        // Autenticação por API Key interna (comunicação server-to-server)
        var apiKey = context.Request.Headers["X-Internal-Api-Key"].FirstOrDefault();
        var configuredKey = _configuration["InternalApiKey"];

        if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(configuredKey) && apiKey == configuredKey)
        {
            var tenantSubdomain = context.Request.Headers["X-Tenant-Subdomain"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(tenantSubdomain))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "X-Tenant-Subdomain header is required for internal API calls" });
                return;
            }
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "internal"),
                new("userId", "00000000-0000-0000-0000-000000000000"),
                new("empresaId", "00000000-0000-0000-0000-000000000000"),
                new("tenantId", "00000000-0000-0000-0000-000000000000"),
                new("tenant", tenantSubdomain!),
                new(ClaimTypes.Role, "admin"),
                new("role", "admin"),
                new("type", "internal"),
            };
            var identity = new ClaimsIdentity(claims, "ApiKey", ClaimTypes.NameIdentifier, ClaimTypes.Role);
            context.User = new ClaimsPrincipal(identity);
            context.Items["TenantSubdomain"] = tenantSubdomain!;
            context.Items["UserRole"] = "admin";
            await _next(context);
            return;
        }

        // Buscar token no header Authorization
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();

            // Validar token
            var payload = authService.ValidateToken(token);

            if (payload != null)
            {
                // Adicionar claims ao contexto
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, payload.UserId.ToString()),
                    new("userId", payload.UserId.ToString()),
                    new("empresaId", payload.EmpresaId.ToString()),
                    new("tenantId", payload.TenantId.ToString()),
                    new("tenant", payload.Tenant),
                    new(ClaimTypes.Role, payload.Role),
                    new("role", payload.Role),
                    new("type", payload.Type)
                };

                var identity = new ClaimsIdentity(claims, "Bearer", ClaimTypes.NameIdentifier, ClaimTypes.Role);
                context.User = new ClaimsPrincipal(identity);

                // Adicionar ao HttpContext.Items para fácil acesso
                context.Items["TenantSubdomain"] = payload.Tenant;
                context.Items["UserRole"] = payload.Role;
                context.Items["UserId"] = payload.UserId;
                context.Items["StoreId"] = payload.StoreId;
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method para adicionar JwtAuthMiddleware ao pipeline
/// </summary>
public static class JwtAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtAuthMiddleware>();
    }
}
