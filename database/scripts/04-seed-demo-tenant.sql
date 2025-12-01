-- =============================================
-- Script: 04-seed-demo-tenant.sql
-- Description: Creates demo tenant and seeds initial data
-- Author: Solis Team
-- Date: 2025-11-30
-- =============================================

-- Insert demo tenant
INSERT INTO public.tenants (id, subdomain, legal_name, trade_name, cnpj, schema_name, active)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'demo',
    'Demo Company LTDA',
    'Demo Store',
    '12345678000195',
    'tenant_demo',
    true
)
ON CONFLICT (subdomain) DO NOTHING;

-- Create demo tenant schema
SELECT create_tenant_schema('tenant_demo');

-- Seed tax regimes for demo tenant
INSERT INTO tenant_demo.tax_regimes (id, code, description, active)
VALUES 
    ('10000000-0000-0000-0000-000000000001', '1', 'Simples Nacional', true),
    ('10000000-0000-0000-0000-000000000002', '2', 'Simples Nacional - Excesso de Sublimite da Receita Bruta', true),
    ('10000000-0000-0000-0000-000000000003', '3', 'Regime Normal', true)
ON CONFLICT (code) DO NOTHING;

-- Seed special tax regimes for demo tenant
INSERT INTO tenant_demo.special_tax_regimes (id, code, description, active)
VALUES 
    ('20000000-0000-0000-0000-000000000001', '1', 'Microempresa Municipal', true),
    ('20000000-0000-0000-0000-000000000002', '2', 'Estimativa', true),
    ('20000000-0000-0000-0000-000000000003', '3', 'Sociedade de Profissionais', true),
    ('20000000-0000-0000-0000-000000000004', '4', 'Cooperativa', true),
    ('20000000-0000-0000-0000-000000000005', '5', 'MEI - Microempreendedor Individual', true),
    ('20000000-0000-0000-0000-000000000006', '6', 'Microempresa ou Empresa de Pequeno Porte', true)
ON CONFLICT (code) DO NOTHING;

-- Seed demo admin user (password: Admin@123)
-- Hash generated with PostgreSQL crypt('Admin@123', gen_salt('bf', 12))
INSERT INTO tenant_demo.users (id, name, email, password_hash, role, active)
VALUES (
    '30000000-0000-0000-0000-000000000001',
    'Demo Admin',
    'admin@demo.com',
    '$2a$12$PdVQ4h5iRXC..zJpkUlTKeWyVyp/OKPdUn1ZfRV1DL3auTV5yOA0C',
    'admin',
    true)
ON CONFLICT (email) DO NOTHING;

-- Seed demo manager user (password: Manager@123)
INSERT INTO tenant_demo.users (id, name, email, password_hash, role, active)
VALUES (
    '30000000-0000-0000-0000-000000000002',
    'Demo Manager',
    'manager@demo.com',
    '$2a$12$k8Y1THKCI6TBKgWPKWkSieMYjnJVPvlpXU9XFQO0XQv3c1yYWvKqK',
    'manager',
    true
)
ON CONFLICT (email) DO NOTHING;

-- Seed demo operator user (password: Operator@123)
INSERT INTO tenant_demo.users (id, name, email, password_hash, role, active)
VALUES (
    '30000000-0000-0000-0000-000000000003',
    'Demo Operator',
    'operator@demo.com',
    '$2a$12$3/p7WlTQPqKPWl0eS5K5KOhKvLg8EjqGcGq5K5sJKW5KqJ5K5KqJ5',
    'operator',
    true
)
ON CONFLICT (email) DO NOTHING;

-- Seed demo company
INSERT INTO tenant_demo.companies (
    id, legal_name, trade_name, cnpj, state_registration, city_registration,
    cnae, tax_regime_id, special_tax_regime_id, active,
    address_zip_code, address_street, address_number, address_complement,
    address_district, address_city, address_state,
    contact_phone, contact_mobile, contact_email
)
VALUES (
    '40000000-0000-0000-0000-000000000001',
    'Demo Store LTDA',
    'Demo Loja',
    '12345678000195',
    '123456789',
    '987654321',
    '4711-3/01',
    '10000000-0000-0000-0000-000000000001', -- Simples Nacional
    '20000000-0000-0000-0000-000000000006', -- ME/EPP
    true,
    '01310100',
    'Avenida Paulista',
    '1000',
    'Sala 101',
    'Bela Vista',
    'São Paulo',
    'SP',
    '1133334444',
    '11999998888',
    'contato@demoloja.com.br'
)
ON CONFLICT (cnpj) DO NOTHING;

