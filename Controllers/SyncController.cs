using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SolisApi.Middleware;

namespace SolisApi.Controllers;

[ApiController]
[Route("api/sync")]
[RequireAuth]
public class SyncController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SyncController> _logger;

    public SyncController(IConfiguration configuration, ILogger<SyncController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private string GetConnectionString(string tenantSubdomain)
    {
        var baseConnectionString = _configuration.GetConnectionString("DefaultConnection");
        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            SearchPath = $"tenant_{tenantSubdomain},public"
        };
        return builder.ToString();
    }

    private string GetTenantSubdomain()
    {
        return HttpContext.Items["TenantSubdomain"]?.ToString() ?? "demo";
    }

    private Guid GetStoreId()
    {
        var raw = HttpContext.Items["StoreId"];
        if (raw is Guid g) return g;
        if (raw is string s && Guid.TryParse(s, out var parsed)) return parsed;
        return Guid.Empty;
    }

    /// <summary>
    /// Retorna a empresa vinculada à loja do agente para sincronização.
    /// </summary>
    [HttpGet("company")]
    public async Task<IActionResult> GetCompany()
    {
        var tenant = GetTenantSubdomain();
        var storeId = GetStoreId();

        if (storeId == Guid.Empty)
            return BadRequest(new { error = "StoreId não encontrado no token do agente" });

        try
        {
            using var connection = new NpgsqlConnection(GetConnectionString(tenant));

            const string sql = """
                SELECT
                    c.id,
                    c.legal_name       AS razao_social,
                    c.trade_name       AS nome_fantasia,
                    c.cnpj,
                    c.state_registration AS inscricao_estadual,
                    c.city_registration  AS inscricao_municipal,
                    c.address_street   AS logradouro,
                    c.address_number   AS numero,
                    c.address_complement AS complemento,
                    c.address_district AS bairro,
                    c.address_city     AS cidade,
                    c.address_state    AS uf,
                    c.address_zip_code AS cep,
                    c.contact_phone    AS telefone,
                    c.contact_email    AS email,
                    c.logo_url         AS logo,
                    c.updated_at,
                    tr.code            AS regime_tributario
                FROM stores s
                JOIN companies c ON c.id = s.company_id
                LEFT JOIN tax_regimes tr ON tr.id = c.tax_regime_id
                WHERE s.id = @StoreId
                  AND s.active = true
                  AND c.active = true
                LIMIT 1
                """;

            var empresa = await connection.QueryFirstOrDefaultAsync(sql, new { StoreId = storeId });

            if (empresa == null)
                return NotFound(new { error = "Empresa não encontrada para a loja do agente" });

            return Ok(empresa);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar empresa para sincronização (tenant: {Tenant}, store: {StoreId})", tenant, storeId);
            return StatusCode(500, new { error = "Erro interno ao buscar empresa" });
        }
    }

    /// <summary>
    /// Retorna produtos com preço, NCM, CEST e unidade de medida para sincronização do agente.
    /// </summary>
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        var tenant = GetTenantSubdomain();
        try
        {
            using var connection = new NpgsqlConnection(GetConnectionString(tenant));

            const string sql = """
                SELECT
                    p.id,
                    p.internal_code   AS codigo_interno,
                    p.barcode         AS codigo_barras,
                    p.description     AS nome,
                    p.active          AS ativo,
                    p.product_origin,
                    p.item_type,
                    p.incide_pis_cofins,
                    p.created_at,
                    p.updated_at,
                    ncm.code          AS ncm,
                    cest.code         AS cest,
                    uom.code          AS unidade_medida,
                    pp.price          AS preco_venda
                FROM products p
                LEFT JOIN ncm_codes  ncm  ON ncm.id  = p.ncm_id
                LEFT JOIN cest_codes cest ON cest.id = p.cest_id
                LEFT JOIN unit_of_measures uom ON uom.id = p.unit_of_measure_id
                LEFT JOIN LATERAL (
                    SELECT price FROM product_prices
                    WHERE product_id = p.id AND active = true
                    ORDER BY effective_date DESC
                    LIMIT 1
                ) pp ON true
                WHERE p.active = true
                ORDER BY p.description
                """;

            var produtos = (await connection.QueryAsync(sql)).ToList();

            return Ok(new { produtos, total = produtos.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar produtos para sincronização (tenant: {Tenant})", tenant);
            return StatusCode(500, new { error = "Erro interno ao buscar produtos" });
        }
    }
}
