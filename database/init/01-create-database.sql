-- =============================================================================
-- SCRIPT 01: CRIAR ESTRUTURA BASE DO BANCO DE DADOS
-- =============================================================================
-- Este script cria a estrutura base com o schema public contendo apenas
-- a tabela de tenants
-- =============================================================================

-- Limpar e recriar schema public (CUIDADO EM PRODUÇÃO!)
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;

-- Garantir permissões no schema public
GRANT ALL ON SCHEMA public TO solis_user;
GRANT ALL ON SCHEMA public TO PUBLIC;

-- Criar extensões necessárias (após recriar o schema)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp" SCHEMA public;
CREATE EXTENSION IF NOT EXISTS "pgcrypto" SCHEMA public;

-- =============================================================================
-- TABELA DE TENANTS (ÚNICA TABELA NO SCHEMA PUBLIC)
-- =============================================================================

CREATE TABLE IF NOT EXISTS public."tenants" (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    subdomain VARCHAR(100) UNIQUE NOT NULL,
    company_name VARCHAR(255) NOT NULL,
    cnpj VARCHAR(18) UNIQUE,
    active BOOLEAN DEFAULT true,
    plan VARCHAR(50) DEFAULT 'basic',
    max_terminals INTEGER DEFAULT 1,
    max_users INTEGER DEFAULT 5,
    features JSONB DEFAULT '{}',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP
);

-- Índices para performance
CREATE INDEX IF NOT EXISTS idx_tenants_subdomain_active ON public."tenants"(subdomain, active);
CREATE INDEX IF NOT EXISTS idx_tenants_active ON public."tenants"(active);

-- Garantir permissões na tabela tenants
GRANT ALL ON public."tenants" TO solis_user;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO solis_user;

-- =============================================================================
-- FUNÇÃO PARA ATUALIZAR UPDATED_AT AUTOMATICAMENTE
-- =============================================================================

CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Trigger para atualizar updated_at na tabela tenants
DROP TRIGGER IF EXISTS update_tenants_updated_at ON public."tenants";
CREATE TRIGGER update_tenants_updated_at
    BEFORE UPDATE ON public."tenants"
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- =============================================================================
-- COMENTÁRIOS
-- =============================================================================

COMMENT ON TABLE public."tenants" IS 'Tabela central de tenants - única tabela no schema public';
COMMENT ON COLUMN public."tenants".subdomain IS 'Subdomínio único para identificar o tenant';
COMMENT ON COLUMN public."tenants".active IS 'Indica se o tenant está ativo no sistema';

-- =============================================================================
-- FIM DO SCRIPT 01
-- =============================================================================