-- Log completion
DO $$
BEGIN
    RAISE NOTICE 'Demo tenant seeded successfully';
    RAISE NOTICE 'Tenant: demo';
    RAISE NOTICE 'Admin: admin@demo.com / Admin@123';
    RAISE NOTICE 'Manager: manager@demo.com / Manager@123';
    RAISE NOTICE 'Operator: operator@demo.com / Operator@123';
END $$;

-- Seed entity metadata for demo tenant
-- Insert User entity
INSERT INTO tenant_demo.entities (id, name, display_name, table_name, category, icon, description, allow_create, allow_read, allow_update, allow_delete)
VALUES (
    'e0000000-0000-0000-0000-000000000001',
    'user',
    'Usuários',
    'users',
    'Configurações',
    'user',
    'Gerenciamento de usuários do sistema',
    true, true, true, true
);

-- Insert User fields
INSERT INTO tenant_demo.entity_fields (entity_id, name, display_name, column_name, data_type, field_type, is_required, is_system_field, show_in_list, show_in_create, show_in_update, list_order, form_order)
VALUES
    ('e0000000-0000-0000-0000-000000000001', 'id', 'ID', 'id', 'uuid', 'text', true, true, false, false, false, 0, 0),
    ('e0000000-0000-0000-0000-000000000001', 'name', 'Nome', 'name', 'string', 'text', true, false, true, true, true, 1, 1),
    ('e0000000-0000-0000-0000-000000000001', 'email', 'E-mail', 'email', 'string', 'email', true, false, true, true, true, 2, 2),
    ('e0000000-0000-0000-0000-000000000001', 'password', 'Senha', 'password_hash', 'string', 'password', true, false, false, true, true, 0, 3),
    ('e0000000-0000-0000-0000-000000000001', 'role', 'Perfil', 'role', 'string', 'select', true, false, true, true, true, 3, 4),
    ('e0000000-0000-0000-0000-000000000001', 'active', 'Ativo', 'active', 'boolean', 'checkbox', true, false, true, true, true, 4, 5),
    ('e0000000-0000-0000-0000-000000000001', 'created_at', 'Criado em', 'created_at', 'datetime', 'datetime', true, true, true, false, false, 5, 0),
    ('e0000000-0000-0000-0000-000000000001', 'updated_at', 'Atualizado em', 'updated_at', 'datetime', 'datetime', true, true, false, false, false, 0, 0);

-- Insert role options
INSERT INTO tenant_demo.entity_field_options (field_id, value, label, display_order)
SELECT id, 'admin', 'Administrador', 1 FROM tenant_demo.entity_fields WHERE entity_id = 'e0000000-0000-0000-0000-000000000001' AND name = 'role'
UNION ALL
SELECT id, 'manager', 'Gerente', 2 FROM tenant_demo.entity_fields WHERE entity_id = 'e0000000-0000-0000-0000-000000000001' AND name = 'role'
UNION ALL
SELECT id, 'operator', 'Operador', 3 FROM tenant_demo.entity_fields WHERE entity_id = 'e0000000-0000-0000-0000-000000000001' AND name = 'role';

-- Insert User permissions
INSERT INTO tenant_demo.entity_permissions (entity_id, role, can_create, can_read, can_update, can_delete, can_read_own_only)
VALUES
    ('e0000000-0000-0000-0000-000000000001', 'admin', true, true, true, true, false),
    ('e0000000-0000-0000-0000-000000000001', 'manager', true, true, true, false, false),
    ('e0000000-0000-0000-0000-000000000001', 'operator', false, true, false, false, true);

-- Insert Company entity
INSERT INTO tenant_demo.entities (id, name, display_name, table_name, category, icon, description, allow_create, allow_read, allow_update, allow_delete)
VALUES (
    'e0000000-0000-0000-0000-000000000002',
    'company',
    'Empresa',
    'companies',
    'Fiscal',
    'building',
    'Gerenciamento de empresas',
    true, true, true, true
);

