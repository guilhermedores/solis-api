-- =============================================================================
-- TABELA DE EMPRESAS (Dados para Cupom Fiscal)
-- Armazena informações da empresa para emissão de documentos fiscais
-- Deve ser criada no schema de cada tenant (ex: tenant_demo.empresas)
-- =============================================================================

-- IMPORTANTE: Este script deve ser executado com o schema path correto
-- SET search_path TO tenant_demo, public;

CREATE TABLE IF NOT EXISTS empresas (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- Dados da Empresa
    razao_social VARCHAR(255) NOT NULL,
    nome_fantasia VARCHAR(255),
    cnpj VARCHAR(18) NOT NULL,
    inscricao_estadual VARCHAR(20),
    inscricao_municipal VARCHAR(20),
    
    -- Endereço
    logradouro VARCHAR(255) NOT NULL,
    numero VARCHAR(20) NOT NULL,
    complemento VARCHAR(100),
    bairro VARCHAR(100) NOT NULL,
    cidade VARCHAR(100) NOT NULL,
    uf VARCHAR(2) NOT NULL,
    cep VARCHAR(9) NOT NULL,
    
    -- Contato
    telefone VARCHAR(20),
    email VARCHAR(255),
    site VARCHAR(255),
    
    -- Regime Tributário
    regime_tributario VARCHAR(50) NOT NULL DEFAULT 'simples_nacional',
    -- Valores: simples_nacional, lucro_presumido, lucro_real
    
    -- Informações Fiscais
    certificado_digital TEXT, -- Base64 do certificado
    senha_certificado VARCHAR(255), -- Criptografada
    ambiente_fiscal VARCHAR(20) DEFAULT 'homologacao', -- producao, homologacao
    
    -- Logo
    logo TEXT, -- Base64 da logo
    
    -- Controle
    ativo BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Índices para performance
CREATE INDEX IF NOT EXISTS idx_empresas_cnpj ON empresas(cnpj);
CREATE INDEX IF NOT EXISTS idx_empresas_ativo ON empresas(ativo);

-- Trigger para atualizar updated_at
CREATE OR REPLACE FUNCTION update_empresas_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_empresas_updated_at
    BEFORE UPDATE ON empresas
    FOR EACH ROW
    EXECUTE FUNCTION update_empresas_updated_at();

-- =============================================================================
-- DADOS DE EXEMPLO
-- =============================================================================

-- Empresa de exemplo para tenant demo
INSERT INTO empresas (
    razao_social,
    nome_fantasia,
    cnpj,
    inscricao_estadual,
    inscricao_municipal,
    logradouro,
    numero,
    complemento,
    bairro,
    cidade,
    uf,
    cep,
    telefone,
    email,
    regime_tributario,
    ambiente_fiscal,
    ativo
) VALUES (
    'EMPRESA DEMO LTDA',
    'Loja Demo',
    '12.345.678/0001-90',
    '123.456.789.012',
    '12345678',
    'Rua Exemplo',
    '123',
    'Sala 1',
    'Centro',
    'São Paulo',
    'SP',
    '01234-567',
    '(11) 1234-5678',
    'contato@empresademo.com.br',
    'simples_nacional',
    'homologacao',
    true
) ON CONFLICT DO NOTHING;

-- Mensagem de confirmação
DO $$
BEGIN
    RAISE NOTICE '=============================================================================';
    RAISE NOTICE 'Tabela empresas criada com sucesso!';
    RAISE NOTICE 'Sistema de cadastro de empresas configurado.';
    RAISE NOTICE '=============================================================================';
END $$;
