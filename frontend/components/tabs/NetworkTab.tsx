"use client";

import { Wifi, Activity, TrendingUp, TrendingDown } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import CurrentMetricsCard from '@/components/charts/CurrentMetricsCard';
import MetricsLineChart from '@/components/charts/MetricsLineChart';
import TimeRangeSelector from '@/components/TimeRangeSelector';
import { DashboardMetrics, MetricsResponse, NetworkDetails, NetworkInterface } from '@/types';

interface NetworkTabProps {
  dashboardMetrics: DashboardMetrics | null;
  historicalMetrics: MetricsResponse | null;
  networkDetails?: NetworkDetails | null;
  timeRange?: '1h' | '6h' | '24h' | '7d' | '30d';
  onTimeRangeChange?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
}

export default function NetworkTab({ dashboardMetrics, historicalMetrics, networkDetails, timeRange = '1h', onTimeRangeChange }: NetworkTabProps) {
  console.log('[NetworkTab] networkDetails:', networkDetails);
  return (
    <div className="space-y-6">
      {/* Network Overview Cards */}
      <div>
        <h3 className="text-lg font-semibold flex items-center gap-2 mb-4">
          <Wifi className="w-5 h-5 text-green-400" />
          Network Overview
        </h3>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {dashboardMetrics?.current && (
            <>
              <CurrentMetricsCard
                icon={TrendingUp}
                label="Download"
                value={dashboardMetrics.current.network * 10}
                unit="MB/s"
                trend="12.5"
                color="green"
              />
              <CurrentMetricsCard
                icon={TrendingDown}
                label="Upload"
                value={dashboardMetrics.current.network * 3}
                unit="MB/s"
                trend="-5.2"
                color="blue"
              />
              <CurrentMetricsCard
                icon={Activity}
                label="Connections"
                value={42}
                unit="active"
                trend="8"
                color="purple"
              />
              <CurrentMetricsCard
                icon={Wifi}
                label="Bandwidth"
                value={1000}
                unit="Mbps"
                color="cyan"
              />
            </>
          )}
        </div>
      </div>

      {/* Network Charts */}
      {historicalMetrics?.data && historicalMetrics.data.length > 0 ? (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <Card className="p-6">
            <div className="flex justify-between items-center mb-4">
              <h4 className="text-sm font-medium text-gray-400">Network Traffic</h4>
              {onTimeRangeChange && (
                <TimeRangeSelector 
                  timeRange={timeRange} 
                  onTimeRangeChange={onTimeRangeChange} 
                />
              )}
            </div>
            <div className="h-80">
              <MetricsLineChart
                data={historicalMetrics.data}
                metricType="network"
                title=""
                color="#10b981"
                unit="MB/s"
                timeRange={timeRange}
              />
            </div>
          </Card>

          <Card className="p-6">
            <CardHeader>
              <CardTitle>Network Interfaces</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4 max-h-64 overflow-y-auto">
                {networkDetails?.interfaces && networkDetails.interfaces.length > 0 ? (
                  networkDetails.interfaces.map((networkInterface: NetworkInterface) => (
                    <div key={networkInterface.name} className="p-3 bg-gray-800 rounded-lg">
                      <div className="flex justify-between items-center mb-2">
                        <span className="font-medium">{networkInterface.name}</span>
                        <span className={`text-xs px-2 py-1 rounded ${
                          networkInterface.status === 'up' 
                            ? 'bg-green-500/20 text-green-400' 
                            : 'bg-gray-500/20 text-gray-400'
                        }`}>
                          {networkInterface.status === 'up' ? 'Active' : 'Inactive'}
                        </span>
                      </div>
                      <div className="mt-2 text-xs text-gray-500">
                        <span>RX: {((networkInterface.rx_bytes || 0) / 1024 / 1024).toFixed(1)} MB</span>
                        <span className="mx-2">•</span>
                        <span>TX: {((networkInterface.tx_bytes || 0) / 1024 / 1024).toFixed(1)} MB</span>
                      </div>
                    </div>
                  ))
                ) : (
                  <div className="text-center py-8 text-gray-400">
                    <Wifi className="w-12 h-12 mx-auto mb-3 opacity-50" />
                    <p>No network interfaces available</p>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </div>
      ) : (
        <Card className="p-12 text-center">
          <Wifi className="w-16 h-16 text-gray-600 mx-auto mb-4" />
          <h3 className="text-xl font-bold mb-2">No Network Data</h3>
          <p className="text-gray-400">Network metrics will appear here once available.</p>
        </Card>
      )}

      {/* Network Details */}
      <Card className="p-6">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Activity className="w-5 h-5" />
            Network Details
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-4">
              <h4 className="font-semibold text-gray-300">Network Configuration</h4>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-400">Primary Interface:</span>
                  <span>eth0</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">IP Address:</span>
                  <span>192.168.1.100</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Gateway:</span>
                  <span>192.168.1.1</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">DNS:</span>
                  <span>8.8.8.8, 8.8.4.4</span>
                </div>
              </div>
            </div>
            <div className="space-y-4">
              <h4 className="font-semibold text-gray-300">Connection Statistics</h4>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-400">Active Connections:</span>
                  <span>42</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Total Bandwidth:</span>
                  <span>1000 Mbps</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Packet Loss:</span>
                  <span className="text-green-400">0.1%</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Latency:</span>
                  <span className="text-green-400">2ms</span>
                </div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
