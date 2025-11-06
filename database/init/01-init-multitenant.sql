-- Script de inicialização do banco de dados Solis
-- Multi-Tenant Architecture

-- =============================================================================
-- SCHEMA PUBLIC (DEFAULT)
-- =============================================================================

-- Tabela de Tenants (clientes/empresas)
CREATE TABLE IF NOT EXISTS tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
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
CREATE INDEX idx_tenants_subdomain ON tenants(subdomain) WHERE active = true;
CREATE INDEX idx_tenants_active ON tenants(active);

-- Inserir tenant de exemplo para testes
INSERT INTO tenants (subdomain, company_name, cnpj, active, plan, max_terminals) VALUES
    ('demo', 'Empresa Demo Ltda', '00.000.000/0001-00', true, 'premium', 5),
    ('cliente1', 'Cliente 1 Comércio', '11.111.111/0001-11', true, 'basic', 2),
    ('cliente2', 'Cliente 2 Supermercado', '22.222.222/0001-22', true, 'professional', 3)
ON CONFLICT (subdomain) DO NOTHING;

-- =============================================================================
-- FUNÇÃO PARA CRIAR SCHEMA DE TENANT
-- =============================================================================

CREATE OR REPLACE FUNCTION create_tenant_schema(tenant_name VARCHAR)
RETURNS VOID AS $$
DECLARE
    schema_name VARCHAR := 'tenant_' || tenant_name;
BEGIN
    -- Criar schema se não existir
    EXECUTE format('CREATE SCHEMA IF NOT EXISTS %I', schema_name);
    
    -- Definir search_path para o novo schema
    EXECUTE format('SET search_path TO %I, public', schema_name);
    
    -- =============================================================================
    -- TABELAS DO TENANT
    -- =============================================================================
    
    -- Tabela de Usuários
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.users (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            name VARCHAR(255) NOT NULL,
            email VARCHAR(255) UNIQUE NOT NULL,
            password_hash VARCHAR(255) NOT NULL,
            role VARCHAR(50) DEFAULT ''operator'',
            active BOOLEAN DEFAULT true,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )', schema_name);
    
    -- Tabela de Produtos
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.produtos (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            codigo_barras VARCHAR(50),
            codigo_interno VARCHAR(50),
            nome VARCHAR(255) NOT NULL,
            descricao TEXT,
            ncm VARCHAR(20),
            cest VARCHAR(20),
            unidade_medida VARCHAR(10) NOT NULL DEFAULT ''UN'',
            ativo BOOLEAN DEFAULT true,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            synced_at TIMESTAMP
        )', schema_name);
    
    -- Tabela de Preços dos Produtos
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.produto_precos (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            produto_id UUID NOT NULL REFERENCES %I.produtos(id) ON DELETE CASCADE,
            preco_venda DECIMAL(10,2) NOT NULL,
            preco_custo DECIMAL(10,2),
            ativo BOOLEAN DEFAULT true,
            vigencia_inicio TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            vigencia_fim TIMESTAMP,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )', schema_name, schema_name);
    
    -- Tabela de Formas de Pagamento
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.formas_pagamento (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            codigo VARCHAR(50) UNIQUE NOT NULL,
            descricao VARCHAR(255) NOT NULL,
            tipo VARCHAR(50) NOT NULL,
            ativa BOOLEAN DEFAULT true,
            ordem INTEGER DEFAULT 0,
            maximo_parcelas INTEGER DEFAULT 1,
            taxa_juros DECIMAL(5,2) DEFAULT 0,
            permite_troco BOOLEAN DEFAULT false,
            requer_tef BOOLEAN DEFAULT false,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )', schema_name);
    
    -- Tabela de Vendas
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.vendas (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            numero_cupom INTEGER NOT NULL,
            user_id UUID REFERENCES %I.users(id),
            cliente_cpf VARCHAR(14),
            cliente_nome VARCHAR(255),
            valor_bruto DECIMAL(10,2) NOT NULL,
            valor_desconto DECIMAL(10,2) DEFAULT 0,
            valor_liquido DECIMAL(10,2) NOT NULL,
            status VARCHAR(50) DEFAULT ''ABERTA'',
            observacoes TEXT,
            sincronizado BOOLEAN DEFAULT false,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            synced_at TIMESTAMP
        )', schema_name, schema_name);
    
    -- Tabela de Itens da Venda
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.venda_itens (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            venda_id UUID NOT NULL REFERENCES %I.vendas(id) ON DELETE CASCADE,
            produto_id UUID REFERENCES %I.produtos(id),
            sequencia INTEGER NOT NULL,
            codigo_produto VARCHAR(50) NOT NULL,
            nome_produto VARCHAR(255) NOT NULL,
            quantidade DECIMAL(10,3) NOT NULL,
            preco_unitario DECIMAL(10,2) NOT NULL,
            desconto_item DECIMAL(10,2) DEFAULT 0,
            valor_total DECIMAL(10,2) NOT NULL,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )', schema_name, schema_name, schema_name);
    
    -- Tabela de Pagamentos da Venda
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.venda_pagamentos (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            venda_id UUID NOT NULL REFERENCES %I.vendas(id) ON DELETE CASCADE,
            forma_pagamento_id UUID REFERENCES %I.formas_pagamento(id),
            valor DECIMAL(10,2) NOT NULL,
            valor_troco DECIMAL(10,2) DEFAULT 0,
            parcelas INTEGER DEFAULT 1,
            nsu VARCHAR(100),
            autorizacao VARCHAR(100),
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )', schema_name, schema_name, schema_name);
    
    -- =============================================================================
    -- ÍNDICES
    -- =============================================================================
    
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_produtos_codigo_barras ON %I.produtos(codigo_barras)', tenant_name, schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_produtos_ativo ON %I.produtos(ativo)', tenant_name, schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_vendas_status ON %I.vendas(status)', tenant_name, schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_vendas_created ON %I.vendas(created_at)', tenant_name, schema_name);
    
    RAISE NOTICE 'Schema % criado com sucesso!', schema_name;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- CRIAR SCHEMAS PARA OS TENANTS DE EXEMPLO