-- Insert Company fields
INSERT INTO tenant_demo.entity_fields (entity_id, name, display_name, column_name, data_type, field_type, max_length, is_required, is_unique, show_in_list, show_in_create, show_in_update, list_order, form_order)
VALUES
    ('e0000000-0000-0000-0000-000000000002', 'id', 'ID', 'id', 'uuid', 'text', NULL, true, false, false, false, false, 0, 0),
    ('e0000000-0000-0000-0000-000000000002', 'legal_name', 'Razão Social', 'legal_name', 'string', 'text', 200, true, false, true, true, true, 1, 1),
    ('e0000000-0000-0000-0000-000000000002', 'trade_name', 'Nome Fantasia', 'trade_name', 'string', 'text', 200, false, false, true, true, true, 2, 2),
    ('e0000000-0000-0000-0000-000000000002', 'cnpj', 'CNPJ', 'cnpj', 'string', 'text', 14, true, true, true, true, false, 3, 3),
    ('e0000000-0000-0000-0000-000000000002', 'state_registration', 'Inscrição Estadual', 'state_registration', 'string', 'text', 50, false, false, false, true, true, 0, 4),
    ('e0000000-0000-0000-0000-000000000002', 'city_registration', 'Inscrição Municipal', 'city_registration', 'string', 'text', 50, false, false, false, true, true, 0, 5),
    ('e0000000-0000-0000-0000-000000000002', 'cnae', 'CNAE', 'cnae', 'string', 'text', 10, false, false, false, true, true, 0, 6),
    ('e0000000-0000-0000-0000-000000000002', 'logo_url', 'URL do Logo', 'logo_url', 'string', 'image', 500, false, false, false, true, true, 0, 7),
    ('e0000000-0000-0000-0000-000000000002', 'tax_regime_id', 'Regime Tributário', 'tax_regime_id', 'uuid', 'select', NULL, true, false, true, true, true, 4, 8),
    ('e0000000-0000-0000-0000-000000000002', 'special_tax_regime_id', 'Regime Especial de Tributação', 'special_tax_regime_id', 'uuid', 'select', NULL, false, false, false, true, true, 0, 9),
    ('e0000000-0000-0000-0000-000000000002', 'active', 'Ativo', 'active', 'boolean', 'checkbox', NULL, true, false, true, true, true, 5, 10),
    ('e0000000-0000-0000-0000-000000000002', 'created_at', 'Criado em', 'created_at', 'datetime', 'datetime', NULL, true, false, true, false, false, 6, 0);

-- Insert Company permissions
INSERT INTO tenant_demo.entity_permissions (entity_id, role, can_create, can_read, can_update, can_delete)
VALUES
    ('e0000000-0000-0000-0000-000000000002', 'admin', true, true, true, true),
    ('e0000000-0000-0000-0000-000000000002', 'manager', true, true, true, false),
    ('e0000000-0000-0000-0000-000000000002', 'operator', false, true, false, false);

-- Insert Tax Regime entity
INSERT INTO tenant_demo.entities (id, name, display_name, table_name, category, icon, description)
VALUES (
    'e0000000-0000-0000-0000-000000000003',
    'tax_regime',
    'Regimes Tributários',
    'tax_regimes',
    'Fiscal',
    'receipt',
    'Tipos de regime tributário'
);

INSERT INTO tenant_demo.entity_fields (entity_id, name, display_name, column_name, data_type, field_type, is_required, show_in_list, show_in_create, show_in_update, list_order, form_order)
VALUES
    ('e0000000-0000-0000-0000-000000000003', 'id', 'ID', 'id', 'uuid', 'text', true, false, false, false, 0, 0),
    ('e0000000-0000-0000-0000-000000000003', 'code', 'Código', 'code', 'string', 'text', true, true, true, true, 1, 1),
    ('e0000000-0000-0000-0000-000000000003', 'description', 'Descrição', 'description', 'string', 'text', true, true, true, true, 2, 2),
    ('e0000000-0000-0000-0000-000000000003', 'active', 'Ativo', 'active', 'boolean', 'checkbox', true, true, true, true, 3, 3);

INSERT INTO tenant_demo.entity_permissions (entity_id, role, can_create, can_read, can_update, can_delete)
VALUES
    ('e0000000-0000-0000-0000-000000000003', 'admin', true, true, true, true),
    ('e0000000-0000-0000-0000-000000000003', 'manager', false, true, false, false),
    ('e0000000-0000-0000-0000-000000000003', 'operator', false, true, false, false);

-- Insert Special Tax Regime entity
INSERT INTO tenant_demo.entities (id, name, display_name, table_name, category, icon, description)
VALUES (
    'e0000000-0000-0000-0000-000000000004',
    'special_tax_regime',
    'Regimes Especiais de Tributação',
    'special_tax_regimes',
    'Fiscal',
    'document',
    'Tipos de regime especial de tributação'
);

