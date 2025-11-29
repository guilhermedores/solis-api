using Microsoft.EntityFrameworkCore;
using SolisApi.Models;

namespace SolisApi.Data;

/// <summary>
/// Interface para factory de TenantDbContext
/// </summary>
public interface ITenantDbContextFactory
{
    TenantDbContext CreateDbContext(string tenantSubdomain);
}

/// <summary>
/// Factory para criar instâncias de TenantDbContext com pooling de conexões
/// </summary>
public class TenantDbContextFactory : ITenantDbContextFactory
{
    private readonly IDbContextFactory<SolisDbContext> _mainDbContextFactory;
    private readonly IConfiguration _configuration;

    public TenantDbContextFactory(
        IDbContextFactory<SolisDbContext> mainDbContextFactory,
        IConfiguration configuration)
    {
        _mainDbContextFactory = mainDbContextFactory;
        _configuration = configuration;
    }

    public TenantDbContext CreateDbContext(string tenantSubdomain)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string não configurada");
        }

        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        var tenantSchema = $"tenant_{tenantSubdomain}";
        return new TenantDbContext(optionsBuilder.Options, tenantSchema);
    }
}
