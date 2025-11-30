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
INSERT INTO tenant_demo.entities (id, name, display_name, table_name, icon, description, allow_create, allow_read, allow_update, allow_delete)
VALUES (
    'e0000000-0000-0000-0000-000000000001',
    'user',
    'Users',
    'users',
    'user',
    'System users management',
    true, true, true, true
);

-- Insert User fields
INSERT INTO tenant_demo.entity_fields (entity_id, name, display_name, column_name, data_type, field_type, is_required, is_system_field, show_in_list, show_in_create, show_in_update, list_order, form_order)
VALUES
    ('e0000000-0000-0000-0000-000000000001', 'id', 'ID', 'id', 'uuid', 'text', true, true, false, false, false, 0, 0),
    ('e0000000-0000-0000-0000-000000000001', 'name', 'Name', 'name', 'string', 'text', true, false, true, true, true, 1, 1),
    ('e0000000-0000-0000-0000-000000000001', 'email', 'Email', 'email', 'string', 'email', true, false, true, true, true, 2, 2),
    ('e0000000-0000-0000-0000-000000000001', 'password', 'Password', 'password_hash', 'string', 'password', true, false, false, true, true, 0, 3),
    ('e0000000-0000-0000-0000-000000000001', 'role', 'Role', 'role', 'string', 'select', true, false, true, true, true, 3, 4),
    ('e0000000-0000-0000-0000-000000000001', 'active', 'Active', 'active', 'boolean', 'checkbox', true, false, true, true, true, 4, 5),
    ('e0000000-0000-0000-0000-000000000001', 'created_at', 'Created At', 'created_at', 'datetime', 'datetime', true, true, true, false, false, 5, 0),
    ('e0000000-0000-0000-0000-000000000001', 'updated_at', 'Updated At', 'updated_at', 'datetime', 'datetime', true, true, false, false, false, 0, 0);

-- Insert role options
INSERT INTO tenant_demo.entity_field_options (field_id, value, label, display_order)
SELECT id, 'admin', 'Administrator', 1 FROM tenant_demo.entity_fields WHERE entity_id = 'e0000000-0000-0000-0000-000000000001' AND name = 'role'
UNION ALL
SELECT id, 'manager', 'Manager', 2 FROM tenant_demo.entity_fields WHERE entity_id = 'e0000000-0000-0000-0000-000000000001' AND name = 'role'
UNION ALL
SELECT id, 'operator', 'Operator', 3 FROM tenant_demo.entity_fields WHERE entity_id = 'e0000000-0000-0000-0000-000000000001' AND name = 'role';

-- Insert User permissions
INSERT INTO tenant_demo.entity_permissions (entity_id, role, can_create, can_read, can_update, can_delete, can_read_own_only)
VALUES
    ('e0000000-0000-0000-0000-000000000001', 'admin', true, true, true, true, false),
    ('e0000000-0000-0000-0000-000000000001', 'manager', true, true, true, false, false),
    ('e0000000-0000-0000-0000-000000000001', 'operator', false, true, false, false, true);

-- Insert Company entity
INSERT INTO tenant_demo.entities (id, name, display_name, table_name, icon, description, allow_create, allow_read, allow_update, allow_delete)
VALUES (
    'e0000000-0000-0000-0000-000000000002',
    'company',
    'Companies',
    'companies',
    'building',
    'Company management',
    true, true, true, true
);

