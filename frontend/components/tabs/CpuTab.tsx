"use client";

import { Cpu, Activity, Thermometer, Zap, Gauge } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import CurrentMetricsCard from '@/components/charts/CurrentMetricsCard';
import MetricsLineChart from '@/components/charts/MetricsLineChart';
import TimeRangeSelector from '@/components/TimeRangeSelector';
import { DashboardMetrics, MetricsResponse, ServerStaticInfo } from '@/types';

interface CpuTabProps {
  dashboardMetrics: DashboardMetrics | null;
  historicalMetrics: MetricsResponse | null;
  cpuUsageHistoricalMetrics: MetricsResponse | null;
  cpuLoadHistoricalMetrics: MetricsResponse | null;
  cpuTemperatureHistoricalMetrics: MetricsResponse | null;
  staticInfo: ServerStaticInfo | null;
  cpuUsageTimeRange?: '1h' | '6h' | '24h' | '7d' | '30d';
  cpuLoadTimeRange?: '1h' | '6h' | '24h' | '7d' | '30d';
  cpuTemperatureTimeRange?: '1h' | '6h' | '24h' | '7d' | '30d';
  onCpuUsageTimeRangeChange?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
  onCpuLoadTimeRangeChange?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
  onCpuTemperatureTimeRangeChange?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
}

export default function CpuTab({ 
  dashboardMetrics, 
  historicalMetrics, 
  cpuUsageHistoricalMetrics, 
  cpuLoadHistoricalMetrics, 
  cpuTemperatureHistoricalMetrics,
  staticInfo,
  cpuUsageTimeRange = '1h', 
  cpuLoadTimeRange = '1h', 
  cpuTemperatureTimeRange = '1h',
  onCpuUsageTimeRangeChange, 
  onCpuLoadTimeRangeChange,
  onCpuTemperatureTimeRangeChange 
}: CpuTabProps) {
  return (
    <div className="space-y-6">
      {/* CPU Overview Cards */}
      <div>
        <h3 className="text-lg font-semibold flex items-center gap-2 mb-4">
          <Cpu className="w-5 h-5 text-blue-400" />
          CPU Overview
        </h3>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {dashboardMetrics?.current && (
            <>
              <CurrentMetricsCard
                icon={Cpu}
                label="CPU Usage"
                value={dashboardMetrics.current.cpu}
                unit="%"
                trend={dashboardMetrics.trends?.cpu?.toString()}
                color="blue"
              />
              <CurrentMetricsCard
                icon={Activity}
                label="CPU Load"
                value={dashboardMetrics.current.load}
                unit=""
                trend={dashboardMetrics.trends?.load?.toString()}
                color="yellow"
              />
              <CurrentMetricsCard
                icon={Thermometer}
                label="CPU Temperature"
                value={dashboardMetrics.current.temperature}
                unit="°C"
                trend={dashboardMetrics.trends?.temperature?.toString()}
                color="red"
              />
              <CurrentMetricsCard
                icon={Zap}
                label="CPU Cores"
                value={8}
                unit="cores"
                color="purple"
              />
            </>
          )}
        </div>
      </div>

      {/* CPU Charts */}
      {historicalMetrics?.data && historicalMetrics.data.length > 0 ? (
        <div>
          <h3 className="text-lg font-semibold flex items-center gap-2 mb-4">
            <Activity className="w-5 h-5 text-green-400" />
            CPU Performance
          </h3>
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <Card className="p-6">
            <div className="flex justify-between items-center mb-4">
              <h4 className="text-sm font-medium text-gray-400">CPU Usage</h4>
              {onCpuUsageTimeRangeChange && (
                <TimeRangeSelector 
                  timeRange={cpuUsageTimeRange} 
                  onTimeRangeChange={onCpuUsageTimeRangeChange} 
                />
              )}
            </div>
            <div className="h-80">
              <MetricsLineChart
                data={cpuUsageHistoricalMetrics?.data || historicalMetrics?.data}
                metricType="cpu"
                title=""
                color="#3b82f6"
                unit="%"
                timeRange={cpuUsageTimeRange}
              />
            </div>
          </Card>

          <Card className="p-6">
            <div className="flex justify-between items-center mb-4">
              <h4 className="text-sm font-medium text-gray-400">CPU Load</h4>
              {onCpuLoadTimeRangeChange && (
                <TimeRangeSelector 
                  timeRange={cpuLoadTimeRange} 
                  onTimeRangeChange={onCpuLoadTimeRangeChange} 
                />
              )}
            </div>
            <div className="h-80">
              <MetricsLineChart
                data={cpuLoadHistoricalMetrics?.data || historicalMetrics?.data}
                metricType="load"
                title=""
                color="#f59e0b"
                timeRange={cpuLoadTimeRange}
              />
            </div>
          </Card>

          <Card className="p-6">
            <div className="flex justify-between items-center mb-4">
              <h4 className="text-sm font-medium text-gray-400">CPU Temperature</h4>
              {onCpuTemperatureTimeRangeChange && (
                <TimeRangeSelector 
                  timeRange={cpuTemperatureTimeRange} 
                  onTimeRangeChange={onCpuTemperatureTimeRangeChange} 
                />
              )}
            </div>
            <div className="h-80">
              <MetricsLineChart
                data={cpuTemperatureHistoricalMetrics?.data || historicalMetrics?.data}
                metricType="temperature"
                title=""
                color="#ef4444"
                unit="°C"
                timeRange={cpuTemperatureTimeRange}
              />
            </div>
          </Card>
          </div>
        </div>
      ) : (
        <Card className="p-12 text-center">
          <Cpu className="w-16 h-16 text-gray-600 mx-auto mb-4" />
          <h3 className="text-xl font-bold mb-2">No CPU Data</h3>
          <p className="text-gray-400">CPU metrics will appear here once available.</p>
        </Card>
      )}

      {/* CPU Details */}
      <Card className="p-6">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Cpu className="w-5 h-5" />
            CPU Details
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-4">
              <h4 className="font-semibold text-gray-300">Processor Information</h4>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-400">Model:</span>
                  <span>{staticInfo?.cpu_info?.model || 'N/A'}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Cores:</span>
                  <span>{staticInfo?.cpu_info?.cores || 'N/A'}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Frequency:</span>
                  <span>{staticInfo?.cpu_info?.frequency_mhz ? `${staticInfo.cpu_info.frequency_mhz} MHz` : 'N/A'}</span>
                </div>
              </div>
            </div>
            <div className="space-y-4">
              <h4 className="font-semibold text-gray-300">Performance Metrics</h4>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-400">Average Load:</span>
                  <span>{dashboardMetrics?.current.load?.toFixed(2) || 'N/A'}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Peak Temperature:</span>
                  <span>{dashboardMetrics?.current.temperature?.toFixed(1) || 'N/A'}°C</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Usage Trend:</span>
                  <span className={dashboardMetrics?.trends?.cpu ? 'text-green-400' : 'text-gray-400'}>
                    {dashboardMetrics?.trends?.cpu ? 'Stable' : 'N/A'}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Efficiency:</span>
                  <span className="text-green-400">Good</span>
                </div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
