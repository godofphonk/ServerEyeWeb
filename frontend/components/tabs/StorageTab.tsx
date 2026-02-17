"use client";

import { Database, HardDrive, Activity, TrendingUp } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import CurrentMetricsCard from '@/components/charts/CurrentMetricsCard';
import MetricsLineChart from '@/components/charts/MetricsLineChart';
import { DashboardMetrics, MetricsResponse } from '@/types';

interface StorageTabProps {
  dashboardMetrics: DashboardMetrics | null;
  historicalMetrics: MetricsResponse | null;
}

export default function StorageTab({ dashboardMetrics, historicalMetrics }: StorageTabProps) {
  return (
    <div className="space-y-6">
      {/* Storage Overview Cards */}
      <div>
        <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
          <Database className="w-5 h-5 text-pink-400" />
          Storage Overview
        </h3>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {dashboardMetrics?.current && (
            <>
              <CurrentMetricsCard
                icon={HardDrive}
                label="Disk Usage"
                value={dashboardMetrics.current.disk}
                unit="%"
                trend={dashboardMetrics.trends?.disk?.toString()}
                color="pink"
              />
              <CurrentMetricsCard
                icon={Database}
                label="Total Space"
                value={1000}
                unit="GB"
                color="blue"
              />
              <CurrentMetricsCard
                icon={Activity}
                label="Read Speed"
                value={125.5}
                unit="MB/s"
                trend="5.2"
                color="green"
              />
              <CurrentMetricsCard
                icon={TrendingUp}
                label="Write Speed"
                value={89.3}
                unit="MB/s"
                trend="-2.1"
                color="orange"
              />
            </>
          )}
        </div>
      </div>

      {/* Storage Charts */}
      {historicalMetrics?.data && historicalMetrics.data.length > 0 ? (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <Card className="p-6">
            <div className="h-80">
              <MetricsLineChart
                data={historicalMetrics.data}
                metricType="disk"
                title="Disk Usage"
                color="#ec4899"
                unit="%"
              />
            </div>
          </Card>

          <Card className="p-6">
            <CardHeader>
              <CardTitle>Storage Volumes</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div>
                  <div className="flex justify-between text-sm mb-2">
                    <span className="text-gray-400">Root (/)</span>
                    <span>{dashboardMetrics?.current.disk?.toFixed(1) || 0}%</span>
                  </div>
                  <div className="w-full bg-gray-700 rounded-full h-2">
                    <div 
                      className="bg-pink-500 h-2 rounded-full" 
                      style={{ width: `${dashboardMetrics?.current.disk || 0}%` }}
                    />
                  </div>
                  <div className="text-xs text-gray-500 mt-1">
                    {(1000 * (dashboardMetrics?.current.disk || 0) / 100).toFixed(0)} GB used of 1000 GB
                  </div>
                </div>
                <div>
                  <div className="flex justify-between text-sm mb-2">
                    <span className="text-gray-400">Home (/home)</span>
                    <span>45.2%</span>
                  </div>
                  <div className="w-full bg-gray-700 rounded-full h-2">
                    <div className="bg-blue-500 h-2 rounded-full" style={{ width: '45.2%' }} />
                  </div>
                  <div className="text-xs text-gray-500 mt-1">226 GB used of 500 GB</div>
                </div>
                <div>
                  <div className="flex justify-between text-sm mb-2">
                    <span className="text-gray-400">Data (/data)</span>
                    <span>78.9%</span>
                  </div>
                  <div className="w-full bg-gray-700 rounded-full h-2">
                    <div className="bg-orange-500 h-2 rounded-full" style={{ width: '78.9%' }} />
                  </div>
                  <div className="text-xs text-gray-500 mt-1">1.5 TB used of 2 TB</div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      ) : (
        <Card className="p-12 text-center">
          <Database className="w-16 h-16 text-gray-600 mx-auto mb-4" />
          <h3 className="text-xl font-bold mb-2">No Storage Data</h3>
          <p className="text-gray-400">Storage metrics will appear here once available.</p>
        </Card>
      )}

      {/* Storage Details */}
      <Card className="p-6">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <HardDrive className="w-5 h-5" />
            Storage Details
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-4">
              <h4 className="font-semibold text-gray-300">Disk Information</h4>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-400">Primary Disk:</span>
                  <span>Samsung SSD 980 Pro</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Capacity:</span>
                  <span>1 TB NVMe</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">File System:</span>
                  <span>ext4</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Mount Point:</span>
                  <span>/</span>
                </div>
              </div>
            </div>
            <div className="space-y-4">
              <h4 className="font-semibold text-gray-300">I/O Performance</h4>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-400">Read Speed:</span>
                  <span>125.5 MB/s</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Write Speed:</span>
                  <span>89.3 MB/s</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">IOPS:</span>
                  <span className="text-green-400">85,420</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Latency:</span>
                  <span className="text-green-400">0.2ms</span>
                </div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