-- =============================================================================

SELECT create_tenant_schema('demo');
SELECT create_tenant_schema('cliente1');
SELECT create_tenant_schema('cliente2');

-- =============================================================================
-- INSERIR DADOS DE EXEMPLO NO TENANT DEMO
-- =============================================================================

SET search_path TO tenant_demo, public;

-- Formas de Pagamento
INSERT INTO formas_pagamento (codigo, descricao, tipo, ativa, ordem, permite_troco) VALUES
    ('DINHEIRO', 'Dinheiro', 'DINHEIRO', true, 1, true),
    ('DEBITO', 'Cartão de Débito', 'DEBITO', true, 2, false),
    ('CREDITO', 'Cartão de Crédito', 'CREDITO', true, 3, false),
    ('PIX', 'PIX', 'PAGAMENTO_INSTANTANEO', true, 4, false)
ON CONFLICT (codigo) DO NOTHING;

-- Produtos de exemplo
INSERT INTO produtos (codigo_barras, codigo_interno, nome, descricao, unidade_medida, ativo) VALUES
    ('7891000100103', 'PROD001', 'COCA-COLA 2L', 'Refrigerante Coca-Cola 2 Litros', 'UN', true),
    ('7891000100202', 'PROD002', 'GUARANÁ 2L', 'Refrigerante Guaraná Antarctica 2 Litros', 'UN', true),
    ('7891234567890', 'PROD003', 'PÃO FRANCÊS', 'Pão Francês Tradicional', 'KG', true)
ON CONFLICT DO NOTHING;

-- Preços dos produtos
INSERT INTO produto_precos (produto_id, preco_venda, preco_custo, ativo) 
SELECT id, 8.90, 5.50, true FROM produtos WHERE codigo_interno = 'PROD001'
UNION ALL
SELECT id, 7.50, 4.80, true FROM produtos WHERE codigo_interno = 'PROD002'
UNION ALL
SELECT id, 15.00, 10.00, true FROM produtos WHERE codigo_interno = 'PROD003'
ON CONFLICT DO NOTHING;

-- Reset search_path
SET search_path TO public;

-- Mensagem final
DO $$
BEGIN
    RAISE NOTICE '=============================================================================';
    RAISE NOTICE 'Banco de dados Solis inicializado com sucesso!';
    RAISE NOTICE '=============================================================================';
    RAISE NOTICE 'Tenants criados:';
    RAISE NOTICE '  - demo (tenant_demo) - Com dados de exemplo';
    RAISE NOTICE '  - cliente1 (tenant_cliente1)';
    RAISE NOTICE '  - cliente2 (tenant_cliente2)';
    RAISE NOTICE '=============================================================================';
END $$;
