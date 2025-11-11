-- =============================================================================
-- SCRIPT 03: CRIAR TABELAS DO TENANT
-- =============================================================================
-- Este script cria as tabelas no schema do tenant
-- Substitua 'tenant_demo' pelo schema do tenant específico ao criar novos tenants
-- =============================================================================

-- =============================================================================
-- TABELA DE USUÁRIOS
-- =============================================================================

CREATE TABLE IF NOT EXISTS tenant_demo."users" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    nome VARCHAR(255) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(50) NOT NULL DEFAULT 'operator',
    ativo BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT users_role_check CHECK (role IN ('admin', 'manager', 'operator'))
);

-- =============================================================================
-- TABELA DE EMPRESAS (FILIAIS/LOJAS DO TENANT)
-- =============================================================================

CREATE TABLE IF NOT EXISTS tenant_demo."empresas" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    razao_social VARCHAR(255) NOT NULL,
    nome_fantasia VARCHAR(255),
    cnpj VARCHAR(14),
    logradouro VARCHAR(255),
    numero VARCHAR(20),
    complemento VARCHAR(100),
    bairro VARCHAR(100),
    cidade VARCHAR(100),
    uf VARCHAR(2),
    cep VARCHAR(9),
    telefone VARCHAR(20),
    email VARCHAR(255),
    regime_tributario VARCHAR(50),
    ativo BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- =============================================================================
-- ÍNDICES PARA PERFORMANCE
-- =============================================================================

CREATE INDEX IF NOT EXISTS idx_usuarios_email ON tenant_demo."usuarios"(email);
CREATE INDEX IF NOT EXISTS idx_usuarios_role ON tenant_demo."usuarios"(role);
CREATE INDEX IF NOT EXISTS idx_empresas_cnpj ON tenant_demo."empresas"(cnpj);

-- =============================================================================
-- TRIGGERS PARA UPDATED_AT
-- =============================================================================

DROP TRIGGER IF EXISTS update_usuarios_updated_at ON tenant_demo."usuarios";
CREATE TRIGGER update_usuarios_updated_at
    BEFORE UPDATE ON tenant_demo."usuarios"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_empresas_updated_at ON tenant_demo."empresas";
CREATE TRIGGER update_empresas_updated_at
    BEFORE UPDATE ON tenant_demo."empresas"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- =============================================================================
-- GARANTIR PERMISSÕES
-- =============================================================================

GRANT ALL ON ALL TABLES IN SCHEMA tenant_demo TO solis_user;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA tenant_demo TO solis_user;

-- =============================================================================
-- COMENTÁRIOS
-- =============================================================================

COMMENT ON TABLE tenant_demo."usuarios" IS 'Usuários do tenant com diferentes níveis de acesso';
COMMENT ON TABLE tenant_demo."empresas" IS 'Empresas/filiais/lojas do tenant';

-- =============================================================================
-- FIM DO SCRIPT 03
-- =============================================================================
