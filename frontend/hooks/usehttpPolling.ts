import { useState, useCallback, useRef, useEffect } from 'react';
import { apiClient } from '@/lib/api';

interface UseMetricsPollingOptions {
  serverId: string;
  onMessage?: (data: any) => void;
  onError?: (error: string) => void;
  enabled?: boolean;
  interval?: number; // в секундах
}

export function usehttpPolling({
  serverId, 
  onMessage, 
  onError,
  enabled = true,
  interval = 30, // Default 30 seconds to respect rate limiting (100 req/min)
}: UseMetricsPollingOptions) {
  const [isConnected, setIsConnected] = useState(false);
  const [lastMessage, setLastMessage] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  const pollMetrics = useCallback(async () => {
    console.log('[Metrics] pollMetrics called for server:', serverId, 'enabled:', enabled, 'isLoading:', isLoading);
    
    if (!enabled || isLoading) {
      console.log('[Metrics] Skipping fetch - enabled:', enabled, 'isLoading:', isLoading);
      return;
    }

    setIsLoading(true);
    console.log('[Metrics] Starting fetch for server:', serverId);

    try {
      // Use C# backend endpoint for real metrics
      const end = new Date();
      const start = new Date(end.getTime() - 5 * 60 * 1000);
      const metrics = await apiClient.get<any>(`/servers/${serverId}/metrics/tiered?start=${start.toISOString()}&end=${end.toISOString()}&granularity=1m`);
      console.log('[Metrics] Success! Got real metrics:', metrics);
      
      // Process real metrics data from C# backend
      const lastDataPoint = metrics.dataPoints?.[metrics.dataPoints?.length - 1];
      const processedData = {
        cpu: metrics.summary?.avgCpu || lastDataPoint?.cpu_avg || 0,
        memory: metrics.summary?.avgMemory || lastDataPoint?.memory_avg || 0,
        disk: metrics.summary?.avgDisk || lastDataPoint?.disk_avg || 0,
        network: lastDataPoint?.network_avg || 0, // Используем последнюю точку данных для network
        load: lastDataPoint?.load_avg || 0,
        temperature: lastDataPoint?.temp_avg || 0,
        gpu_temperature: metrics.temperatureDetails?.gpu_temperature || 0, // Используем temperatureDetails из C# Backend
        networkDetails: metrics.networkDetails || null, // Добавляем networkDetails из C# Backend
        timestamp: new Date().toISOString(),
        serverId: metrics.serverId,
        serverName: metrics.serverName,
        dataPoints: metrics.dataPoints || [],
        totalPoints: metrics.totalPoints || 0,
        message: metrics.message || null,
        isCached: metrics.isCached || false
      };
      
      console.log('[Metrics] Processed real data:', processedData);
      
      // Log message if present
      if (metrics.message) {
        console.log('[Metrics] API Message:', metrics.message);
      }
      
      setLastMessage(processedData);
      setIsConnected(true);
      setError(null);
      onMessage?.(processedData);
    } catch (err: any) {
      console.error('[Metrics] Failed to fetch:', err);
      console.log('[Metrics] Error status:', err.response?.status);
      
      // Handle errors
      if (err.response?.status === 401) {
        setError('Authentication required');
        onError?.('Authentication required');
      } else {
        setError('Failed to fetch metrics');
        onError?.(err.message);
      }
    } finally {
      setIsLoading(false);
    }
  }, [serverId, enabled, onMessage, onError, isLoading]);

  // Setup automatic polling
  useEffect(() => {
    if (enabled && interval > 0) {
      // Initial fetch
      pollMetrics();
      
      // Setup interval
      intervalRef.current = setInterval(() => {
        pollMetrics();
      }, interval * 1000);
      
      return () => {
        if (intervalRef.current) {
          clearInterval(intervalRef.current);
          intervalRef.current = null;
        }
      };
    }
  }, [enabled, interval, pollMetrics]);

  return {
    isConnected,
    lastMessage,
    error,
    isLoading,
    fetchMetrics: pollMetrics,
    reconnect: pollMetrics,
    disconnect: () => {},
  };
}
