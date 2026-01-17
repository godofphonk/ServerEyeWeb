-- ServerEye Database Initialization Script
-- This script sets up the TimescaleDB database for ServerEye metrics storage

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_servers_user_id ON servers(user_id);
CREATE INDEX IF NOT EXISTS idx_servers_status ON servers(status);
CREATE INDEX IF NOT EXISTS idx_metrics_server_id ON metrics(server_id);
CREATE INDEX IF NOT EXISTS idx_metrics_type ON metrics(type);
CREATE INDEX IF NOT EXISTS idx_metrics_timestamp ON metrics(timestamp DESC);

-- Create user roles and permissions
CREATE ROLE IF NOT EXISTS servereye_app_role;
GRANT CONNECT ON DATABASE servereye TO servereye_app_role;
GRANT USAGE ON SCHEMA public TO servereye_app_role;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO servereye_app_role;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO servereye_app_role;

-- Set default privileges for future tables
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO servereye_app_role;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO servereye_app_role;

-- Create continuous aggregates for better performance
-- These will be created after the tables are populated