INSERT INTO tenant_demo.entity_fields (entity_id, name, display_name, column_name, data_type, field_type, is_required, show_in_list, show_in_create, show_in_update, list_order, form_order)
VALUES
    ('e0000000-0000-0000-0000-000000000004', 'id', 'ID', 'id', 'uuid', 'text', true, false, false, false, 0, 0),
    ('e0000000-0000-0000-0000-000000000004', 'code', 'Código', 'code', 'string', 'text', true, true, true, true, 1, 1),
    ('e0000000-0000-0000-0000-000000000004', 'description', 'Descrição', 'description', 'string', 'text', true, true, true, true, 2, 2),
    ('e0000000-0000-0000-0000-000000000004', 'active', 'Ativo', 'active', 'boolean', 'checkbox', true, true, true, true, 3, 3);

INSERT INTO tenant_demo.entity_permissions (entity_id, role, can_create, can_read, can_update, can_delete)
VALUES
    ('e0000000-0000-0000-0000-000000000004', 'admin', true, true, true, true),
    ('e0000000-0000-0000-0000-000000000004', 'manager', false, true, false, false),
    ('e0000000-0000-0000-0000-000000000004', 'operator', false, true, false, false);

-- Insert relationships
-- Company -> TaxRegime (many-to-one)
INSERT INTO tenant_demo.entity_relationships (entity_id, field_id, related_entity_id, relationship_type, foreign_key_column, display_field)
SELECT 
    'e0000000-0000-0000-0000-000000000002',
    ef.id,
    'e0000000-0000-0000-0000-000000000003',
    'many-to-one',
    'tax_regime_id',
    'description'
FROM tenant_demo.entity_fields ef
WHERE ef.entity_id = 'e0000000-0000-0000-0000-000000000002' AND ef.name = 'tax_regime_id';

-- Company -> SpecialTaxRegime (many-to-one)
INSERT INTO tenant_demo.entity_relationships (entity_id, field_id, related_entity_id, relationship_type, foreign_key_column, display_field)
SELECT 
    'e0000000-0000-0000-0000-000000000002',
    ef.id,
    'e0000000-0000-0000-0000-000000000004',
    'many-to-one',
    'special_tax_regime_id',
    'description'
FROM tenant_demo.entity_fields ef
WHERE ef.entity_id = 'e0000000-0000-0000-0000-000000000002' AND ef.name = 'special_tax_regime_id';

-- Log metadata seeding completion
DO $$
BEGIN
    RAISE NOTICE 'Entity metadata seeded successfully';
    RAISE NOTICE 'Entities: user, company, tax_regime, special_tax_regime';
END $$;

-- =====================================================
-- PRODUCTS MODULE - SEED DATA
-- =====================================================

-- Seed default product group (minimal - customer will add their own)
INSERT INTO tenant_demo.product_groups (id, code, name) VALUES
('10000000-0000-0000-0000-000000000001', '1', 'GERAL')
ON CONFLICT (id) DO NOTHING;

-- Seed default product subgroup (minimal - customer will add their own)
INSERT INTO tenant_demo.product_subgroups (id, code, name, product_group_id) VALUES
('20000000-0000-0000-0000-000000000001', '1', 'GERAL', '10000000-0000-0000-0000-000000000001')
ON CONFLICT (id) DO NOTHING;

-- Seed default brand (minimal - customer will add their own)
INSERT INTO tenant_demo.brands (id, name) VALUES
('30000000-0000-0000-0000-000000000001', 'SEM MARCA')
ON CONFLICT (id) DO NOTHING;

-- Register entities in metadata system
INSERT INTO tenant_demo.entities (id, name, display_name, table_name, category, icon, description, allow_create, allow_read, allow_update, allow_delete)
VALUES 
    ('e0000000-0000-0000-0000-000000000005', 'product', 'Produtos', 'products', 'Produtos', 'Package', 'Cadastro de produtos para venda', true, true, true, true),
    ('e0000000-0000-0000-0000-000000000006', 'product_group', 'Grupos de Produto', 'product_groups', 'Produtos', 'FolderTree', 'Categorias principais de produtos', true, true, true, true),
    ('e0000000-0000-0000-0000-000000000007', 'product_subgroup', 'Sub-Grupos de Produto', 'product_subgroups', 'Produtos', 'Folder', 'Subcategorias de produtos', true, true, true, true),
    ('e0000000-0000-0000-0000-000000000008', 'brand', 'Marcas', 'brands', 'Produtos', 'Tag', 'Fabricantes e marcas de produtos', true, true, true, true)
ON CONFLICT (id) DO NOTHING;