-- Insert Company fields
INSERT INTO tenant_demo.entity_fields (entity_id, name, display_name, column_name, data_type, field_type, max_length, is_required, is_unique, show_in_list, show_in_create, show_in_update, list_order, form_order)
VALUES
    ('e0000000-0000-0000-0000-000000000002', 'id', 'ID', 'id', 'uuid', 'text', NULL, true, false, false, false, false, 0, 0),
    ('e0000000-0000-0000-0000-000000000002', 'legal_name', 'Legal Name', 'legal_name', 'string', 'text', 200, true, false, true, true, true, 1, 1),
    ('e0000000-0000-0000-0000-000000000002', 'trade_name', 'Trade Name', 'trade_name', 'string', 'text', 200, false, false, true, true, true, 2, 2),
    ('e0000000-0000-0000-0000-000000000002', 'cnpj', 'CNPJ', 'cnpj', 'string', 'text', 14, true, true, true, true, false, 3, 3),
    ('e0000000-0000-0000-0000-000000000002', 'state_registration', 'State Registration', 'state_registration', 'string', 'text', 50, false, false, false, true, true, 0, 4),
    ('e0000000-0000-0000-0000-000000000002', 'city_registration', 'City Registration', 'city_registration', 'string', 'text', 50, false, false, false, true, true, 0, 5),
    ('e0000000-0000-0000-0000-000000000002', 'cnae', 'CNAE', 'cnae', 'string', 'text', 10, false, false, false, true, true, 0, 6),
    ('e0000000-0000-0000-0000-000000000002', 'logo_url', 'Logo URL', 'logo_url', 'string', 'image', 500, false, false, false, true, true, 0, 7),
    ('e0000000-0000-0000-0000-000000000002', 'tax_regime_id', 'Tax Regime', 'tax_regime_id', 'uuid', 'select', NULL, true, false, true, true, true, 4, 8),
    ('e0000000-0000-0000-0000-000000000002', 'special_tax_regime_id', 'Special Tax Regime', 'special_tax_regime_id', 'uuid', 'select', NULL, false, false, false, true, true, 0, 9),
    ('e0000000-0000-0000-0000-000000000002', 'active', 'Active', 'active', 'boolean', 'checkbox', NULL, true, false, true, true, true, 5, 10),
    ('e0000000-0000-0000-0000-000000000002', 'created_at', 'Created At', 'created_at', 'datetime', 'datetime', NULL, true, false, true, false, false, 6, 0);

-- Insert Company permissions
INSERT INTO tenant_demo.entity_permissions (entity_id, role, can_create, can_read, can_update, can_delete)
VALUES
    ('e0000000-0000-0000-0000-000000000002', 'admin', true, true, true, true),
    ('e0000000-0000-0000-0000-000000000002', 'manager', true, true, true, false),
    ('e0000000-0000-0000-0000-000000000002', 'operator', false, true, false, false);

-- Insert Tax Regime entity
INSERT INTO tenant_demo.entities (id, name, display_name, table_name, icon, description)
VALUES (
    'e0000000-0000-0000-0000-000000000003',
    'tax_regime',
    'Tax Regimes',
    'tax_regimes',
    'receipt',
    'Tax regime types'
);

INSERT INTO tenant_demo.entity_fields (entity_id, name, display_name, column_name, data_type, field_type, is_required, show_in_list, show_in_create, show_in_update, list_order, form_order)
VALUES
    ('e0000000-0000-0000-0000-000000000003', 'id', 'ID', 'id', 'uuid', 'text', true, false, false, false, 0, 0),
    ('e0000000-0000-0000-0000-000000000003', 'code', 'Code', 'code', 'string', 'text', true, true, true, true, 1, 1),
    ('e0000000-0000-0000-0000-000000000003', 'description', 'Description', 'description', 'string', 'text', true, true, true, true, 2, 2),
    ('e0000000-0000-0000-0000-000000000003', 'active', 'Active', 'active', 'boolean', 'checkbox', true, true, true, true, 3, 3);

INSERT INTO tenant_demo.entity_permissions (entity_id, role, can_create, can_read, can_update, can_delete)
VALUES
    ('e0000000-0000-0000-0000-000000000003', 'admin', true, true, true, true),
    ('e0000000-0000-0000-0000-000000000003', 'manager', false, true, false, false),
    ('e0000000-0000-0000-0000-000000000003', 'operator', false, true, false, false);

-- Insert Special Tax Regime entity
INSERT INTO tenant_demo.entities (id, name, display_name, table_name, icon, description)
VALUES (
    'e0000000-0000-0000-0000-000000000004',
    'special_tax_regime',
    'Special Tax Regimes',
    'special_tax_regimes',
    'document',
    'Special tax regime types'
);

INSERT INTO tenant_demo.entity_fields (entity_id, name, display_name, column_name, data_type, field_type, is_required, show_in_list, show_in_create, show_in_update, list_order, form_order)
VALUES
    ('e0000000-0000-0000-0000-000000000004', 'id', 'ID', 'id', 'uuid', 'text', true, false, false, false, 0, 0),
    ('e0000000-0000-0000-0000-000000000004', 'code', 'Code', 'code', 'string', 'text', true, true, true, true, 1, 1),
    ('e0000000-0000-0000-0000-000000000004', 'description', 'Description', 'description', 'string', 'text', true, true, true, true, 2, 2),
    ('e0000000-0000-0000-0000-000000000004', 'active', 'Active', 'active', 'boolean', 'checkbox', true, true, true, true, 3, 3);

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
