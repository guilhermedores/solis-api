-- =============================================================================
-- SCRIPT 04: POPULAR DADOS INICIAIS DO TENANT DEMO
-- =============================================================================
-- Este script insere dados de exemplo no tenant demo:
-- - Tenant demo na tabela public.tenants
-- - Empresa demo
-- - Usuário admin padrão (senha: admin123)
-- =============================================================================

-- =============================================================================
-- INSERIR TENANT DEMO
-- =============================================================================

INSERT INTO public."tenants" (
    id,
    subdomain,
    company_name,
    cnpj,
    active,
    plan,
    max_terminals,
    max_users,
    features,
    created_at,
    updated_at
)
VALUES 
    (
        '10000000-0000-0000-0000-000000000001'::uuid,
        'demo',
        'Tenant Demo',
        '12345678000190',
        true,
        'premium',
        10,
        50,
        '{"pos": true, "inventory": true, "reports": true, "api_access": true}'::jsonb,
        CURRENT_TIMESTAMP,
        CURRENT_TIMESTAMP
    )
ON CONFLICT (subdomain) DO NOTHING;

-- =============================================================================
-- INSERIR EMPRESA DEMO
-- =============================================================================

-- Inserir empresa demo
INSERT INTO tenant_demo."empresas" (
    id,
    razao_social, 
    nome_fantasia, 
    cnpj, 
    logradouro,
    numero,
    bairro,
    cidade,
    uf,
    cep,
    telefone, 
    regime_tributario,
    ativo,
    created_at,
    updated_at
)
VALUES 
    (
        'a0000000-0000-0000-0000-000000000001'::uuid,
        'Empresa Demo LTDA', 
        'Demo Store', 
        '12345678000190', 
        'Rua Demo',
        '123',
        'Centro',
        'São Paulo',
        'SP',
        '01234-567',
        '(11) 1234-5678',
        'simples_nacional',
        true,
        CURRENT_TIMESTAMP,
        CURRENT_TIMESTAMP
    )
ON CONFLICT (id) DO NOTHING;

-- =============================================================================
-- INSERIR USUÁRIO ADMIN
-- =============================================================================

-- Inserir usuário admin
-- Senha: admin123 (hash bcrypt com salt rounds = 10)
-- Hash gerado com: bcryptjs.hashSync('admin123', 10)
INSERT INTO tenant_demo."usuarios" (
    id,
    nome,
    email,
    password_hash,
    role,
    ativo,
    created_at,
    updated_at
)
VALUES 
    (
        '00000000-0000-0000-0000-000000000001'::uuid,
        'Administrador',
        'admin@admin.com',
        '$2b$10$c8oHqIICMIfqj6kxCBVtZerdfwQVuDhl81fRebNIXLOhbeHaFUlia',
        'admin',
        true,
        CURRENT_TIMESTAMP,
        CURRENT_TIMESTAMP
    )
ON CONFLICT (email) DO NOTHING;

-- =============================================================================
-- FIM DO SCRIPT 04
-- =============================================================================

-- Credenciais padrão:
--   Tenant: demo
--   Email: admin@admin.com
--   Senha: admin123
--
-- IMPORTANTE: Altere a senha após o primeiro login em produção!
