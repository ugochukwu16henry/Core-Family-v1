-- Core Family Database Initialization Script
-- Runs automatically when Docker PostgreSQL container starts for the first time
-- For production: use EF Core migrations instead

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";    -- for fast text search
CREATE EXTENSION IF NOT EXISTS "unaccent";   -- for language-neutral search

-- Confirm setup
SELECT current_database(), current_user, version();
