using Microsoft.EntityFrameworkCore;
using SolisApi.Models;
using SolisApi.Models.Reports;

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
    
    // Reports
    public DbSet<Report> Reports { get; set; } = null!;
    public DbSet<ReportField> ReportFields { get; set; } = null!;
    public DbSet<ReportFilter> ReportFilters { get; set; } = null!;
    public DbSet<ReportFilterOption> ReportFilterOptions { get; set; } = null!;

    // Sales Module
    public DbSet<Sale> Sales { get; set; } = null!;
    public DbSet<SaleItem> SaleItems { get; set; } = null!;
    public DbSet<SalePayment> SalePayments { get; set; } = null!;
    public DbSet<SaleTax> SaleTaxes { get; set; } = null!;
    public DbSet<SaleCancellation> SaleCancellations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurar schema public para Tenant
        modelBuilder.Entity<Tenant>()
            .ToTable("tenants", schema: "public");
    }
}
