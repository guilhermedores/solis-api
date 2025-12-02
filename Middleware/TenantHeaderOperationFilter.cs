using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SolisApi.Middleware;

/// <summary>
/// Adiciona o header X-Tenant-Subdomain em todos os endpoints do Swagger
/// </summary>
public class TenantHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        // Verificar se o endpoint é público (não requer tenant)
        var isPublicEndpoint = context.ApiDescription.RelativePath?.Contains("tenants/check") == true
                            || context.ApiDescription.RelativePath?.Contains("health") == true;

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Tenant-Subdomain",
            In = ParameterLocation.Header,
            Description = "Subdomain do tenant (ex: demo, acme). Obrigatório para endpoints autenticados.",
            Required = !isPublicEndpoint, // Opcional apenas para endpoints públicos
            Schema = new OpenApiSchema
            {
                Type = "string",
                Default = new Microsoft.OpenApi.Any.OpenApiString("demo")
            }
        });
    }
}
