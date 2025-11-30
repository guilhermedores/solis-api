-- =============================================
-- Script: 99-rollback-all.sql
-- Description: Rollback script - drops all tenant schemas and tables
-- Author: Solis Team
-- Date: 2025-11-30
-- WARNING: This will delete ALL data from the database!
-- =============================================

-- WARNING: Uncomment only if you really want to drop everything!

-- Drop all tenant schemas
-- DO $$
-- DECLARE
--     tenant_rec RECORD;
-- BEGIN
--     FOR tenant_rec IN SELECT schema_name FROM public.tenants
--     LOOP
--         EXECUTE format('DROP SCHEMA IF EXISTS %I CASCADE', tenant_rec.schema_name);
--         RAISE NOTICE 'Dropped schema: %', tenant_rec.schema_name;
--     END LOOP;
-- END $$;

-- Drop tenants table
-- DROP TABLE IF EXISTS public.tenants CASCADE;

-- Drop functions
-- DROP FUNCTION IF EXISTS create_tenant_schema(VARCHAR) CASCADE;
-- DROP FUNCTION IF EXISTS update_updated_at_column() CASCADE;

-- Drop extensions (uncomment if needed)
-- DROP EXTENSION IF EXISTS "uuid-ossp" CASCADE;
-- DROP EXTENSION IF EXISTS "pgcrypto" CASCADE;

-- RAISE NOTICE 'Database rolled back successfully. All data has been deleted.';
