using SolisApi.Models.ValueObjects;

namespace SolisApi.DTOs;

/// <summary>
/// DTO de resposta da Company
/// </summary>
public class CompanyResponse
{
    public Guid Id { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? LogoUrl { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string StateRegistration { get; set; } = string.Empty;
    public string? CityRegistration { get; set; }
    public string Cnae { get; set; } = string.Empty;
    public Guid TaxRegimeId { get; set; }
    public string? TaxRegimeName { get; set; }
    public Guid? SpecialTaxRegimeId { get; set; }
    public string? SpecialTaxRegimeName { get; set; }
    public AddressResponse Address { get; set; } = new();
    public ContactResponse Contact { get; set; } = new();
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO para criar Company
/// </summary>
public class CreateCompanyRequest
{
    public string LegalName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? LogoUrl { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string StateRegistration { get; set; } = string.Empty;
    public string? CityRegistration { get; set; }
    public string Cnae { get; set; } = string.Empty;
    public Guid TaxRegimeId { get; set; }
    public Guid? SpecialTaxRegimeId { get; set; }
    public AddressRequest Address { get; set; } = new();
    public ContactRequest Contact { get; set; } = new();
}

/// <summary>
/// DTO para atualizar Company
/// </summary>
public class UpdateCompanyRequest
{
    public string? LegalName { get; set; }
    public string? TradeName { get; set; }
    public string? LogoUrl { get; set; }
    public string? StateRegistration { get; set; }
    public string? CityRegistration { get; set; }
    public string? Cnae { get; set; }
    public Guid? TaxRegimeId { get; set; }
    public Guid? SpecialTaxRegimeId { get; set; }
    public AddressRequest? Address { get; set; }
    public ContactRequest? Contact { get; set; }
}

/// <summary>
/// DTO de endereço para request
/// </summary>
public class AddressRequest
{
    public string ZipCode { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Complement { get; set; }
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

/// <summary>
/// DTO de endereço para response
/// </summary>
public class AddressResponse
{
    public string ZipCode { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Complement { get; set; }
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

/// <summary>
/// DTO de contato para request
/// </summary>
public class ContactRequest
{
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
}

/// <summary>
/// DTO de contato para response
/// </summary>
public class ContactResponse
{
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
}
