-- =============================================
-- Script: 02-create-tenants-table.sql
-- Description: Creates the tenants management table in public schema
-- Author: Solis Team
-- Date: 2025-11-30
-- =============================================

-- Create tenants table in public schema
CREATE TABLE IF NOT EXISTS public.tenants (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    subdomain VARCHAR(50) NOT NULL UNIQUE,
    legal_name VARCHAR(200) NOT NULL,
    trade_name VARCHAR(200),
    cnpj VARCHAR(14) UNIQUE,
    schema_name VARCHAR(63) NOT NULL UNIQUE,
    active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT chk_subdomain_format CHECK (subdomain ~ '^[a-z0-9][a-z0-9-]*[a-z0-9]$'),
    CONSTRAINT chk_schema_name_format CHECK (schema_name ~ '^tenant_[a-z0-9_]+$'),
    CONSTRAINT chk_cnpj_length CHECK (cnpj IS NULL OR LENGTH(cnpj) = 14)
);

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_tenants_subdomain ON public.tenants(subdomain) WHERE active = true;
CREATE INDEX IF NOT EXISTS idx_tenants_cnpj ON public.tenants(cnpj) WHERE cnpj IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_tenants_active ON public.tenants(active);

-- Create updated_at trigger function
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create trigger for tenants table
DROP TRIGGER IF EXISTS trg_tenants_updated_at ON public.tenants;
CREATE TRIGGER trg_tenants_updated_at
    BEFORE UPDATE ON public.tenants
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Add comments for documentation
COMMENT ON TABLE public.tenants IS 'Multi-tenant management table - stores tenant metadata';
COMMENT ON COLUMN public.tenants.subdomain IS 'Unique subdomain identifier for tenant (e.g., demo, acme)';
COMMENT ON COLUMN public.tenants.schema_name IS 'PostgreSQL schema name for tenant data isolation';
COMMENT ON COLUMN public.tenants.legal_name IS 'Legal company name (Razão Social)';
COMMENT ON COLUMN public.tenants.trade_name IS 'Trade name (Nome Fantasia)';
COMMENT ON COLUMN public.tenants.cnpj IS 'Brazilian company registration number (14 digits)';
