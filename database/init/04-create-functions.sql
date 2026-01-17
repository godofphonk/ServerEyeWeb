-- ServerEye Database Functions
-- Helper functions for metrics insertion and querying

-- Function to insert complete metrics payload
CREATE OR REPLACE FUNCTION insert_server_metrics(
    p_server_key VARCHAR(255),
    p_timestamp TIMESTAMPTZ,
    p_cpu_usage JSONB,
    p_memory_details JSONB,
    p_disk_details JSONB,
    p_network_details JSONB,
    p_temperature_details JSONB,
    p_system_details JSONB,
    p_status JSONB
) RETURNS UUID AS $$
DECLARE
    v_server_id UUID;
    v_hostname VARCHAR(255);
    v_os_info TEXT;
    v_agent_version VARCHAR(50);
BEGIN
    -- Get or create server
    SELECT id INTO v_server_id 
    FROM servers 
    WHERE server_key = p_server_key;
    
    IF v_server_id IS NULL THEN
        -- Create new server record
        INSERT INTO servers (server_key, server_name, user_id, hostname, os_info, agent_version, status, last_seen)
        VALUES (
            p_server_key,
            COALESCE((p_status->>'hostname'), p_server_key),
            '00000000-0000-0000-0000-000000000001', -- Default user ID, should be replaced with actual user
            (p_status->>'hostname'),
            (p_status->>'os_info'),
            (p_status->>'agent_version'),
            CASE WHEN (p_status->>'online')::BOOLEAN THEN 'online' ELSE 'offline' END,
            p_timestamp
        )
        RETURNING id INTO v_server_id;
    ELSE
        -- Update existing server
        UPDATE servers 
        SET 
            hostname = COALESCE((p_status->>'hostname'), hostname),
            os_info = COALESCE((p_status->>'os_info'), os_info),
            agent_version = COALESCE((p_status->>'agent_version'), agent_version),
            status = CASE WHEN (p_status->>'online')::BOOLEAN THEN 'online' ELSE 'offline' END,
            last_seen = p_timestamp,
            updated_at = NOW()
        WHERE id = v_server_id;
    END IF;
    
    -- Insert CPU metrics
    INSERT INTO cpu_metrics (timestamp, server_id, usage_total, usage_user, usage_system, usage_idle)
    VALUES (
        p_timestamp,
        v_server_id,
        (p_cpu_usage->>'usage_total')::DOUBLE PRECISION,
        (p_cpu_usage->>'usage_user')::DOUBLE PRECISION,
        (p_cpu_usage->>'usage_system')::DOUBLE PRECISION,
        (p_cpu_usage->>'usage_idle')::DOUBLE PRECISION
    );
    
    -- Insert memory metrics
    INSERT INTO memory_metrics (
        timestamp, server_id, total_gb, used_gb, available_gb, free_gb, 
        buffers_gb, cached_gb, used_percent
    )
    VALUES (
        p_timestamp,
        v_server_id,
        (p_memory_details->>'total_gb')::DOUBLE PRECISION,
        (p_memory_details->>'used_gb')::DOUBLE PRECISION,
        (p_memory_details->>'available_gb')::DOUBLE PRECISION,
        (p_memory_details->>'free_gb')::DOUBLE PRECISION,
        (p_memory_details->>'buffers_gb')::DOUBLE PRECISION,
        (p_memory_details->>'cached_gb')::DOUBLE PRECISION,
        (p_memory_details->>'used_percent')::DOUBLE PRECISION
    );
    
    -- Insert disk metrics
    INSERT INTO disk_metrics (timestamp, server_id, disk_details)
    VALUES (p_timestamp, v_server_id, p_disk_details);
    
    -- Insert network metrics
    INSERT INTO network_metrics (timestamp, server_id, interfaces, total_rx_mbps, total_tx_mbps)
    VALUES (
        p_timestamp,
        v_server_id,
        p_network_details->'interfaces',
        (p_network_details->>'total_rx_mbps')::DOUBLE PRECISION,
        (p_network_details->>'total_tx_mbps')::DOUBLE PRECISION
    );
    
    -- Insert temperature metrics
    INSERT INTO temperature_metrics (
        timestamp, server_id, cpu_temperature, gpu_temperature, system_temperature,
        storage_temperatures, highest_temperature, temperature_unit
    )
    VALUES (
        p_timestamp,
        v_server_id,
        (p_temperature_details->>'cpu_temperature')::DOUBLE PRECISION,
        (p_temperature_details->>'gpu_temperature')::DOUBLE PRECISION,
        (p_temperature_details->>'system_temperature')::DOUBLE PRECISION,
        p_temperature_details->'storage_temperatures',
        (p_temperature_details->>'highest_temperature')::DOUBLE PRECISION,
        (p_temperature_details->>'temperature_unit')
    );
    
    -- Insert system metrics
    INSERT INTO system_metrics (
        timestamp, server_id, kernel, architecture, uptime_seconds, uptime_human,
        boot_time, processes_total, processes_running, processes_sleeping
    )
    VALUES (
        p_timestamp,
        v_server_id,
        (p_system_details->>'kernel'),
        (p_system_details->>'architecture'),
        (p_system_details->>'uptime_seconds')::BIGINT,
        (p_system_details->>'uptime_human'),
        (p_system_details->>'boot_time')::TIMESTAMPTZ,
        (p_system_details->>'processes_total')::INTEGER,
        (p_system_details->>'processes_running')::INTEGER,
        (p_system_details->>'processes_sleeping')::INTEGER
    );
    
    RETURN v_server_id;
