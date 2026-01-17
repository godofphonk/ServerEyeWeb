-- ServerEye Tables Creation Script
-- Creates permanent and time-series tables for metrics storage

-- Permanent server registry table
CREATE TABLE IF NOT EXISTS servers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    server_key VARCHAR(255) UNIQUE NOT NULL,
    server_name VARCHAR(255) NOT NULL DEFAULT server_key,
    user_id UUID NOT NULL,
    hostname VARCHAR(255),
    ip_address INET,
    os_info TEXT,
    agent_version VARCHAR(50),
    status VARCHAR(20) DEFAULT 'offline' CHECK (status IN ('online', 'offline', 'error')),
    last_seen TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Main metrics hypertable (time-series data with 30-day TTL)
CREATE TABLE IF NOT EXISTS metrics (
    timestamp TIMESTAMPTZ NOT NULL,
    server_id UUID NOT NULL REFERENCES servers(id) ON DELETE CASCADE,
    metric_type VARCHAR(100) NOT NULL,
    metric_value DOUBLE PRECISION NOT NULL,
    metric_unit VARCHAR(50),
    labels JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Convert metrics table to hypertable
SELECT create_hypertable('metrics', 'timestamp', chunk_time_interval => INTERVAL '1 hour');

-- Create indexes for metrics table
CREATE INDEX IF NOT EXISTS idx_metrics_server_timestamp ON metrics(server_id, timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_metrics_type_timestamp ON metrics(metric_type, timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_metrics_labels_gin ON metrics USING GIN(labels);

-- CPU metrics table (specialized for CPU data)
CREATE TABLE IF NOT EXISTS cpu_metrics (
    timestamp TIMESTAMPTZ NOT NULL,
    server_id UUID NOT NULL REFERENCES servers(id) ON DELETE CASCADE,
    usage_total DOUBLE PRECISION,
    usage_user DOUBLE PRECISION,
    usage_system DOUBLE PRECISION,
    usage_idle DOUBLE PRECISION,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

SELECT create_hypertable('cpu_metrics', 'timestamp', chunk_time_interval => INTERVAL '1 hour');

-- Memory metrics table
CREATE TABLE IF NOT EXISTS memory_metrics (
    timestamp TIMESTAMPTZ NOT NULL,
    server_id UUID NOT NULL REFERENCES servers(id) ON DELETE CASCADE,
    total_gb DOUBLE PRECISION,
    used_gb DOUBLE PRECISION,
    available_gb DOUBLE PRECISION,
    free_gb DOUBLE PRECISION,
    buffers_gb DOUBLE PRECISION,
    cached_gb DOUBLE PRECISION,
    used_percent DOUBLE PRECISION,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

SELECT create_hypertable('memory_metrics', 'timestamp', chunk_time_interval => INTERVAL '1 hour');

-- Disk metrics table (JSON for multiple disks)
CREATE TABLE IF NOT EXISTS disk_metrics (
    timestamp TIMESTAMPTZ NOT NULL,
    server_id UUID NOT NULL REFERENCES servers(id) ON DELETE CASCADE,
    disk_details JSONB NOT NULL, -- Array of disk objects
    created_at TIMESTAMPTZ DEFAULT NOW()
);

SELECT create_hypertable('disk_metrics', 'timestamp', chunk_time_interval => INTERVAL '1 hour');

-- Network metrics table (JSON for multiple interfaces)
CREATE TABLE IF NOT EXISTS network_metrics (
    timestamp TIMESTAMPTZ NOT NULL,
    server_id UUID NOT NULL REFERENCES servers(id) ON DELETE CASCADE,
    interfaces JSONB NOT NULL, -- Array of network interface objects
    total_rx_mbps DOUBLE PRECISION,
    total_tx_mbps DOUBLE PRECISION,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

SELECT create_hypertable('network_metrics', 'timestamp', chunk_time_interval => INTERVAL '1 hour');

-- Temperature metrics table
CREATE TABLE IF NOT EXISTS temperature_metrics (
    timestamp TIMESTAMPTZ NOT NULL,
    server_id UUID NOT NULL REFERENCES servers(id) ON DELETE CASCADE,
    cpu_temperature DOUBLE PRECISION,
    gpu_temperature DOUBLE PRECISION,
    system_temperature DOUBLE PRECISION,
    storage_temperatures JSONB,
    highest_temperature DOUBLE PRECISION,
    temperature_unit VARCHAR(20) DEFAULT 'celsius',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

SELECT create_hypertable('temperature_metrics', 'timestamp', chunk_time_interval => INTERVAL '1 hour');

-- System details table
CREATE TABLE IF NOT EXISTS system_metrics (
    timestamp TIMESTAMPTZ NOT NULL,
    server_id UUID NOT NULL REFERENCES servers(id) ON DELETE CASCADE,
    kernel VARCHAR(255),
    architecture VARCHAR(50),
    uptime_seconds BIGINT,
    uptime_human TEXT,
    boot_time TIMESTAMPTZ,
    processes_total INTEGER,
    processes_running INTEGER,
    processes_sleeping INTEGER,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

SELECT create_hypertable('system_metrics', 'timestamp', chunk_time_interval => INTERVAL '1 hour');

-- Create triggers to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER update_servers_updated_at 
    BEFORE UPDATE ON servers 
    FOR EACH ROW 
    EXECUTE FUNCTION update_updated_at_column();