-- Register product entity fields
INSERT INTO tenant_demo.entity_fields (id, entity_id, name, display_name, column_name, data_type, is_required, is_readonly, is_system_field, show_in_list, show_in_detail, show_in_create, show_in_update, list_order, form_order, field_type, placeholder, help_text, validation_regex, default_value) VALUES
('f0500000-0000-0000-0000-000000000001', 'e0000000-0000-0000-0000-000000000005', 'id', 'ID', 'id', 'uuid', true, true, true, false, true, false, false, 0, 0, 'text', NULL, NULL, NULL, NULL),
('f0500000-0000-0000-0000-000000000002', 'e0000000-0000-0000-0000-000000000005', 'internal_code', 'Código Interno', 'internal_code', 'string', true, false, false, true, true, true, true, 2, 1, 'text', '12345', 'Identificação no PWA', NULL, NULL),
('f0500000-0000-0000-0000-000000000003', 'e0000000-0000-0000-0000-000000000005', 'barcode', 'Código de Barras / GTIN', 'barcode', 'string', false, false, false, false, true, true, true, 0, 2, 'text', '7891234567890', 'Fundamental para o leitor de código de barras', NULL, NULL),
('f0500000-0000-0000-0000-000000000004', 'e0000000-0000-0000-0000-000000000005', 'description', 'Descrição do Produto', 'description', 'text', true, false, false, true, true, true, true, 3, 3, 'textarea', 'Refrigerante Cola 2L', 'Nome que sai na nota fiscal (NFCe)', NULL, NULL),
('f0500000-0000-0000-0000-000000000005', 'e0000000-0000-0000-0000-000000000005', 'active', 'Produto Ativo?', 'active', 'boolean', true, false, false, true, true, true, true, 4, 4, 'checkbox', NULL, 'Permite inativar produtos sem apagar o histórico de vendas', NULL, 'true'),
('f0500000-0000-0000-0000-000000000006', 'e0000000-0000-0000-0000-000000000005', 'product_group_id', 'Grupo de Produto', 'product_group_id', 'uuid', false, false, false, true, true, true, true, 5, 5, 'select', NULL, 'Ajuda em relatórios e filtros de produtos', NULL, NULL),
('f0500000-0000-0000-0000-000000000007', 'e0000000-0000-0000-0000-000000000005', 'product_subgroup_id', 'Sub-Grupo de Produto', 'product_subgroup_id', 'uuid', false, false, false, false, true, true, true, 0, 6, 'select', NULL, 'Nível de detalhe adicional na organização', NULL, NULL),
('f0500000-0000-0000-0000-000000000008', 'e0000000-0000-0000-0000-000000000005', 'brand_id', 'Fabricante / Marca', 'brand_id', 'uuid', false, false, false, false, true, true, true, 0, 7, 'select', NULL, 'Informação relevante para compras e relatórios de marca', NULL, NULL),
('f0500000-0000-0000-0000-000000000009', 'e0000000-0000-0000-0000-000000000005', 'own_production', 'Produção Própria?', 'own_production', 'boolean', true, false, false, false, true, true, true, 0, 8, 'checkbox', NULL, 'Define se é revenda ou fabricação', NULL, 'false'),
('f0500000-0000-0000-0000-000000000010', 'e0000000-0000-0000-0000-000000000005', 'unit_of_measure', 'Unidade de Medida', 'unit_of_measure', 'string', true, false, false, false, true, true, true, 0, 9, 'select', NULL, 'Padrão: UN, KG, LT', NULL, 'UN'),
('f0500000-0000-0000-0000-000000000011', 'e0000000-0000-0000-0000-000000000005', 'ncm', 'NCM', 'ncm', 'string', true, false, false, false, true, true, true, 0, 10, 'text', '22021000', 'Código de 8 dígitos', '^[0-9]{8}$', NULL),
('f0500000-0000-0000-0000-000000000012', 'e0000000-0000-0000-0000-000000000005', 'cest', 'CEST', 'cest', 'string', false, false, false, false, true, true, true, 0, 11, 'text', '3007000', 'Código de 7 dígitos', '^[0-9]{7}$', NULL),
('f0500000-0000-0000-0000-000000000013', 'e0000000-0000-0000-0000-000000000005', 'product_origin', 'Código Origem do Produto', 'product_origin', 'number', true, false, false, false, true, true, true, 0, 12, 'select', NULL, 'Define a origem da mercadoria', NULL, '0'),
('f0500000-0000-0000-0000-000000000014', 'e0000000-0000-0000-0000-000000000005', 'item_type', 'Tipo de Item', 'item_type', 'number', true, false, false, false, true, true, true, 0, 13, 'select', NULL, 'Mercadoria para Revenda, etc.', NULL, '0'),
('f0500000-0000-0000-0000-000000000015', 'e0000000-0000-0000-0000-000000000005', 'incide_pis_cofins', 'Incide PIS/COFINS?', 'incide_pis_cofins', 'boolean', true, false, false, false, true, true, true, 0, 14, 'checkbox', NULL, 'Flag para regra tributária', NULL, 'true'),
('f0500000-0000-0000-0000-000000000016', 'e0000000-0000-0000-0000-000000000005', 'sale_price', 'Preço de Venda', 'sale_price', 'number', true, false, false, true, true, true, true, 6, 15, 'number', '10.00', 'Valor no PDV', NULL, NULL),
('f0500000-0000-0000-0000-000000000017', 'e0000000-0000-0000-0000-000000000005', 'cost_price', 'Preço de Custo', 'cost_price', 'number', false, false, false, false, true, true, true, 0, 16, 'number', '4.50', 'Para cálculo de margem', NULL, NULL),
('f0500000-0000-0000-0000-000000000018', 'e0000000-0000-0000-0000-000000000005', 'purchase_price', 'Valor de Compra', 'purchase_price', 'number', false, false, false, false, true, true, true, 0, 17, 'number', '4.00', 'Último preço pago', NULL, NULL),
('f0500000-0000-0000-0000-000000000019', 'e0000000-0000-0000-0000-000000000005', 'created_at', 'Criado em', 'created_at', 'datetime', true, true, true, false, true, false, false, 0, 0, 'datetime', NULL, NULL, NULL, NULL)
ON CONFLICT (id) DO NOTHING;