END;
$$ LANGUAGE plpgsql;

-- Function to get latest metrics for a server
CREATE OR REPLACE FUNCTION get_latest_metrics(p_server_key VARCHAR(255))
RETURNS JSONB AS $$
DECLARE
    v_server_id UUID;
    v_result JSONB;
BEGIN
    -- Get server ID
    SELECT id INTO v_server_id FROM servers WHERE server_key = p_server_key;
    
    IF v_server_id IS NULL THEN
        RETURN NULL;
    END IF;
    
    -- Build result JSON with latest metrics
    SELECT jsonb_build_object(
        'server_info', (
            SELECT jsonb_build_object(
                'server_key', server_key,
                'server_name', server_name,
                'hostname', hostname,
                'os_info', os_info,
                'agent_version', agent_version,
                'status', status,
                'last_seen', last_seen
            )
            FROM servers WHERE id = v_server_id
        ),
        'cpu_usage', (
            SELECT jsonb_build_object(
                'usage_total', usage_total,
                'usage_user', usage_user,
                'usage_system', usage_system,
                'usage_idle', usage_idle,
                'timestamp', timestamp
            )
            FROM cpu_metrics 
            WHERE server_id = v_server_id 
            ORDER BY timestamp DESC LIMIT 1
        ),
        'memory_details', (
            SELECT jsonb_build_object(
                'total_gb', total_gb,
                'used_gb', used_gb,
                'available_gb', available_gb,
                'free_gb', free_gb,
                'buffers_gb', buffers_gb,
                'cached_gb', cached_gb,
                'used_percent', used_percent,
                'timestamp', timestamp
            )
            FROM memory_metrics 
            WHERE server_id = v_server_id 
            ORDER BY timestamp DESC LIMIT 1
        ),
        'disk_details', (
            SELECT disk_details
            FROM disk_metrics 
            WHERE server_id = v_server_id 
            ORDER BY timestamp DESC LIMIT 1
        ),
        'network_details', (
            SELECT jsonb_build_object(
                'interfaces', interfaces,
                'total_rx_mbps', total_rx_mbps,
                'total_tx_mbps', total_tx_mbps,
                'timestamp', timestamp
            )
            FROM network_metrics 
            WHERE server_id = v_server_id 
            ORDER BY timestamp DESC LIMIT 1
        ),
        'temperature_details', (
            SELECT jsonb_build_object(
                'cpu_temperature', cpu_temperature,
                'gpu_temperature', gpu_temperature,
                'system_temperature', system_temperature,
                'storage_temperatures', storage_temperatures,
                'highest_temperature', highest_temperature,
                'temperature_unit', temperature_unit,
                'timestamp', timestamp
            )
            FROM temperature_metrics 
            WHERE server_id = v_server_id 
            ORDER BY timestamp DESC LIMIT 1
        ),
        'system_details', (
            SELECT jsonb_build_object(
                'kernel', kernel,
                'architecture', architecture,
                'uptime_seconds', uptime_seconds,
                'uptime_human', uptime_human,
                'boot_time', boot_time,
                'processes_total', processes_total,
                'processes_running', processes_running,
                'processes_sleeping', processes_sleeping,
                'timestamp', timestamp
            )
            FROM system_metrics 
            WHERE server_id = v_server_id 
            ORDER BY timestamp DESC LIMIT 1
        )
    ) INTO v_result;
    
    RETURN v_result;
