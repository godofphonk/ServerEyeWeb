-- ServerEye Data Retention Script
-- Sets up 30-day TTL for metrics data and creates continuous aggregates

-- Create data retention policy for metrics tables (30 days)
SELECT add_retention_policy('metrics', INTERVAL '30 days');
SELECT add_retention_policy('cpu_metrics', INTERVAL '30 days');
SELECT add_retention_policy('memory_metrics', INTERVAL '30 days');
SELECT add_retention_policy('disk_metrics', INTERVAL '30 days');
SELECT add_retention_policy('network_metrics', INTERVAL '30 days');
SELECT add_retention_policy('temperature_metrics', INTERVAL '30 days');
SELECT add_retention_policy('system_metrics', INTERVAL '30 days');

-- Create continuous aggregates for better query performance
-- CPU usage hourly averages
CREATE MATERIALIZED VIEW cpu_usage_hourly
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 hour', timestamp) AS hour,
    server_id,
    AVG(usage_total) as avg_usage_total,
    AVG(usage_user) as avg_usage_user,
    AVG(usage_system) as avg_usage_system,
    AVG(usage_idle) as avg_usage_idle,
    MAX(usage_total) as max_usage_total,
    MIN(usage_total) as min_usage_total
FROM cpu_metrics
GROUP BY hour, server_id;

-- Memory usage hourly averages
CREATE MATERIALIZED VIEW memory_usage_hourly
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 hour', timestamp) AS hour,
    server_id,
    AVG(used_percent) as avg_used_percent,
    MAX(used_percent) as max_used_percent,
    MIN(used_percent) as min_used_percent,
    AVG(total_gb) as avg_total_gb,
    AVG(used_gb) as avg_used_gb
FROM memory_metrics
GROUP BY hour, server_id;

-- Temperature hourly averages
CREATE MATERIALIZED VIEW temperature_hourly
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 hour', timestamp) AS hour,
    server_id,
    AVG(cpu_temperature) as avg_cpu_temp,
    MAX(cpu_temperature) as max_cpu_temp,
    MIN(cpu_temperature) as min_cpu_temp,
    AVG(gpu_temperature) as avg_gpu_temp,
    MAX(gpu_temperature) as max_gpu_temp,
    MIN(gpu_temperature) as min_gpu_temp,
    AVG(highest_temperature) as avg_highest_temp,
    MAX(highest_temperature) as max_highest_temp
FROM temperature_metrics
GROUP BY hour, server_id;

-- Network hourly totals
CREATE MATERIALIZED VIEW network_hourly
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 hour', timestamp) AS hour,
    server_id,
    AVG(total_rx_mbps) as avg_rx_mbps,
    AVG(total_tx_mbps) as avg_tx_mbps,
    MAX(total_rx_mbps) as max_rx_mbps,
    MAX(total_tx_mbps) as max_tx_mbps
FROM network_metrics
GROUP BY hour, server_id;

-- Disk usage hourly averages (extract from JSON)
CREATE MATERIALIZED VIEW disk_usage_hourly
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 hour', timestamp) AS hour,
    server_id,
    AVG((disk_detail->>'used_percent')::DOUBLE PRECISION) as avg_used_percent,
    MAX((disk_detail->>'used_percent')::DOUBLE PRECISION) as max_used_percent,
    AVG((disk_detail->>'total_gb')::DOUBLE PRECISION) as avg_total_gb,
    AVG((disk_detail->>'used_gb')::DOUBLE PRECISION) as avg_used_gb
FROM disk_metrics, jsonb_array_elements(disk_details) as disk_detail
GROUP BY hour, server_id;

-- Set refresh policies for continuous aggregates
-- Refresh every 5 minutes for real-time data
SELECT add_continuous_aggregate_policy('cpu_usage_hourly', 
    start_offset => INTERVAL '1 hour',
    end_offset => INTERVAL '5 minutes',
    schedule_interval => INTERVAL '5 minutes');

SELECT add_continuous_aggregate_policy('memory_usage_hourly', 
    start_offset => INTERVAL '1 hour',
    end_offset => INTERVAL '5 minutes',
    schedule_interval => INTERVAL '5 minutes');

SELECT add_continuous_aggregate_policy('temperature_hourly', 
    start_offset => INTERVAL '1 hour',
    end_offset => INTERVAL '5 minutes',
    schedule_interval => INTERVAL '5 minutes');

SELECT add_continuous_aggregate_policy('network_hourly', 
    start_offset => INTERVAL '1 hour',
    end_offset => INTERVAL '5 minutes',
    schedule_interval => INTERVAL '5 minutes');

SELECT add_continuous_aggregate_policy('disk_usage_hourly', 
    start_offset => INTERVAL '1 hour',
    end_offset => INTERVAL '5 minutes',
    schedule_interval => INTERVAL '5 minutes');

-- Create compression policies for better storage efficiency
-- Compress data after 1 hour
SELECT add_compression_policy('metrics', INTERVAL '1 hour');
SELECT add_compression_policy('cpu_metrics', INTERVAL '1 hour');
SELECT add_compression_policy('memory_metrics', INTERVAL '1 hour');
SELECT add_compression_policy('disk_metrics', INTERVAL '1 hour');
SELECT add_compression_policy('network_metrics', INTERVAL '1 hour');
SELECT add_compression_policy('temperature_metrics', INTERVAL '1 hour');
SELECT add_compression_policy('system_metrics', INTERVAL '1 hour');

-- Set compression segment size for optimal performance
ALTER TABLE metrics SET (
    timescaledb.compress_segmentby = 'server_id, metric_type',
    timescaledb.compress_orderby = 'timestamp DESC'
);

ALTER TABLE cpu_metrics SET (
    timescaledb.compress_segmentby = 'server_id',
    timescaledb.compress_orderby = 'timestamp DESC'
);

ALTER TABLE memory_metrics SET (
    timescaledb.compress_segmentby = 'server_id',
    timescaledb.compress_orderby = 'timestamp DESC'
);

ALTER TABLE temperature_metrics SET (
    timescaledb.compress_segmentby = 'server_id',
    timescaledb.compress_orderby = 'timestamp DESC'
);
