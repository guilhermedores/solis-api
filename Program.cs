using Microsoft.EntityFrameworkCore;
using SolisApi.Data;
using SolisApi.Services;
using SolisApi.Services.Domain;
using SolisApi.Repositories;
using SolisApi.Middleware;
using Serilog;

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
        .Build())
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/solis-api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Iniciando Solis API");

var builder = WebApplication.CreateBuilder(args);

// Usar Serilog
builder.Host.UseSerilog();

// Configuração do CORS
// ===== CORS ORIGINS =====
var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS");
if (string.IsNullOrEmpty(corsOrigins))
{
    corsOrigins = "http://localhost:5173,http://localhost:5174"; // Fallback para dev local
}

// Configurar CORS com suporte a wildcard (*.projetosolis.com.br)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSolisAdmin", policy =>
    {
        var origins = corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(o => o.Trim())
                                 .ToArray();
        
        // Se tem wildcard (*.), usar SetIsOriginAllowed
        if (origins.Any(o => o.Contains("*")))
        {
            policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin))
                    return false;

                foreach (var allowedOrigin in origins)
                {
                    // Se for wildcard (ex: https://*.projetosolis.com.br)
                    if (allowedOrigin.Contains("*"))
                    {
                        var pattern = allowedOrigin.Replace("*", ".*");
                        if (System.Text.RegularExpressions.Regex.IsMatch(origin, pattern))
                            return true;
                    }
                    // Se for origem exata
                    else if (origin.Equals(allowedOrigin, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
        }
        else
        {
            // Se não tem wildcard, usar WithOrigins normal
            policy.WithOrigins(origins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// Adicionar controllers
builder.Services.AddControllers();

// Configurar DbContext para o schema public (tenants)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Validar JWT Secret
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["Jwt:Secret"];

if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 32)
{
    throw new InvalidOperationException("JWT Secret deve ter no mínimo 32 caracteres. Configure JWT_SECRET ou Jwt:Secret em appsettings.json.");
}

// Atualizar configuração com a secret da variável de ambiente
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JWT_SECRET")))
{
    builder.Configuration["Jwt:Secret"] = jwtSecret;
}

// DbContext para o schema public (tenants)
builder.Services.AddDbContext<SolisDbContext>(options =>
    options.UseNpgsql(connectionString));

// Registrar IHttpContextAccessor (necessário para ReportService)
builder.Services.AddHttpContextAccessor();

// Registrar serviços
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DynamicCrudService>();
builder.Services.AddScoped<ReportService>();

// Sales Module Services (DDD)
builder.Services.AddScoped<ITaxDomainService, TaxDomainService>();
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<ISalesService, SalesApplicationService>();

// Health Checks - sem DbContext check para evitar problemas de scoping
builder.Services.AddHealthChecks();

// Configurar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Solis API", Version = "v1" });
    
    // Configurar autenticação JWT no Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando Bearer scheme. Exemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Adicionar header X-Tenant-Subdomain para todos os endpoints
    c.OperationFilter<TenantHeaderOperationFilter>();
});

var app = builder.Build();

// Middleware global de exceções (deve ser o primeiro)
app.UseGlobalExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Solis API v1");
    c.RoutePrefix = "docs"; // Acesso via /docs
});

app.UseCors("AllowSolisAdmin");

app.UseHttpsRedirection();

// Serilog request logging
app.UseSerilogRequestLogging();

// Middleware de autenticação JWT customizado
app.UseJwtAuth();

app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação falhou ao iniciar");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
