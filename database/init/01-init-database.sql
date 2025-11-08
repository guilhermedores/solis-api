-- =============================================================================
-- SCRIPT DE INICIALIZAÇÃO DO BANCO DE DADOS SOLIS PDV
-- ATENÇÃO: Este script cria apenas EXTENSÕES no schema public
-- Tabelas de negócio devem estar nos schemas dos tenants
-- =============================================================================

-- Extensões necessárias
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- =============================================================================
-- FIM DO SCRIPT
-- Tabelas foram removidas deste script e devem estar nos schemas dos tenants
-- =============================================================================