-- Register product_group entity fields
INSERT INTO tenant_demo.entity_fields (id, entity_id, name, display_name, column_name, data_type, is_required, is_readonly, is_system_field, show_in_list, show_in_detail, show_in_create, show_in_update, list_order, form_order, field_type, placeholder, help_text, validation_regex, default_value) VALUES
('f0600000-0000-0000-0000-000000000001', 'e0000000-0000-0000-0000-000000000006', 'id', 'ID', 'id', 'uuid', true, true, true, false, true, false, false, 0, 0, 'text', NULL, NULL, NULL, NULL),
('f0600000-0000-0000-0000-000000000002', 'e0000000-0000-0000-0000-000000000006', 'code', 'Código', 'code', 'string', true, false, false, true, true, true, true, 2, 1, 'text', 'ALIM', NULL, NULL, NULL),
('f0600000-0000-0000-0000-000000000003', 'e0000000-0000-0000-0000-000000000006', 'name', 'Nome', 'name', 'string', true, false, false, true, true, true, true, 3, 2, 'text', 'Alimentos', NULL, NULL, NULL),
('f0600000-0000-0000-0000-000000000004', 'e0000000-0000-0000-0000-000000000006', 'active', 'Ativo', 'active', 'boolean', true, false, false, true, true, true, true, 4, 3, 'checkbox', NULL, NULL, NULL, 'true')
ON CONFLICT (id) DO NOTHING;

-- Register product_subgroup entity fields
INSERT INTO tenant_demo.entity_fields (id, entity_id, name, display_name, column_name, data_type, is_required, is_readonly, is_system_field, show_in_list, show_in_detail, show_in_create, show_in_update, list_order, form_order, field_type, placeholder, help_text, validation_regex, default_value) VALUES
('f0700000-0000-0000-0000-000000000001', 'e0000000-0000-0000-0000-000000000007', 'id', 'ID', 'id', 'uuid', true, true, true, false, true, false, false, 0, 0, 'text', NULL, NULL, NULL, NULL),
('f0700000-0000-0000-0000-000000000002', 'e0000000-0000-0000-0000-000000000007', 'code', 'Código', 'code', 'string', true, false, false, true, true, true, true, 2, 1, 'text', 'LATICINIOS', NULL, NULL, NULL),
('f0700000-0000-0000-0000-000000000003', 'e0000000-0000-0000-0000-000000000007', 'name', 'Nome', 'name', 'string', true, false, false, true, true, true, true, 3, 2, 'text', 'Laticínios', NULL, NULL, NULL),
('f0700000-0000-0000-0000-000000000004', 'e0000000-0000-0000-0000-000000000007', 'product_group_id', 'Grupo de Produto', 'product_group_id', 'uuid', false, false, false, true, true, true, true, 4, 3, 'select', NULL, NULL, NULL, NULL),
('f0700000-0000-0000-0000-000000000005', 'e0000000-0000-0000-0000-000000000007', 'active', 'Ativo', 'active', 'boolean', true, false, false, true, true, true, true, 5, 4, 'checkbox', NULL, NULL, NULL, 'true')
ON CONFLICT (id) DO NOTHING;

