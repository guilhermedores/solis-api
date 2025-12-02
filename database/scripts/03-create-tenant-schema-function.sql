-- =============================================
-- Script: 03-create-tenant-schema-function.sql
-- Description: Creates function to provision new tenant schemas
-- Author: Solis Team
-- Date: 2025-11-30
-- =============================================

-- Function to create a new tenant schema with all required tables
CREATE OR REPLACE FUNCTION create_tenant_schema(p_schema_name VARCHAR)
RETURNS VOID AS $$
BEGIN
    -- Create schema
    EXECUTE format('CREATE SCHEMA IF NOT EXISTS %I', p_schema_name);
    
    -- Grant privileges
    EXECUTE format('GRANT ALL ON SCHEMA %I TO solis_user', p_schema_name);
    
    -- Create users table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.users (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            name VARCHAR(200) NOT NULL,
            email VARCHAR(200) NOT NULL UNIQUE,
            password_hash VARCHAR(500) NOT NULL,
            role VARCHAR(50) NOT NULL DEFAULT ''operator'',
            active BOOLEAN NOT NULL DEFAULT true,
            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            
            CONSTRAINT chk_users_role CHECK (role IN (''admin'', ''manager'', ''operator''))
        )', p_schema_name);
    
    -- Create indexes for users table
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_users_email ON %I.users(email) WHERE active = true', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_users_role ON %I.users(role)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_users_active ON %I.users(active)', p_schema_name);
    
    -- Create trigger for users updated_at
    EXECUTE format('
        DROP TRIGGER IF EXISTS trg_users_updated_at ON %I.users;
        CREATE TRIGGER trg_users_updated_at
            BEFORE UPDATE ON %I.users
            FOR EACH ROW
            EXECUTE FUNCTION update_updated_at_column()', p_schema_name, p_schema_name);
    
    -- Create tax_regimes table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.tax_regimes (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            code VARCHAR(10) NOT NULL UNIQUE,
            description VARCHAR(200) NOT NULL,
            active BOOLEAN NOT NULL DEFAULT true,
            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
        )', p_schema_name);
    
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_tax_regimes_code ON %I.tax_regimes(code)', p_schema_name);
    
    EXECUTE format('
        DROP TRIGGER IF EXISTS trg_tax_regimes_updated_at ON %I.tax_regimes;
        CREATE TRIGGER trg_tax_regimes_updated_at
            BEFORE UPDATE ON %I.tax_regimes
            FOR EACH ROW
            EXECUTE FUNCTION update_updated_at_column()', p_schema_name, p_schema_name);
    
    -- Create special_tax_regimes table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.special_tax_regimes (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            code VARCHAR(10) NOT NULL UNIQUE,
            description VARCHAR(200) NOT NULL,
            active BOOLEAN NOT NULL DEFAULT true,
            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
        )', p_schema_name);
    
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_special_tax_regimes_code ON %I.special_tax_regimes(code)', p_schema_name);
    
    EXECUTE format('
        DROP TRIGGER IF EXISTS trg_special_tax_regimes_updated_at ON %I.special_tax_regimes;
        CREATE TRIGGER trg_special_tax_regimes_updated_at
            BEFORE UPDATE ON %I.special_tax_regimes
            FOR EACH ROW
            EXECUTE FUNCTION update_updated_at_column()', p_schema_name, p_schema_name);
    
    -- Create companies table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.companies (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            legal_name VARCHAR(200) NOT NULL,
            trade_name VARCHAR(200),
            cnpj VARCHAR(14) NOT NULL UNIQUE,
            state_registration VARCHAR(50),
            city_registration VARCHAR(50),
            cnae VARCHAR(10),
            logo_url VARCHAR(500),
            tax_regime_id UUID NOT NULL,
            special_tax_regime_id UUID,
            active BOOLEAN NOT NULL DEFAULT true,
            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            
            -- Address fields (owned entity)
            address_zip_code VARCHAR(8),
            address_street VARCHAR(200),
            address_number VARCHAR(20),
            address_complement VARCHAR(100),
            address_district VARCHAR(100),
            address_city VARCHAR(100),
            address_state VARCHAR(2),
            
            -- Contact fields (owned entity)
            contact_phone VARCHAR(20),
            contact_mobile VARCHAR(20),
            contact_email VARCHAR(200),
            
            CONSTRAINT fk_companies_tax_regime FOREIGN KEY (tax_regime_id) 
                REFERENCES %I.tax_regimes(id) ON DELETE RESTRICT,
            CONSTRAINT fk_companies_special_tax_regime FOREIGN KEY (special_tax_regime_id) 
                REFERENCES %I.special_tax_regimes(id) ON DELETE RESTRICT,
            CONSTRAINT chk_companies_cnpj_length CHECK (LENGTH(cnpj) = 14),
            CONSTRAINT chk_companies_state_length CHECK (address_state IS NULL OR LENGTH(address_state) = 2)
        )', p_schema_name, p_schema_name, p_schema_name);
    
    -- Create indexes for companies table
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_companies_cnpj ON %I.companies(cnpj)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_companies_tax_regime ON %I.companies(tax_regime_id)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_companies_active ON %I.companies(active)', p_schema_name);
    
    EXECUTE format('
        DROP TRIGGER IF EXISTS trg_companies_updated_at ON %I.companies;
        CREATE TRIGGER trg_companies_updated_at
            BEFORE UPDATE ON %I.companies
            FOR EACH ROW
            EXECUTE FUNCTION update_updated_at_column()', p_schema_name, p_schema_name);
    
    -- Add table comments
    EXECUTE format('COMMENT ON TABLE %I.users IS ''Users table for tenant %I''', p_schema_name, p_schema_name);
    EXECUTE format('COMMENT ON TABLE %I.tax_regimes IS ''Tax regimes table for tenant %I''', p_schema_name, p_schema_name);
    EXECUTE format('COMMENT ON TABLE %I.special_tax_regimes IS ''Special tax regimes table for tenant %I''', p_schema_name, p_schema_name);
    EXECUTE format('COMMENT ON TABLE %I.companies IS ''Companies table for tenant %I''', p_schema_name, p_schema_name);
    
    -- Create metadata tables for dynamic CRUD
    -- Entity metadata table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.entities (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            name VARCHAR(100) NOT NULL UNIQUE,
            display_name VARCHAR(200) NOT NULL,
            table_name VARCHAR(100) NOT NULL,
            category VARCHAR(100),
            icon VARCHAR(50),
            description TEXT,
            is_active BOOLEAN NOT NULL DEFAULT true,
            allow_create BOOLEAN NOT NULL DEFAULT true,
            allow_read BOOLEAN NOT NULL DEFAULT true,
            allow_update BOOLEAN NOT NULL DEFAULT true,
            allow_delete BOOLEAN NOT NULL DEFAULT true,
            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
        )', p_schema_name);
    
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_entities_name ON %I.entities(name)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_entities_active ON %I.entities(is_active)', p_schema_name);
    
    -- Entity fields table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.entity_fields (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            entity_id UUID NOT NULL,
            name VARCHAR(100) NOT NULL,
            display_name VARCHAR(200) NOT NULL,
            column_name VARCHAR(100) NOT NULL,
            data_type VARCHAR(50) NOT NULL,
            field_type VARCHAR(50) NOT NULL DEFAULT ''text'',
            max_length INTEGER,
            is_required BOOLEAN NOT NULL DEFAULT false,
            is_unique BOOLEAN NOT NULL DEFAULT false,
            is_readonly BOOLEAN NOT NULL DEFAULT false,
            is_system_field BOOLEAN NOT NULL DEFAULT false,
            show_in_list BOOLEAN NOT NULL DEFAULT true,
            show_in_detail BOOLEAN NOT NULL DEFAULT true,
            show_in_create BOOLEAN NOT NULL DEFAULT true,
            show_in_update BOOLEAN NOT NULL DEFAULT true,
            list_order INTEGER NOT NULL DEFAULT 0,
            form_order INTEGER NOT NULL DEFAULT 0,
            default_value TEXT,
            validation_regex TEXT,
            validation_message TEXT,
            help_text TEXT,
            placeholder TEXT,
            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            
            CONSTRAINT fk_entity_fields_entity FOREIGN KEY (entity_id) 
                REFERENCES %I.entities(id) ON DELETE CASCADE,
            CONSTRAINT chk_entity_fields_data_type CHECK (data_type IN (''string'', ''number'', ''boolean'', ''date'', ''datetime'', ''uuid'', ''json'', ''text'')),
            CONSTRAINT chk_entity_fields_field_type CHECK (field_type IN (''text'', ''textarea'', ''number'', ''email'', ''password'', ''select'', ''multiselect'', ''date'', ''datetime'', ''checkbox'', ''file'', ''image''))
        )', p_schema_name, p_schema_name);
    
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_entity_fields_entity ON %I.entity_fields(entity_id)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_entity_fields_name ON %I.entity_fields(name)', p_schema_name);
    
    -- Entity relationships table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.entity_relationships (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            entity_id UUID NOT NULL,
            field_id UUID NOT NULL,
            related_entity_id UUID NOT NULL,
            relationship_type VARCHAR(50) NOT NULL,
            foreign_key_column VARCHAR(100),
            display_field VARCHAR(100),
            cascade_delete BOOLEAN NOT NULL DEFAULT false,
            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            
            CONSTRAINT fk_entity_relationships_entity FOREIGN KEY (entity_id) 
                REFERENCES %I.entities(id) ON DELETE CASCADE,
            CONSTRAINT fk_entity_relationships_field FOREIGN KEY (field_id) 
                REFERENCES %I.entity_fields(id) ON DELETE CASCADE,
            CONSTRAINT fk_entity_relationships_related FOREIGN KEY (related_entity_id) 
                REFERENCES %I.entities(id) ON DELETE CASCADE,
            CONSTRAINT chk_entity_relationships_type CHECK (relationship_type IN (''many-to-one'', ''one-to-many'', ''many-to-many''))
        )', p_schema_name, p_schema_name, p_schema_name, p_schema_name);
    
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_entity_relationships_entity ON %I.entity_relationships(entity_id)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_entity_relationships_related ON %I.entity_relationships(related_entity_id)', p_schema_name);
    
    -- Entity field options table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.entity_field_options (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            field_id UUID NOT NULL,
            value VARCHAR(100) NOT NULL,
            label VARCHAR(200) NOT NULL,
            display_order INTEGER NOT NULL DEFAULT 0,
            is_active BOOLEAN NOT NULL DEFAULT true,
            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            
            CONSTRAINT fk_entity_field_options_field FOREIGN KEY (field_id) 
                REFERENCES %I.entity_fields(id) ON DELETE CASCADE
        )', p_schema_name, p_schema_name);
    
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_entity_field_options_field ON %I.entity_field_options(field_id)', p_schema_name);
    
    -- Entity permissions table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.entity_permissions (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            entity_id UUID NOT NULL,
            role VARCHAR(50) NOT NULL,
            can_create BOOLEAN NOT NULL DEFAULT false,
            can_read BOOLEAN NOT NULL DEFAULT true,
            can_update BOOLEAN NOT NULL DEFAULT false,
            can_delete BOOLEAN NOT NULL DEFAULT false,
            can_read_own_only BOOLEAN NOT NULL DEFAULT false,
            field_permissions JSONB,
            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            
            CONSTRAINT fk_entity_permissions_entity FOREIGN KEY (entity_id) 
                REFERENCES %I.entities(id) ON DELETE CASCADE,
            CONSTRAINT chk_entity_permissions_role CHECK (role IN (''admin'', ''manager'', ''operator'')),
            CONSTRAINT uk_entity_permissions_entity_role UNIQUE (entity_id, role)
        )', p_schema_name, p_schema_name);
    
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_entity_permissions_entity ON %I.entity_permissions(entity_id)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_entity_permissions_role ON %I.entity_permissions(role)', p_schema_name);
    
    -- Create triggers for metadata tables
    EXECUTE format('
        DROP TRIGGER IF EXISTS trg_entities_updated_at ON %I.entities;
        CREATE TRIGGER trg_entities_updated_at
            BEFORE UPDATE ON %I.entities
            FOR EACH ROW
            EXECUTE FUNCTION update_updated_at_column()', p_schema_name, p_schema_name);
    
    EXECUTE format('
        DROP TRIGGER IF EXISTS trg_entity_fields_updated_at ON %I.entity_fields;
        CREATE TRIGGER trg_entity_fields_updated_at
            BEFORE UPDATE ON %I.entity_fields
            FOR EACH ROW
            EXECUTE FUNCTION update_updated_at_column()', p_schema_name, p_schema_name);
    
    EXECUTE format('
        DROP TRIGGER IF EXISTS trg_entity_permissions_updated_at ON %I.entity_permissions;
        CREATE TRIGGER trg_entity_permissions_updated_at
            BEFORE UPDATE ON %I.entity_permissions
            FOR EACH ROW
            EXECUTE FUNCTION update_updated_at_column()', p_schema_name, p_schema_name);
    
    -- =====================================================
    -- PRODUCTS MODULE TABLES
    -- =====================================================
    
    -- Create product_groups table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.product_groups (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            code VARCHAR(10) UNIQUE NOT NULL,
            name VARCHAR(100) NOT NULL,
            active BOOLEAN NOT NULL DEFAULT true,
            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
        )', p_schema_name);
    
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_product_groups_code ON %I.product_groups(code)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_product_groups_active ON %I.product_groups(active)', p_schema_name);
    
    EXECUTE format('
        DROP TRIGGER IF EXISTS trg_product_groups_updated_at ON %I.product_groups;
        CREATE TRIGGER trg_product_groups_updated_at
            BEFORE UPDATE ON %I.product_groups
            FOR EACH ROW
            EXECUTE FUNCTION update_updated_at_column()', p_schema_name, p_schema_name);
    
    -- Create product_subgroups table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.product_subgroups (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            code VARCHAR(10) UNIQUE NOT NULL,
            name VARCHAR(100) NOT NULL,
            product_group_id UUID,
            active BOOLEAN NOT NULL DEFAULT true,
            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            
            CONSTRAINT fk_subgroup_group FOREIGN KEY (product_group_id) 
                REFERENCES %I.product_groups(id) ON DELETE RESTRICT
        )', p_schema_name, p_schema_name);
    
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_product_subgroups_code ON %I.product_subgroups(code)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_product_subgroups_group ON %I.product_subgroups(product_group_id)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_product_subgroups_active ON %I.product_subgroups(active)', p_schema_name);
    
    EXECUTE format('
        DROP TRIGGER IF EXISTS trg_product_subgroups_updated_at ON %I.product_subgroups;
        CREATE TRIGGER trg_product_subgroups_updated_at
            BEFORE UPDATE ON %I.product_subgroups
            FOR EACH ROW
            EXECUTE FUNCTION update_updated_at_column()', p_schema_name, p_schema_name);
    
    -- Create brands table
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.brands (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            name VARCHAR(100) UNIQUE NOT NULL,
            active BOOLEAN NOT NULL DEFAULT true,
            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
        )', p_schema_name);
    
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_brands_name ON %I.brands(name)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_brands_active ON %I.brands(active)', p_schema_name);
    
    EXECUTE format('
        DROP TRIGGER IF EXISTS trg_brands_updated_at ON %I.brands;
        CREATE TRIGGER trg_brands_updated_at
            BEFORE UPDATE ON %I.brands
            FOR EACH ROW
            EXECUTE FUNCTION update_updated_at_column()', p_schema_name, p_schema_name);
    
    -- Create products table
    -- Create sequence for internal_code
    EXECUTE format('CREATE SEQUENCE IF NOT EXISTS %I.products_internal_code_seq START 1', p_schema_name);
    
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.products (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            internal_code VARCHAR(50) UNIQUE NOT NULL DEFAULT nextval(''%I.products_internal_code_seq''),
            barcode VARCHAR(13),
            description TEXT NOT NULL,
            active BOOLEAN NOT NULL DEFAULT true,
            product_group_id UUID,
            product_subgroup_id UUID,
            brand_id UUID,
            own_production BOOLEAN NOT NULL DEFAULT false,
            unit_of_measure VARCHAR(10) NOT NULL DEFAULT ''UN'',
            ncm VARCHAR(8) NOT NULL,
            cest VARCHAR(7),
            product_origin INTEGER NOT NULL DEFAULT 0,
            item_type INTEGER NOT NULL DEFAULT 0,
            incide_pis_cofins BOOLEAN NOT NULL DEFAULT true,
            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            
            CONSTRAINT fk_products_group FOREIGN KEY (product_group_id) 
                REFERENCES %I.product_groups(id) ON DELETE RESTRICT,
            CONSTRAINT fk_products_subgroup FOREIGN KEY (product_subgroup_id) 
                REFERENCES %I.product_subgroups(id) ON DELETE RESTRICT,
            CONSTRAINT fk_products_brand FOREIGN KEY (brand_id) 
                REFERENCES %I.brands(id) ON DELETE RESTRICT,
            CONSTRAINT chk_products_ncm_length CHECK (LENGTH(ncm) = 8),
            CONSTRAINT chk_products_cest_length CHECK (cest IS NULL OR LENGTH(cest) = 7),
            CONSTRAINT chk_products_product_origin CHECK (product_origin BETWEEN 0 AND 8),
            CONSTRAINT chk_products_item_type CHECK (item_type BETWEEN 0 AND 99)
        )', p_schema_name, p_schema_name, p_schema_name, p_schema_name, p_schema_name);
    
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_products_internal_code ON %I.products(internal_code)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_products_barcode ON %I.products(barcode)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_products_active ON %I.products(active)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_products_product_group ON %I.products(product_group_id)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_products_ncm ON %I.products(ncm)', p_schema_name);
    
    EXECUTE format('
        DROP TRIGGER IF EXISTS trg_products_updated_at ON %I.products;
        CREATE TRIGGER trg_products_updated_at
            BEFORE UPDATE ON %I.products
            FOR EACH ROW
            EXECUTE FUNCTION update_updated_at_column()', p_schema_name, p_schema_name);
    
    EXECUTE format('COMMENT ON TABLE %I.product_groups IS ''Product groups table for tenant %I''', p_schema_name, p_schema_name);
    EXECUTE format('COMMENT ON TABLE %I.product_subgroups IS ''Product subgroups table for tenant %I''', p_schema_name, p_schema_name);
    EXECUTE format('COMMENT ON TABLE %I.brands IS ''Brands table for tenant %I''', p_schema_name, p_schema_name);
    EXECUTE format('COMMENT ON TABLE %I.products IS ''Products table for tenant %I''', p_schema_name, p_schema_name);
    
    -- =====================================================
    -- PRODUCT PRICES TABLE (Append-only audit trail)
    -- =====================================================
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.product_prices (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            product_id UUID NOT NULL,
            price DECIMAL(15,2) NOT NULL,
            effective_date DATE NOT NULL DEFAULT CURRENT_DATE,
            active BOOLEAN NOT NULL DEFAULT true,
            created_by UUID NOT NULL,
            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            
            CONSTRAINT fk_product_prices_product FOREIGN KEY (product_id) 
                REFERENCES %I.products(id) ON DELETE RESTRICT,
            CONSTRAINT fk_product_prices_user FOREIGN KEY (created_by) 
                REFERENCES %I.users(id) ON DELETE RESTRICT,
            CONSTRAINT chk_product_prices_price CHECK (price >= 0)
        )', p_schema_name, p_schema_name, p_schema_name);
    
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_product_prices_product ON %I.product_prices(product_id)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_product_prices_effective_date ON %I.product_prices(effective_date)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_product_prices_created_by ON %I.product_prices(created_by)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_product_prices_active ON %I.product_prices(active)', p_schema_name);
    
    -- Trigger for product_prices updated_at
    EXECUTE format('
        DROP TRIGGER IF EXISTS trg_product_prices_updated_at ON %I.product_prices;
        CREATE TRIGGER trg_product_prices_updated_at
            BEFORE UPDATE ON %I.product_prices
            FOR EACH ROW
            EXECUTE FUNCTION update_updated_at_column()', p_schema_name, p_schema_name);
    
    EXECUTE format('COMMENT ON TABLE %I.product_prices IS ''Product price history (soft-delete enabled) for tenant %I''', p_schema_name, p_schema_name);
    
    -- =====================================================
    -- PRODUCT COSTS TABLE (Soft-delete enabled)
    -- =====================================================
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.product_costs (
            id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
            product_id UUID NOT NULL,
            cost_price DECIMAL(15,2) NOT NULL,
            effective_date DATE NOT NULL DEFAULT CURRENT_DATE,
            active BOOLEAN NOT NULL DEFAULT true,
            created_by UUID NOT NULL,
            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            
            CONSTRAINT fk_product_costs_product FOREIGN KEY (product_id) 
                REFERENCES %I.products(id) ON DELETE RESTRICT,
            CONSTRAINT fk_product_costs_user FOREIGN KEY (created_by) 
                REFERENCES %I.users(id) ON DELETE RESTRICT,
            CONSTRAINT chk_product_costs_price CHECK (cost_price >= 0)
        )', p_schema_name, p_schema_name, p_schema_name);
    
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_product_costs_product ON %I.product_costs(product_id)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_product_costs_effective_date ON %I.product_costs(effective_date)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_product_costs_created_by ON %I.product_costs(created_by)', p_schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_product_costs_active ON %I.product_costs(active)', p_schema_name);
    
    -- Trigger for product_costs updated_at
    EXECUTE format('
        DROP TRIGGER IF EXISTS trg_product_costs_updated_at ON %I.product_costs;
        CREATE TRIGGER trg_product_costs_updated_at
            BEFORE UPDATE ON %I.product_costs
            FOR EACH ROW
            EXECUTE FUNCTION update_updated_at_column()', p_schema_name, p_schema_name);
    
    EXECUTE format('COMMENT ON TABLE %I.product_costs IS ''Product cost history (soft-delete enabled) for tenant %I''', p_schema_name, p_schema_name);
    
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION create_tenant_schema(VARCHAR) IS 'Creates a complete tenant schema with all required tables and indexes';
