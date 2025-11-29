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

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<TaxRegime> TaxRegimes { get; set; } = null!;
    public DbSet<SpecialTaxRegime> SpecialTaxRegimes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurar schema dinâmico do tenant
        modelBuilder.HasDefaultSchema(_tenantSchema);

        // Configurações User
        modelBuilder.Entity<User>()
            .ToTable("users", schema: _tenantSchema);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Configurações Company
        modelBuilder.Entity<Company>()
            .ToTable("empresas", schema: _tenantSchema);

        modelBuilder.Entity<Company>()
            .HasIndex(e => e.Cnpj)
            .IsUnique();

        // Configurar Value Objects da Company
        modelBuilder.Entity<Company>()
            .OwnsOne(e => e.Address, address =>
            {
                address.Property(a => a.ZipCode).HasColumnName("cep").IsRequired();
                address.Property(a => a.Street).HasColumnName("logradouro").IsRequired();
                address.Property(a => a.Number).HasColumnName("numero").IsRequired();
                address.Property(a => a.Complement).HasColumnName("complemento");
                address.Property(a => a.District).HasColumnName("bairro").IsRequired();
                address.Property(a => a.City).HasColumnName("cidade").IsRequired();
                address.Property(a => a.State).HasColumnName("uf").IsRequired();
            });

        modelBuilder.Entity<Company>()
            .OwnsOne(e => e.Contact, contact =>
            {
                contact.Property(c => c.Phone).HasColumnName("telefone_fixo");
                contact.Property(c => c.Mobile).HasColumnName("celular_whatsapp");
                contact.Property(c => c.Email).HasColumnName("email_comercial");
            });

        modelBuilder.Entity<Company>()
            .HasOne(e => e.TaxRegime)
            .WithMany(r => r.Companies)
            .HasForeignKey(e => e.TaxRegimeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Company>()
            .HasOne(e => e.SpecialTaxRegime)
            .WithMany(r => r.Companies)
            .HasForeignKey(e => e.SpecialTaxRegimeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configurações TaxRegime
        modelBuilder.Entity<TaxRegime>()
            .ToTable("regimes_tributarios", schema: _tenantSchema);

        modelBuilder.Entity<TaxRegime>()
            .HasIndex(r => r.Code)
            .IsUnique();

        // Configurações SpecialTaxRegime
        modelBuilder.Entity<SpecialTaxRegime>()
            .ToTable("regimes_especiais_tributacao", schema: _tenantSchema);

        modelBuilder.Entity<SpecialTaxRegime>()
            .HasIndex(r => r.Code)
            .IsUnique();
    }
}
