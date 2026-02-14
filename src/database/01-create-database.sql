-- ============================================================================
-- GTD Todo Application - Database Creation Script
-- ============================================================================
-- Creates the PostgreSQL database for the GTD Todo application
-- Run this script as a PostgreSQL superuser

-- Drop database if it exists (for clean initialization)
DROP DATABASE IF EXISTS todo_app;

-- Create database
CREATE DATABASE todo_app
    OWNER postgres
    ENCODING 'UTF8'
    LOCALE 'en_US.UTF-8'
    TEMPLATE template0;

-- Connect to the new database and set up required extensions
\c todo_app

-- Enable UUID extension (for uuid data type)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Set default timezone to UTC
SET TIMEZONE = 'UTC';

-- Create schema comments for documentation
COMMENT ON DATABASE todo_app IS 'GTD (Getting Things Done) Todo Application Database';
