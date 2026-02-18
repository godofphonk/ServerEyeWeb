"use client";

import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Activity, Wifi, WifiOff, RefreshCw } from 'lucide-react';
import { usehttpPolling } from '@/hooks/usehttpPolling';
import { LiveMetrics } from '@/types';
import { Card } from '@/components/ui/Card';
import { MetricsNotification } from '@/components/MetricsNotification';

interface LiveMetricsPanelProps {
  serverId: string;
  enabled?: boolean;
}

export default function LiveMetricsPanel({ serverId, enabled = true }: LiveMetricsPanelProps) {
  const [metricsHistory, setMetricsHistory] = useState<LiveMetrics[]>([]);
  const [apiMessage, setApiMessage] = useState<string | null>(null);
  const maxHistoryLength = 60; // Keep last 60 data points

  const { isConnected, lastMessage, error, fetchMetrics, isLoading } = usehttpPolling({
    serverId,
    enabled,
    onMessage: (data) => {
      setMetricsHistory(prev => {
        const newHistory = [...prev, data];
        if (newHistory.length > maxHistoryLength) {
          return newHistory.slice(-maxHistoryLength);
        }
        return newHistory;
      });
      
      // Show API message if present
      if (data.message) {
        setApiMessage(data.message);
      }
    },
  });

  useEffect(() => {
    if (enabled && fetchMetrics) {
      fetchMetrics();
    }
  }, [serverId, enabled]); // Remove fetchMetrics to prevent infinite loop

  const getMetricColor = (value: number, type: 'cpu' | 'memory' | 'disk' | 'temperature') => {
    switch (type) {
      case 'cpu':
      case 'temperature':
        if (value > 80) return 'text-red-400';
        if (value > 60) return 'text-yellow-400';
        return 'text-green-400';
      case 'memory':
      case 'disk':
        if (value > 90) return 'text-red-400';
        if (value > 75) return 'text-yellow-400';
        return 'text-green-400';
      default:
        return 'text-gray-400';
    }
  };

  if (!enabled) return null;

  return (
    <Card className="p-6 border-blue-500/20 bg-gradient-to-br from-blue-500/5 to-purple-500/5">
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <div className="relative">
            <Activity className="w-6 h-6 text-blue-400" />
            {isConnected && (
              <motion.div
                className="absolute -top-1 -right-1 w-3 h-3 bg-green-500 rounded-full"
                animate={{ scale: [1, 1.2, 1] }}
                transition={{ repeat: Infinity, duration: 2 }}
              />
            )}
          </div>
          <div>
            <h3 className="text-lg font-bold">Live Metrics</h3>
            <p className="text-sm text-gray-400">Real-time monitoring</p>
          </div>
        </div>
        
        <div className="flex items-center gap-2">
          {isConnected ? (
            <>
              <Wifi className="w-5 h-5 text-green-400" />
              <span className="text-sm text-green-400 font-semibold">Connected</span>
            </>
          ) : (
            <>
              <WifiOff className="w-5 h-5 text-red-400" />
              <span className="text-sm text-red-400 font-semibold">
                {error || 'Disconnected'}
              </span>
            </>
          )}
          
          <button
            onClick={fetchMetrics}
            disabled={isLoading}
            className="p-2 rounded-lg bg-blue-500/10 hover:bg-blue-500/20 transition-colors disabled:opacity-50"
            title="Обновить метрики"
          >
            <RefreshCw className={`w-4 h-4 text-blue-400 ${isLoading ? 'animate-spin' : ''}`} />
          </button>
        </div>
      </div>

      <AnimatePresence mode="wait">
        {lastMessage ? (
          <motion.div
            key={lastMessage.timestamp}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -10 }}
            className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4"
          >
            <div className="bg-white/5 rounded-lg p-4">
              <p className="text-xs text-gray-400 mb-1">CPU</p>
              <p className={`text-2xl font-bold ${getMetricColor(lastMessage?.cpu || 0, 'cpu')}`}>
                {(lastMessage?.cpu || 0).toFixed(1)}%
              </p>
            </div>

            <div className="bg-white/5 rounded-lg p-4">
              <p className="text-xs text-gray-400 mb-1">Memory</p>
              <p className={`text-2xl font-bold ${getMetricColor(lastMessage?.memory || 0, 'memory')}`}>
                {(lastMessage?.memory || 0).toFixed(1)}%
              </p>
            </div>

            <div className="bg-white/5 rounded-lg p-4">
              <p className="text-xs text-gray-400 mb-1">Disk</p>
              <p className={`text-2xl font-bold ${getMetricColor(lastMessage?.disk || 0, 'disk')}`}>
                {(lastMessage?.disk || 0).toFixed(1)}%
              </p>
            </div>

            <div className="bg-white/5 rounded-lg p-4">
              <p className="text-xs text-gray-400 mb-1">Network</p>
              <p className="text-2xl font-bold text-blue-400">
                {(lastMessage?.network || 0).toFixed(2)} MB/s
              </p>
            </div>

            <div className="bg-white/5 rounded-lg p-4">
              <p className="text-xs text-gray-400 mb-1">CPU Temp</p>
              <p className={`text-2xl font-bold ${getMetricColor(lastMessage?.temperature || 0, 'temperature')}`}>
                {(lastMessage?.temperature || 0).toFixed(1)}°C
              </p>
            </div>

            <div className="bg-white/5 rounded-lg p-4">
              <p className="text-xs text-gray-400 mb-1">GPU Temp</p>
              <p className={`text-2xl font-bold ${getMetricColor(lastMessage?.gpu_temperature || 0, 'temperature')}`}>
                {(lastMessage?.gpu_temperature || 0).toFixed(1)}°C
              </p>
            </div>
          </motion.div>
        ) : (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="text-center py-8 text-gray-400"
          >
            <Activity className="w-12 h-12 mx-auto mb-3 opacity-50" />
            <p>Waiting for live data...</p>
          </motion.div>
        )}
      </AnimatePresence>

      {lastMessage && (
        <div className="mt-4 text-xs text-gray-500 text-center">
          Last update: {new Date(lastMessage?.timestamp || new Date().toISOString()).toLocaleTimeString()}
        </div>
      )}
      
      <MetricsNotification 
        message={apiMessage} 
        onClose={() => setApiMessage(null)} 
      />
    </Card>
  );
}
