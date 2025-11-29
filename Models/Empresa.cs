using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SolisApi.Models;

/// <summary>
/// Model Empresa - armazenado nos schemas dos tenants (tenant_*)
/// Dados da empresa para cupom fiscal
/// </summary>
[Table("empresas")]
public class Empresa
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Dados da Empresa
    [Required]
    [MaxLength(255)]
    [Column("razao_social")]
    public string RazaoSocial { get; set; } = string.Empty;

    [MaxLength(255)]
    [Column("nome_fantasia")]
    public string? NomeFantasia { get; set; }

    [Required]
    [MaxLength(18)]
    [Column("cnpj")]
    public string Cnpj { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("inscricao_estadual")]
    public string? InscricaoEstadual { get; set; }

    [MaxLength(20)]
    [Column("inscricao_municipal")]
    public string? InscricaoMunicipal { get; set; }

    // Endereço
    [Required]
    [MaxLength(255)]
    [Column("logradouro")]
    public string Logradouro { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("numero")]
    public string Numero { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("complemento")]
    public string? Complemento { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("bairro")]
    public string Bairro { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("cidade")]
    public string Cidade { get; set; } = string.Empty;

    [Required]
    [MaxLength(2)]
    [Column("uf")]
    public string Uf { get; set; } = string.Empty;

    [Required]
    [MaxLength(9)]
    [Column("cep")]
    public string Cep { get; set; } = string.Empty;

    // Contato
    [MaxLength(20)]
    [Column("telefone")]
    public string? Telefone { get; set; }

    [MaxLength(255)]
    [Column("email")]
    public string? Email { get; set; }

    [MaxLength(255)]
    [Column("site")]
    public string? Site { get; set; }

    // Regime Tributário
    [Required]
    [MaxLength(50)]
    [Column("regime_tributario")]
    public string RegimeTributario { get; set; } = string.Empty; // simples_nacional, lucro_presumido, lucro_real

    // Informações Fiscais
    [Column("certificado_digital", TypeName = "text")]
    public string? CertificadoDigital { get; set; } // Base64 do certificado

    [MaxLength(255)]
    [Column("senha_certificado")]
    public string? SenhaCertificado { get; set; } // Criptografada

    [MaxLength(20)]
    [Column("ambiente_fiscal")]
    public string AmbienteFiscal { get; set; } = "homologacao"; // producao, homologacao

    // Logo (Base64)
    [Column("logo", TypeName = "text")]
    public string? Logo { get; set; }

    // Controle
    [Column("ativo")]
    public bool Ativo { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
