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

    public JwtAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AuthService authService)
    {
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