-- Register brand entity fields
INSERT INTO tenant_demo.entity_fields (id, entity_id, name, display_name, column_name, data_type, is_required, is_readonly, is_system_field, show_in_list, show_in_detail, show_in_create, show_in_update, list_order, form_order, field_type, placeholder, help_text, validation_regex, default_value) VALUES
('f0800000-0000-0000-0000-000000000001', 'e0000000-0000-0000-0000-000000000008', 'id', 'ID', 'id', 'uuid', true, true, true, false, true, false, false, 0, 0, 'text', NULL, NULL, NULL, NULL),
('f0800000-0000-0000-0000-000000000002', 'e0000000-0000-0000-0000-000000000008', 'name', 'Nome', 'name', 'string', true, false, false, true, true, true, true, 2, 1, 'text', 'Nestlé', NULL, NULL, NULL),
('f0800000-0000-0000-0000-000000000003', 'e0000000-0000-0000-0000-000000000008', 'active', 'Ativo', 'active', 'boolean', true, false, false, true, true, true, true, 3, 2, 'checkbox', NULL, NULL, NULL, 'true')
ON CONFLICT (id) DO NOTHING;

-- Register relationships
INSERT INTO tenant_demo.entity_relationships (id, entity_id, field_id, related_entity_id, relationship_type, foreign_key_column, display_field) VALUES
('50000000-0000-0000-0000-000000000001', 'e0000000-0000-0000-0000-000000000005', 'f0500000-0000-0000-0000-000000000006', 'e0000000-0000-0000-0000-000000000006', 'many-to-one', 'product_group_id', 'name'),
('50000000-0000-0000-0000-000000000002', 'e0000000-0000-0000-0000-000000000005', 'f0500000-0000-0000-0000-000000000007', 'e0000000-0000-0000-0000-000000000007', 'many-to-one', 'product_subgroup_id', 'name'),
('50000000-0000-0000-0000-000000000003', 'e0000000-0000-0000-0000-000000000005', 'f0500000-0000-0000-0000-000000000008', 'e0000000-0000-0000-0000-000000000008', 'many-to-one', 'brand_id', 'name'),
('70000000-0000-0000-0000-000000000001', 'e0000000-0000-0000-0000-000000000007', 'f0700000-0000-0000-0000-000000000004', 'e0000000-0000-0000-0000-000000000006', 'many-to-one', 'product_group_id', 'name')
ON CONFLICT (id) DO NOTHING;

-- Register field options for static selects
INSERT INTO tenant_demo.entity_field_options (field_id, value, label, display_order) 
SELECT 'f0500000-0000-0000-0000-000000000010'::uuid, 'UN', 'UN', 1 WHERE NOT EXISTS (SELECT 1 FROM tenant_demo.entity_field_options WHERE field_id = 'f0500000-0000-0000-0000-000000000010')
UNION ALL SELECT 'f0500000-0000-0000-0000-000000000010'::uuid, 'KG', 'KG', 2 WHERE NOT EXISTS (SELECT 1 FROM tenant_demo.entity_field_options WHERE field_id = 'f0500000-0000-0000-0000-000000000010' AND value = 'KG')
UNION ALL SELECT 'f0500000-0000-0000-0000-000000000010'::uuid, 'LT', 'LT', 3 WHERE NOT EXISTS (SELECT 1 FROM tenant_demo.entity_field_options WHERE field_id = 'f0500000-0000-0000-0000-000000000010' AND value = 'LT')
UNION ALL SELECT 'f0500000-0000-0000-0000-000000000010'::uuid, 'MT', 'MT', 4 WHERE NOT EXISTS (SELECT 1 FROM tenant_demo.entity_field_options WHERE field_id = 'f0500000-0000-0000-0000-000000000010' AND value = 'MT')
UNION ALL SELECT 'f0500000-0000-0000-0000-000000000010'::uuid, 'CX', 'CX', 5 WHERE NOT EXISTS (SELECT 1 FROM tenant_demo.entity_field_options WHERE field_id = 'f0500000-0000-0000-0000-000000000010' AND value = 'CX')
UNION ALL SELECT 'f0500000-0000-0000-0000-000000000010'::uuid, 'PC', 'PC', 6 WHERE NOT EXISTS (SELECT 1 FROM tenant_demo.entity_field_options WHERE field_id = 'f0500000-0000-0000-0000-000000000010' AND value = 'PC');

