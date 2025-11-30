using Microsoft.AspNetCore.Mvc;
using SolisApi.DTOs;
using SolisApi.Services;
using SolisApi.Middleware;

namespace SolisApi.Controllers;

/// <summary>
/// Controller de gerenciamento de empresas
/// </summary>
[ApiController]
[Route("api/companies")]
[RequireAuth]
public class CompaniesController : ControllerBase
{
    private readonly CompanyService _companyService;

    public CompaniesController(CompanyService companyService)
    {
        _companyService = companyService;
    }

    /// <summary>
    /// Listar todas as empresas do tenant
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListCompanies()
    {
        var tenant = User.FindFirst("tenant")?.Value;

        if (string.IsNullOrEmpty(tenant))
        {
            return Unauthorized(new ErrorResponse { Error = "Tenant não identificado" });
        }

        var companies = await _companyService.ListCompaniesAsync(tenant);
        return Ok(companies);
    }

    /// <summary>
    /// Buscar empresa por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCompany(Guid id)
    {
        var tenant = User.FindFirst("tenant")?.Value;

        if (string.IsNullOrEmpty(tenant))
        {
            return Unauthorized(new ErrorResponse { Error = "Tenant não identificado" });
        }

        var company = await _companyService.GetCompanyByIdAsync(tenant, id);

        if (company == null)
        {
            return NotFound(new ErrorResponse { Error = "Empresa não encontrada" });
        }

        return Ok(company);
    }

    /// <summary>
    /// Criar nova empresa
    /// </summary>
    [HttpPost]
    [RequireManager]
    public async Task<IActionResult> CreateCompany([FromBody] CreateCompanyRequest request)
    {
        var tenant = User.FindFirst("tenant")?.Value;

        if (string.IsNullOrEmpty(tenant))
        {
            return Unauthorized(new ErrorResponse { Error = "Tenant não identificado" });
        }

        // Validações básicas
        if (string.IsNullOrWhiteSpace(request.LegalName))
        {
            return BadRequest(new ErrorResponse { Error = "Razão social é obrigatória" });
        }

        if (string.IsNullOrWhiteSpace(request.Cnpj) || request.Cnpj.Length != 14)
        {
            return BadRequest(new ErrorResponse { Error = "CNPJ inválido (deve ter 14 dígitos)" });
        }

        if (string.IsNullOrWhiteSpace(request.StateRegistration))
        {
            return BadRequest(new ErrorResponse { Error = "Inscrição estadual é obrigatória" });
        }

        if (string.IsNullOrWhiteSpace(request.Cnae))
        {
            return BadRequest(new ErrorResponse { Error = "CNAE é obrigatório" });
        }

        if (request.TaxRegimeId == Guid.Empty)
        {
            return BadRequest(new ErrorResponse { Error = "Regime tributário é obrigatório" });
        }

        // Validar endereço
        if (string.IsNullOrWhiteSpace(request.Address.ZipCode) ||
            string.IsNullOrWhiteSpace(request.Address.Street) ||
            string.IsNullOrWhiteSpace(request.Address.Number) ||
            string.IsNullOrWhiteSpace(request.Address.District) ||
            string.IsNullOrWhiteSpace(request.Address.City) ||
            string.IsNullOrWhiteSpace(request.Address.State))
        {
            return BadRequest(new ErrorResponse { Error = "Endereço completo é obrigatório" });
        }

        var company = await _companyService.CreateCompanyAsync(tenant, request);

        if (company == null)
        {
            return BadRequest(new ErrorResponse { Error = "CNPJ já cadastrado ou regime tributário inválido" });
        }

        return CreatedAtAction(nameof(GetCompany), new { id = company.Id }, company);
    }

    /// <summary>
    /// Atualizar empresa existente
    /// </summary>
    [HttpPut("{id}")]
    [RequireManager]
    public async Task<IActionResult> UpdateCompany(Guid id, [FromBody] UpdateCompanyRequest request)
    {
        var tenant = User.FindFirst("tenant")?.Value;

        if (string.IsNullOrEmpty(tenant))
        {
            return Unauthorized(new ErrorResponse { Error = "Tenant não identificado" });
        }

        var company = await _companyService.UpdateCompanyAsync(tenant, id, request);

        if (company == null)
        {
            return NotFound(new ErrorResponse { Error = "Empresa não encontrada ou regime tributário inválido" });
        }

        return Ok(company);
    }

    /// <summary>
    /// Desativar empresa
    /// </summary>
    [HttpDelete("{id}")]
    [RequireAdmin]
    public async Task<IActionResult> DeleteCompany(Guid id)
    {
        var tenant = User.FindFirst("tenant")?.Value;

        if (string.IsNullOrEmpty(tenant))
        {
            return Unauthorized(new ErrorResponse { Error = "Tenant não identificado" });
        }

        var success = await _companyService.DeactivateCompanyAsync(tenant, id);

        if (!success)
        {
            return NotFound(new ErrorResponse { Error = "Empresa não encontrada" });
        }

        return Ok(new SuccessResponse { Message = "Empresa desativada com sucesso" });
    }

    /// <summary>
    /// Reativar empresa
    /// </summary>
    [HttpPost("{id}/reactivate")]
    [RequireAdmin]
    public async Task<IActionResult> ReactivateCompany(Guid id)
    {
        var tenant = User.FindFirst("tenant")?.Value;

        if (string.IsNullOrEmpty(tenant))
        {
            return Unauthorized(new ErrorResponse { Error = "Tenant não identificado" });
        }

        var success = await _companyService.ReactivateCompanyAsync(tenant, id);

        if (!success)
        {
            return NotFound(new ErrorResponse { Error = "Empresa não encontrada" });
        }

        return Ok(new SuccessResponse { Message = "Empresa reativada com sucesso" });
    }
}
