using Microsoft.EntityFrameworkCore;
using SolisApi.Models;

namespace SolisApi.Data;

/// <summary>
/// DbContext para schemas dos tenants (tenant_demo, tenant_cliente1, etc.)
/// Armazena dados específicos do tenant: Usuarios e Empresas
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly string _tenantSchema;

    public TenantDbContext(DbContextOptions<TenantDbContext> options, string tenantSchema) : base(options)
    {
        _tenantSchema = tenantSchema;
    }

    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Empresa> Empresas { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurar schema dinâmico do tenant
        modelBuilder.HasDefaultSchema(_tenantSchema);

        // Configurações Usuario
        modelBuilder.Entity<Usuario>()
            .ToTable("users", schema: _tenantSchema);

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Configurações Empresa
        modelBuilder.Entity<Empresa>()
            .ToTable("empresas", schema: _tenantSchema);

        modelBuilder.Entity<Empresa>()
            .HasIndex(e => e.Cnpj)
            .IsUnique();
    }
}
