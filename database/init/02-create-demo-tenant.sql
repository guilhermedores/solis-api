-- =============================================================================
-- SCRIPT 02: CRIAR TENANT DEMO
-- =============================================================================
-- Este script cria o tenant de demonstração e seu schema
-- =============================================================================

-- Inserir tenant de demonstração
INSERT INTO public."tenants" (subdomain, company_name, active)
VALUES ('demo', 'Tenant Demo', true)
ON CONFLICT (subdomain) DO NOTHING;

-- Criar schema para o tenant demo
CREATE SCHEMA IF NOT EXISTS tenant_demo;

-- Garantir permissões no schema do tenant
GRANT ALL ON SCHEMA tenant_demo TO solis_user;
GRANT USAGE ON SCHEMA tenant_demo TO solis_user;

-- Comentário
COMMENT ON SCHEMA tenant_demo IS 'Schema do tenant demo - contém todas as tabelas de dados do tenant';

-- =============================================================================
-- FIM DO SCRIPT 02
-- =============================================================================
