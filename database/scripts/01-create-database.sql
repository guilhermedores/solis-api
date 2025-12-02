-- =============================================
-- Script: 01-create-database.sql
-- Description: Creates the main database and extensions
-- Author: Solis Team
-- Date: 2025-11-30
-- =============================================

-- Create database (execute this outside the database context)
-- This should be run by a superuser or database owner

-- Note: If running via docker-entrypoint-initdb.d, the database is already created
-- This script is provided for manual database creation if needed

-- CREATE DATABASE solis_pdv
--     WITH 
--     OWNER = solis_user
--     ENCODING = 'UTF8'
--     LC_COLLATE = 'en_US.utf8'
--     LC_CTYPE = 'en_US.utf8'
--     TABLESPACE = pg_default
--     CONNECTION LIMIT = -1;

-- Connect to the database (if running manually)
-- \c solis_pdv

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Grant necessary privileges
GRANT ALL PRIVILEGES ON DATABASE solis_db TO solis_user;
GRANT ALL ON SCHEMA public TO solis_user;

-- Create schema for multi-tenant management
COMMENT ON DATABASE solis_db IS 'Solis PDV Multi-tenant Database';
