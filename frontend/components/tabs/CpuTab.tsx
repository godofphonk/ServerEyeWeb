"use client";

import { Cpu, Activity, Thermometer, Zap, Gauge } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import CurrentMetricsCard from '@/components/charts/CurrentMetricsCard';
import MetricsLineChart from '@/components/charts/MetricsLineChart';
import { DashboardMetrics, MetricsResponse } from '@/types';

interface CpuTabProps {
  dashboardMetrics: DashboardMetrics | null;
  historicalMetrics: MetricsResponse | null;
}

export default function CpuTab({ dashboardMetrics, historicalMetrics }: CpuTabProps) {
  return (
    <div className="space-y-6">
      {/* CPU Overview Cards */}
      <div>
        <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
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
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <Card className="p-6">
            <div className="h-80">
              <MetricsLineChart
                data={historicalMetrics.data}
                metricType="cpu"
                title="CPU Usage"
                color="#3b82f6"
                unit="%"
              />
            </div>
          </Card>

          <Card className="p-6">
            <div className="h-80">
              <MetricsLineChart
                data={historicalMetrics.data}
                metricType="load"
                title="CPU Load"
                color="#f59e0b"
              />
            </div>
          </Card>
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
                  <span>N/A</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Cores:</span>
                  <span>N/A</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Base Clock:</span>
                  <span>N/A</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Max Boost:</span>
                  <span>N/A</span>
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
