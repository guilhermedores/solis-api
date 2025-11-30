using Microsoft.EntityFrameworkCore;
using SolisApi.Data;
using SolisApi.DTOs;
using SolisApi.Models;
using SolisApi.Models.ValueObjects;

namespace SolisApi.Services;

/// <summary>
/// Serviço de gerenciamento de empresas
/// </summary>
public class CompanyService
{
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ILogger<CompanyService> _logger;

    public CompanyService(
        ITenantDbContextFactory tenantDbContextFactory,
        ILogger<CompanyService> logger)
    {
        _tenantDbContextFactory = tenantDbContextFactory;
        _logger = logger;
    }

    /// <summary>
    /// Criar DbContext do tenant usando factory
    /// </summary>
    private TenantDbContext CreateTenantContext(string tenantSubdomain)
    {
        return _tenantDbContextFactory.CreateDbContext(tenantSubdomain);
    }

    /// <summary>
    /// Listar todas as empresas do tenant
    /// </summary>
    public async Task<List<CompanyResponse>> ListCompaniesAsync(string tenantSubdomain)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        var companies = await tenantContext.Companies
            .Include(c => c.TaxRegime)
            .Include(c => c.SpecialTaxRegime)
            .OrderBy(c => c.LegalName)
            .ToListAsync();

