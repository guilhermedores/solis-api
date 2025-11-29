using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SolisApi.Middleware;

/// <summary>
/// Atributo para exigir autenticação em endpoints
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAuthAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                success = false,
                error = "Token não fornecido ou inválido"
            });
        }
    }
}

/// <summary>
/// Atributo para exigir role específica
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _allowedRoles;

    public RequireRoleAttribute(params string[] roles)
    {
        _allowedRoles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                success = false,
                error = "Token não fornecido ou inválido"
            });
            return;
        }

        var userRole = user.FindFirst("role")?.Value;

        if (string.IsNullOrEmpty(userRole) || !_allowedRoles.Contains(userRole))
        {
            context.Result = new ObjectResult(new
            {
                success = false,
                error = "Acesso negado. Permissão insuficiente."
            })
            {
                StatusCode = 403
            };
        }
    }
}

/// <summary>
/// Atributo para exigir role admin
/// </summary>
public class RequireAdminAttribute : RequireRoleAttribute
{
    public RequireAdminAttribute() : base("admin")
    {
    }
}

/// <summary>
/// Atributo para exigir role admin ou manager
/// </summary>
public class RequireManagerAttribute : RequireRoleAttribute
{
    public RequireManagerAttribute() : base("admin", "manager")
    {
    }
}