END;
$$ LANGUAGE plpgsql;

-- Function to get metrics history for charts
CREATE OR REPLACE FUNCTION get_metrics_history(
    p_server_key VARCHAR(255),
    p_metric_type VARCHAR(100),
    p_start_time TIMESTAMPTZ,
    p_end_time TIMESTAMPTZ,
    p_interval VARCHAR(50) DEFAULT '1 hour'
)
RETURNS TABLE(timestamp TIMESTAMPTZ, value DOUBLE PRECISION) AS $$
DECLARE
    v_server_id UUID;
BEGIN
    -- Get server ID
    SELECT id INTO v_server_id FROM servers WHERE server_key = p_server_key;
    
    IF v_server_id IS NULL THEN
        RETURN;
    END IF;
    
    -- Return query based on metric type
    CASE p_metric_type
        WHEN 'cpu_usage_total' THEN
            RETURN QUERY
            SELECT time_bucket(p_interval, timestamp) as timestamp, AVG(usage_total) as value
            FROM cpu_metrics 
            WHERE server_id = v_server_id 
                AND timestamp BETWEEN p_start_time AND p_end_time
            GROUP BY time_bucket(p_interval, timestamp)
            ORDER BY timestamp;
            
        WHEN 'memory_usage_percent' THEN
            RETURN QUERY
            SELECT time_bucket(p_interval, timestamp) as timestamp, AVG(used_percent) as value
            FROM memory_metrics 
            WHERE server_id = v_server_id 
                AND timestamp BETWEEN p_start_time AND p_end_time
            GROUP BY time_bucket(p_interval, timestamp)
            ORDER BY timestamp;
            
        WHEN 'cpu_temperature' THEN
            RETURN QUERY
            SELECT time_bucket(p_interval, timestamp) as timestamp, AVG(cpu_temperature) as value
            FROM temperature_metrics 
            WHERE server_id = v_server_id 
                AND timestamp BETWEEN p_start_time AND p_end_time
            GROUP BY time_bucket(p_interval, timestamp)
            ORDER BY timestamp;
            
        WHEN 'network_rx_mbps' THEN
            RETURN QUERY
            SELECT time_bucket(p_interval, timestamp) as timestamp, AVG(total_rx_mbps) as value
            FROM network_metrics 
            WHERE server_id = v_server_id 
                AND timestamp BETWEEN p_start_time AND p_end_time
            GROUP BY time_bucket(p_interval, timestamp)
            ORDER BY timestamp;
            
        WHEN 'network_tx_mbps' THEN
            RETURN QUERY
            SELECT time_bucket(p_interval, timestamp) as timestamp, AVG(total_tx_mbps) as value
            FROM network_metrics 
            WHERE server_id = v_server_id 
                AND timestamp BETWEEN p_start_time AND p_end_time
            GROUP BY time_bucket(p_interval, timestamp)
            ORDER BY timestamp;
    END CASE;
END;
$$ LANGUAGE plpgsql;
