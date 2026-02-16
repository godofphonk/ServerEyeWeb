import { useState, useCallback } from 'react';
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
}: UseMetricsPollingOptions) {
  const [isConnected, setIsConnected] = useState(false);
  const [lastMessage, setLastMessage] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

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
      const metrics = await apiClient.get<any>(`/servers/${serverId}/metrics/dashboard`);
      console.log('[Metrics] Success! Got real metrics:', metrics);
      
      // Process real metrics data from C# backend
      const processedData = {
        cpu: metrics.summary?.avgCpu || 0,
        memory: metrics.summary?.avgMemory || 0,
        disk: metrics.summary?.avgDisk || 0,
        network: metrics.summary?.avgNetwork || 0,
        load: metrics.summary?.avgLoad || 0,
        temperature: metrics.dataPoints?.[metrics.dataPoints?.length - 1]?.temperature_details?.cpu_temperature || 
                   metrics.dataPoints?.[metrics.dataPoints?.length - 1]?.temperature?.avg || 0,
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
