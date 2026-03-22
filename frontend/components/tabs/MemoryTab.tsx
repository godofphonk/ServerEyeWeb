'use client';

import { HardDrive, Database, Zap, TrendingUp } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import CurrentMetricsCard from '@/components/charts/CurrentMetricsCard';
import MetricsAreaChart from '@/components/charts/MetricsAreaChart';
import TimeRangeSelector from '@/components/TimeRangeSelector';
import { DashboardMetrics, MetricsResponse, ServerStaticInfo } from '@/types';

interface MemoryTabProps {
  dashboardMetrics: DashboardMetrics | null;
  historicalMetrics: MetricsResponse | null;
  staticInfo: ServerStaticInfo | null;
  timeRange?: '1h' | '6h' | '24h' | '7d' | '30d';
  onTimeRangeChange?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
}

export default function MemoryTab({
  dashboardMetrics,
  historicalMetrics,
  staticInfo,
  timeRange = '1h',
  onTimeRangeChange,
}: MemoryTabProps) {
  return (
    <div className='space-y-6'>
      {/* Memory Overview Cards */}
      <div>
        <h3 className='text-lg font-semibold mb-4 flex items-center gap-2'>
          <HardDrive className='w-5 h-5 text-purple-400' />
          Memory Overview
        </h3>
        <div className='grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4'>
          {dashboardMetrics?.current && (
            <>
              <CurrentMetricsCard
                icon={HardDrive}
                label='RAM Usage'
                value={dashboardMetrics.current.memory}
                unit='%'
                trend={dashboardMetrics.trends?.memory?.toString()}
                color='purple'
              />
              <CurrentMetricsCard
                icon={Database}
                label='Total'
                value={staticInfo?.memory_info?.total_gb || 32}
                unit='GB'
                trend="-"
                color='blue'
              />
              <CurrentMetricsCard
                icon={Zap}
                label='Cache'
                value={12.9} // TODO: Get from Go API memory_details.cached_gb
                unit='GB'
                trend="-"
                color='green'
              />
              <CurrentMetricsCard
                icon={TrendingUp}
                label='Available'
                value={14.7} // TODO: Get from Go API memory_details.available_gb
                unit='GB'
                trend="-"
                color='cyan'
              />
            </>
          )}
        </div>
      </div>

      {/* Memory Charts */}
      {historicalMetrics?.data && historicalMetrics.data.length > 0 ? (
        <div className='grid grid-cols-1 lg:grid-cols-2 gap-6'>
          <Card className='p-6'>
            <div className='flex justify-between items-center mb-4'>
              <h4 className='text-sm font-medium text-gray-400'>Memory Usage</h4>
              {onTimeRangeChange && (
                <TimeRangeSelector timeRange={timeRange} onTimeRangeChange={onTimeRangeChange} />
              )}
            </div>
            <div className='h-80'>
              <MetricsAreaChart
                data={historicalMetrics.data}
                metricType='memory'
                title=''
                color='#a855f7'
                unit='%'
                timeRange={timeRange}
              />
            </div>
          </Card>

          <Card className='p-6'>
            <CardHeader>
              <CardTitle>Memory Distribution</CardTitle>
            </CardHeader>
            <CardContent>
              <div className='space-y-4'>
                <div>
                  <div className='flex justify-between text-sm mb-2'>
                    <span className='text-gray-400'>Used</span>
                    <span>{dashboardMetrics?.current.memory?.toFixed(1) || 0}%</span>
                  </div>
                  <div className='w-full bg-gray-700 rounded-full h-2'>
                    <div
                      className='bg-purple-500 h-2 rounded-full'
                      style={{ width: `${dashboardMetrics?.current.memory || 0}%` }}
                    />
                  </div>
                </div>
                <div>
                  <div className='flex justify-between text-sm mb-2'>
                    <span className='text-gray-400'>Cache</span>
                    <span>12.9 GB (40.3%)</span>
                  </div>
                  <div className='w-full bg-gray-700 rounded-full h-2'>
                    <div className='bg-blue-500 h-2 rounded-full' style={{ width: '40.3%' }} />
                  </div>
                </div>
                <div>
                  <div className='flex justify-between text-sm mb-2'>
                    <span className='text-gray-400'>Free</span>
                    <span>
                      {(16 - (16 * (dashboardMetrics?.current.memory || 0)) / 100).toFixed(1)} GB
                    </span>
                  </div>
                  <div className='w-full bg-gray-700 rounded-full h-2'>
                    <div
                      className='bg-green-500 h-2 rounded-full'
                      style={{ width: `${100 - (dashboardMetrics?.current.memory || 0)}%` }}
                    />
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      ) : (
        <Card className='p-12 text-center'>
          <HardDrive className='w-16 h-16 text-gray-600 mx-auto mb-4' />
          <h3 className='text-xl font-bold mb-2'>No Memory Data</h3>
          <p className='text-gray-400'>Memory metrics will appear here once available.</p>
        </Card>
      )}

      {/* Memory Details */}
      <Card className='p-6'>
        <CardHeader>
          <CardTitle className='flex items-center gap-2'>
            <Database className='w-5 h-5' />
            Memory Details
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className='grid grid-cols-1 md:grid-cols-2 gap-6'>
            <div className='space-y-4'>
              <h4 className='font-semibold text-gray-300'>System Memory</h4>
              <div className='space-y-2 text-sm'>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Total RAM:</span>
                  <span>{staticInfo?.memory_info?.total_gb || 'N/A'} GB</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Available:</span>
                  <span>
                    {staticInfo?.memory_info?.total_gb
                      ? (
                          staticInfo.memory_info.total_gb -
                          (staticInfo.memory_info.total_gb *
                            (dashboardMetrics?.current.memory || 0)) /
                            100
                        ).toFixed(1)
                      : 'N/A'}{' '}
                    GB
                  </span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Used:</span>
                  <span>
                    {staticInfo?.memory_info?.total_gb
                      ? (
                          (staticInfo.memory_info.total_gb *
                            (dashboardMetrics?.current.memory || 0)) /
                          100
                        ).toFixed(1)
                      : 'N/A'}{' '}
                    GB
                  </span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Type:</span>
                  <span>{staticInfo?.memory_info?.type || 'N/A'}</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Speed:</span>
                  <span>
                    {staticInfo?.memory_info?.speed_mhz
                      ? `${staticInfo.memory_info.speed_mhz} MHz`
                      : 'N/A'}
                  </span>
                </div>
              </div>
            </div>
            <div className='space-y-4'>
              <h4 className='font-semibold text-gray-300'>Performance Metrics</h4>
              <div className='space-y-2 text-sm'>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Memory Usage:</span>
                  <span>{dashboardMetrics?.current.memory?.toFixed(1) || 'N/A'}%</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Total Memory:</span>
                  <span>{staticInfo?.memory_info?.total_gb || 'N/A'} GB</span>
                </div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