        return companies.Select(c => MapToResponse(c)).ToList();
    }

    /// <summary>
    /// Buscar empresa por ID
    /// </summary>
    public async Task<CompanyResponse?> GetCompanyByIdAsync(string tenantSubdomain, Guid id)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        var company = await tenantContext.Companies
            .Include(c => c.TaxRegime)
            .Include(c => c.SpecialTaxRegime)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (company == null)
        {
            return null;
        }

        return MapToResponse(company);
    }

    /// <summary>
    /// Criar nova empresa
    /// </summary>
    public async Task<CompanyResponse?> CreateCompanyAsync(string tenantSubdomain, CreateCompanyRequest request)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        // Verificar se CNPJ já existe
        var cnpjExists = await tenantContext.Companies
            .AnyAsync(c => c.Cnpj == request.Cnpj);

        if (cnpjExists)
        {
            _logger.LogWarning("Tentativa de criar empresa com CNPJ duplicado {Cnpj} no tenant {Tenant}", 
                request.Cnpj, tenantSubdomain);
            return null;
        }

        // Verificar se regime tributário existe
        var taxRegimeExists = await tenantContext.TaxRegimes
            .AnyAsync(r => r.Id == request.TaxRegimeId && r.Active);

        if (!taxRegimeExists)
        {
            _logger.LogWarning("Regime tributário {TaxRegimeId} não encontrado ou inativo", request.TaxRegimeId);
            return null;
        }

        // Verificar regime especial se informado
        if (request.SpecialTaxRegimeId.HasValue)
        {
            var specialTaxRegimeExists = await tenantContext.SpecialTaxRegimes
                .AnyAsync(r => r.Id == request.SpecialTaxRegimeId.Value && r.Active);

            if (!specialTaxRegimeExists)
            {
                _logger.LogWarning("Regime especial de tributação {SpecialTaxRegimeId} não encontrado ou inativo", 
                    request.SpecialTaxRegimeId);
                return null;
            }
        }

        var company = new Company
        {
            LegalName = request.LegalName,
            TradeName = request.TradeName,
            LogoUrl = request.LogoUrl,
            Cnpj = request.Cnpj,
            StateRegistration = request.StateRegistration,
            CityRegistration = request.CityRegistration,
            Cnae = request.Cnae,
            TaxRegimeId = request.TaxRegimeId,
            SpecialTaxRegimeId = request.SpecialTaxRegimeId,
            Address = new Address(
                request.Address.ZipCode,
                request.Address.Street,
                request.Address.Number,
                request.Address.District,
                request.Address.City,
                request.Address.State,
                request.Address.Complement
            ),
            Contact = new Contact(
                request.Contact.Phone,
                request.Contact.Mobile,
                request.Contact.Email
            ),
            Active = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        tenantContext.Companies.Add(company);
        await tenantContext.SaveChangesAsync();

        _logger.LogInformation("Empresa criada: {CompanyId} ({LegalName}) no tenant {Tenant}", 
            company.Id, company.LegalName, tenantSubdomain);

        // Recarregar com relacionamentos
        await tenantContext.Entry(company)
            .Reference(c => c.TaxRegime)
            .LoadAsync();

        if (company.SpecialTaxRegimeId.HasValue)
        {
            await tenantContext.Entry(company)
                .Reference(c => c.SpecialTaxRegime)
                .LoadAsync();
        }

        return MapToResponse(company);
    }

    /// <summary>
    /// Atualizar empresa existente
    /// </summary>
    public async Task<CompanyResponse?> UpdateCompanyAsync(string tenantSubdomain, Guid id, UpdateCompanyRequest request)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        var company = await tenantContext.Companies.FindAsync(id);

        if (company == null)
        {
            return null;
        }

        // Atualizar campos se fornecidos
        if (!string.IsNullOrWhiteSpace(request.LegalName))
        {
            company.LegalName = request.LegalName;
        }

        if (request.TradeName != null)
        {
            company.TradeName = request.TradeName;
        }

        if (request.LogoUrl != null)
        {
            company.LogoUrl = request.LogoUrl;
        }

        if (!string.IsNullOrWhiteSpace(request.StateRegistration))
        {
            company.StateRegistration = request.StateRegistration;
        }

        if (request.CityRegistration != null)
        {
            company.CityRegistration = request.CityRegistration;
        }

        if (!string.IsNullOrWhiteSpace(request.Cnae))
        {
            company.Cnae = request.Cnae;
        }

        if (request.TaxRegimeId.HasValue)
        {
            var taxRegimeExists = await tenantContext.TaxRegimes
                .AnyAsync(r => r.Id == request.TaxRegimeId.Value && r.Active);

            if (!taxRegimeExists)
            {
                _logger.LogWarning("Regime tributário {TaxRegimeId} não encontrado ou inativo", request.TaxRegimeId);
                return null;
            }

            company.TaxRegimeId = request.TaxRegimeId.Value;
        }

        if (request.SpecialTaxRegimeId.HasValue)
        {
            var specialTaxRegimeExists = await tenantContext.SpecialTaxRegimes
                .AnyAsync(r => r.Id == request.SpecialTaxRegimeId.Value && r.Active);

            if (!specialTaxRegimeExists)
            {
                _logger.LogWarning("Regime especial de tributação {SpecialTaxRegimeId} não encontrado ou inativo", 
                    request.SpecialTaxRegimeId);
                return null;
            }

            company.SpecialTaxRegimeId = request.SpecialTaxRegimeId.Value;
        }

        if (request.Address != null)
        {
            company.Address = new Address(
                request.Address.ZipCode,
                request.Address.Street,
                request.Address.Number,
                request.Address.District,
                request.Address.City,
                request.Address.State,
                request.Address.Complement
            );
        }

        if (request.Contact != null)
        {
            company.Contact = new Contact(
                request.Contact.Phone,
                request.Contact.Mobile,
                request.Contact.Email
            );
        }

        company.UpdatedAt = DateTime.UtcNow;

        await tenantContext.SaveChangesAsync();

        _logger.LogInformation("Empresa atualizada: {CompanyId} no tenant {Tenant}", id, tenantSubdomain);

        // Recarregar com relacionamentos
        await tenantContext.Entry(company)
            .Reference(c => c.TaxRegime)
            .LoadAsync();

        if (company.SpecialTaxRegimeId.HasValue)
        {
            await tenantContext.Entry(company)
                .Reference(c => c.SpecialTaxRegime)
                .LoadAsync();
        }

        return MapToResponse(company);
    }

    /// <summary>
    /// Desativar empresa (soft delete)
    /// </summary>
    public async Task<bool> DeactivateCompanyAsync(string tenantSubdomain, Guid id)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        var company = await tenantContext.Companies.FindAsync(id);

        if (company == null)
        {
            return false;
        }

        company.Active = false;
        company.UpdatedAt = DateTime.UtcNow;

        await tenantContext.SaveChangesAsync();

        _logger.LogInformation("Empresa desativada: {CompanyId} no tenant {Tenant}", id, tenantSubdomain);

        return true;
    }

    /// <summary>
    /// Reativar empresa
    /// </summary>
    public async Task<bool> ReactivateCompanyAsync(string tenantSubdomain, Guid id)
    {
        using var tenantContext = CreateTenantContext(tenantSubdomain);

        var company = await tenantContext.Companies.FindAsync(id);

        if (company == null)
        {
            return false;
        }

        company.Active = true;
        company.UpdatedAt = DateTime.UtcNow;

        await tenantContext.SaveChangesAsync();

        _logger.LogInformation("Empresa reativada: {CompanyId} no tenant {Tenant}", id, tenantSubdomain);

        return true;
    }

    /// <summary>
    /// Mapear entidade para DTO
    /// </summary>
    private CompanyResponse MapToResponse(Company company)
    {
        return new CompanyResponse
        {
            Id = company.Id,
            LegalName = company.LegalName,
            TradeName = company.TradeName,
            LogoUrl = company.LogoUrl,
            Cnpj = company.Cnpj,
            StateRegistration = company.StateRegistration,
            CityRegistration = company.CityRegistration,
            Cnae = company.Cnae,
            TaxRegimeId = company.TaxRegimeId,
            TaxRegimeName = company.TaxRegime?.Description,
            SpecialTaxRegimeId = company.SpecialTaxRegimeId,
            SpecialTaxRegimeName = company.SpecialTaxRegime?.Description,
            Address = new AddressResponse
            {
                ZipCode = company.Address.ZipCode,
                Street = company.Address.Street,
                Number = company.Address.Number,
                Complement = company.Address.Complement,
                District = company.Address.District,
                City = company.Address.City,
                State = company.Address.State
            },
            Contact = new ContactResponse
            {
                Phone = company.Contact.Phone,
                Mobile = company.Contact.Mobile,
                Email = company.Contact.Email
            },
            Active = company.Active,
            CreatedAt = company.CreatedAt,
            UpdatedAt = company.UpdatedAt
        };
    }
}
