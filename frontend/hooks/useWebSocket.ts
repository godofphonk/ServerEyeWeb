import { useEffect, useRef, useState, useCallback } from 'react';
import { LiveMetrics, WebSocketTokenResponse } from '@/types';
import { apiClient } from '@/lib/api';

interface UseWebSocketOptions {
  serverId: string;
  onMessage?: (data: LiveMetrics) => void;
  onError?: (error: Event) => void;
  onClose?: () => void;
  enabled?: boolean;
}

export function useWebSocket({ 
  serverId, 
  onMessage, 
  onError, 
  onClose,
  enabled = true 
}: UseWebSocketOptions) {
  const [isConnected, setIsConnected] = useState(false);
  const [lastMessage, setLastMessage] = useState<LiveMetrics | null>(null);
  const [error, setError] = useState<string | null>(null);
  const wsRef = useRef<WebSocket | null>(null);
  const reconnectTimeoutRef = useRef<NodeJS.Timeout>();
  const tokenExpiryRef = useRef<Date | null>(null);

  const connect = useCallback(async () => {
    if (!enabled) return;

    try {
      const tokenResponse = await apiClient.post<WebSocketTokenResponse>(
        `/servers/${serverId}/metrics/live-token`,
        {}
      );

      tokenExpiryRef.current = new Date(tokenResponse.expiresAt);

      const ws = new WebSocket(tokenResponse.wsUrl);

      ws.onopen = () => {
        console.log('[WebSocket] Connected to live metrics');
        setIsConnected(true);
        setError(null);
      };

      ws.onmessage = (event) => {
        try {
          const data: LiveMetrics = JSON.parse(event.data);
          setLastMessage(data);
          onMessage?.(data);
        } catch (err) {
          console.error('[WebSocket] Failed to parse message:', err);
        }
      };

      ws.onerror = (event) => {
        console.error('[WebSocket] Error:', event);
        setError('WebSocket connection error');
        onError?.(event);
      };

      ws.onclose = () => {
        console.log('[WebSocket] Connection closed');
        setIsConnected(false);
        wsRef.current = null;
        onClose?.();

        if (enabled) {
          reconnectTimeoutRef.current = setTimeout(() => {
            console.log('[WebSocket] Reconnecting...');
            connect();
          }, 5000);
        }
      };

      wsRef.current = ws;

      const tokenRefreshInterval = setInterval(() => {
        if (tokenExpiryRef.current) {
          const timeUntilExpiry = tokenExpiryRef.current.getTime() - Date.now();
          if (timeUntilExpiry < 5 * 60 * 1000) {
            console.log('[WebSocket] Token expiring soon, reconnecting...');
            disconnect();
            connect();
          }
        }
      }, 60000);

      return () => {
        clearInterval(tokenRefreshInterval);
      };
    } catch (err) {
      console.error('[WebSocket] Failed to get token:', err);
      setError('Failed to connect to live metrics');
      
      if (enabled) {
        reconnectTimeoutRef.current = setTimeout(() => {
          connect();
        }, 10000);
      }
    }
  }, [serverId, enabled, onMessage, onError, onClose]);

  const disconnect = useCallback(() => {
    if (reconnectTimeoutRef.current) {
      clearTimeout(reconnectTimeoutRef.current);
    }

    if (wsRef.current) {
      wsRef.current.close();
      wsRef.current = null;
    }

    setIsConnected(false);
  }, []);

  useEffect(() => {
    if (enabled) {
      connect();
    }

    return () => {
      disconnect();
    };
  }, [enabled, connect, disconnect]);

  return {
    isConnected,
    lastMessage,
    error,
    reconnect: connect,
    disconnect,
  };
}
