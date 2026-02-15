import { useEffect, useRef, useState, useCallback } from 'react';
import { apiClient } from '@/lib/api';

interface UseMetricsPollingOptions {
  serverId: string;
  onMessage?: (data: any) => void;
  onError?: (error: string) => void;
  enabled?: boolean;
  interval?: number; // в секундах
}

export function useWebSocket({ 
  serverId, 
  onMessage, 
  onError,
  enabled = true,
  interval = 30 // по умолчанию каждые 30 секунд
}: UseMetricsPollingOptions) {
  const [isConnected, setIsConnected] = useState(false);
  const [lastMessage, setLastMessage] = useState<any>(null);
  const [error, setError] = useState<string | null>(null);
  const intervalRef = useRef<NodeJS.Timeout>();
  const isPollingRef = useRef(false);

  const pollMetrics = useCallback(async () => {
    if (!enabled || isPollingRef.current) return;

    isPollingRef.current = true;

    try {
      console.log(`[Metrics] Polling metrics for server ${serverId}`);
      const metrics = await apiClient.get<any>(`/servers/${serverId}/metrics/realtime?duration=5m`);
      
      setLastMessage(metrics);
      setIsConnected(true);
      setError(null);
      onMessage?.(metrics);
    } catch (err: any) {
      console.error('[Metrics] Failed to poll:', err);
      setError('Failed to fetch metrics');
      onError?.(err.message);
    } finally {
      isPollingRef.current = false;
    }
  }, [serverId, enabled, onMessage, onError]);

  const startPolling = useCallback(() => {
    if (!enabled) return;

    console.log(`[Metrics] Starting polling every ${interval}s`);
    
    // Первоначальный запрос
    pollMetrics();
    
    // Последующие запросы
    intervalRef.current = setInterval(() => {
      pollMetrics();
    }, interval * 1000);
  }, [enabled, interval, pollMetrics]);

  const stopPolling = useCallback(() => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = undefined;
    }
    setIsConnected(false);
    isPollingRef.current = false;
    console.log('[Metrics] Stopped polling');
  }, []);

  useEffect(() => {
    if (enabled) {
      startPolling();
    } else {
      stopPolling();
    }

    return () => {
      stopPolling();
    };
  }, [enabled, startPolling, stopPolling]);

  return {
    isConnected,
    lastMessage,
    error,
    reconnect: pollMetrics,
    disconnect: stopPolling,
  };
}