-- Product origin options
INSERT INTO tenant_demo.entity_field_options (field_id, value, label, display_order)
SELECT 'f0500000-0000-0000-0000-000000000013'::uuid, '0', '0 - Nacional', 1 WHERE NOT EXISTS (SELECT 1 FROM tenant_demo.entity_field_options WHERE field_id = 'f0500000-0000-0000-0000-000000000013' AND value = '0')
UNION ALL SELECT 'f0500000-0000-0000-0000-000000000013'::uuid, '1', '1 - Estrangeira (Importação Direta)', 2 WHERE NOT EXISTS (SELECT 1 FROM tenant_demo.entity_field_options WHERE field_id = 'f0500000-0000-0000-0000-000000000013' AND value = '1')
UNION ALL SELECT 'f0500000-0000-0000-0000-000000000013'::uuid, '2', '2 - Estrangeira (Adquirida no Mercado Interno)', 3 WHERE NOT EXISTS (SELECT 1 FROM tenant_demo.entity_field_options WHERE field_id = 'f0500000-0000-0000-0000-000000000013' AND value = '2')
UNION ALL SELECT 'f0500000-0000-0000-0000-000000000013'::uuid, '3', '3 - Nacional (Conteúdo de Importação Superior a 40%)', 4 WHERE NOT EXISTS (SELECT 1 FROM tenant_demo.entity_field_options WHERE field_id = 'f0500000-0000-0000-0000-000000000013' AND value = '3');

-- Item type options
INSERT INTO tenant_demo.entity_field_options (field_id, value, label, display_order)
SELECT 'f0500000-0000-0000-0000-000000000014'::uuid, '0', '00 - Mercadoria para Revenda', 1 WHERE NOT EXISTS (SELECT 1 FROM tenant_demo.entity_field_options WHERE field_id = 'f0500000-0000-0000-0000-000000000014' AND value = '0')
UNION ALL SELECT 'f0500000-0000-0000-0000-000000000014'::uuid, '1', '01 - Matéria-Prima', 2 WHERE NOT EXISTS (SELECT 1 FROM tenant_demo.entity_field_options WHERE field_id = 'f0500000-0000-0000-0000-000000000014' AND value = '1')
UNION ALL SELECT 'f0500000-0000-0000-0000-000000000014'::uuid, '7', '07 - Material de Uso e Consumo', 3 WHERE NOT EXISTS (SELECT 1 FROM tenant_demo.entity_field_options WHERE field_id = 'f0500000-0000-0000-0000-000000000014' AND value = '7')
UNION ALL SELECT 'f0500000-0000-0000-0000-000000000014'::uuid, '9', '09 - Serviços', 4 WHERE NOT EXISTS (SELECT 1 FROM tenant_demo.entity_field_options WHERE field_id = 'f0500000-0000-0000-0000-000000000014' AND value = '9');

-- Register permissions
INSERT INTO tenant_demo.entity_permissions (entity_id, role, can_create, can_read, can_update, can_delete) VALUES
('e0000000-0000-0000-0000-000000000005', 'admin', true, true, true, true),
('e0000000-0000-0000-0000-000000000005', 'manager', true, true, true, false),
('e0000000-0000-0000-0000-000000000005', 'operator', false, true, false, false),
('e0000000-0000-0000-0000-000000000006', 'admin', true, true, true, true),
('e0000000-0000-0000-0000-000000000006', 'manager', true, true, true, false),
('e0000000-0000-0000-0000-000000000006', 'operator', false, true, false, false),
('e0000000-0000-0000-0000-000000000007', 'admin', true, true, true, true),
('e0000000-0000-0000-0000-000000000007', 'manager', true, true, true, false),
('e0000000-0000-0000-0000-000000000007', 'operator', false, true, false, false),
('e0000000-0000-0000-0000-000000000008', 'admin', true, true, true, true),
('e0000000-0000-0000-0000-000000000008', 'manager', true, true, true, false),
('e0000000-0000-0000-0000-000000000008', 'operator', false, true, false, false)
ON CONFLICT (entity_id, role) DO NOTHING;

-- Log products module completion
DO $$
BEGIN
    RAISE NOTICE '📦 Products module seeded successfully';
    RAISE NOTICE 'Default values: 1 GERAL group, 1 GERAL subgroup, 1 SEM MARCA brand';
    RAISE NOTICE 'Entities: product, product_group, product_subgroup, brand';
    RAISE NOTICE 'Customers can add their own categories using the dynamic CRUD endpoints';
END $$;

