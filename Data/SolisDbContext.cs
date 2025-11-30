using Microsoft.EntityFrameworkCore;
using SolisApi.Models;

namespace SolisApi.Data;

/// <summary>
/// DbContext para o schema public - gerenciamento de tenants
/// </summary>
public class SolisDbContext : DbContext
{
    public SolisDbContext(DbContextOptions<SolisDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurar schema public para Tenant
        modelBuilder.Entity<Tenant>()
            .ToTable("tenants", schema: "public");
    }
}
