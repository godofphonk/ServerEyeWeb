'use client';

import { useState, useEffect } from 'react';
import { Database, HardDrive, Activity, TrendingUp } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import CurrentMetricsCard from '@/components/charts/CurrentMetricsCard';
import MetricsLineChart from '@/components/charts/MetricsLineChart';
import TimeRangeSelector from '@/components/TimeRangeSelector';
import { DashboardMetrics, MetricsResponse } from '@/types';

interface StorageTabProps {
  dashboardMetrics: DashboardMetrics | null;
  historicalMetrics: MetricsResponse | null;
  staticInfo?: any;
  loadHistoricalMetrics?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => Promise<MetricsResponse>;
}

export default function StorageTab({
  dashboardMetrics,
  historicalMetrics,
  staticInfo,
  loadHistoricalMetrics,
}: StorageTabProps) {
  // Independent time range state for storage chart
  const [storageTimeRange, setStorageTimeRange] = useState<'1h' | '6h' | '24h' | '7d' | '30d'>('1h');
  
  // State for independently loaded metrics
  const [storageMetrics, setStorageMetrics] = useState<MetricsResponse | null>(null);

  // Load data when time range changes
  useEffect(() => {
    if (loadHistoricalMetrics) {
      console.log('[StorageTab] Loading Storage data for range:', storageTimeRange);
      loadHistoricalMetrics(storageTimeRange)
        .then(data => setStorageMetrics(data))
        .catch(error => console.error('[StorageTab] Failed to load Storage data:', error));
    }
  }, [storageTimeRange, loadHistoricalMetrics]);
  return (
    <div className='space-y-6'>
      {/* Storage Overview Cards */}
      <div>
        <h3 className='text-lg font-semibold mb-4 flex items-center gap-2'>
          <Database className='w-5 h-5 text-pink-400' />
          Storage Overview
        </h3>
        <div className='grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4'>
          {dashboardMetrics?.current && (
            <>
              <CurrentMetricsCard
                icon={HardDrive}
                label='Disk Usage'
                value={dashboardMetrics.current.disk}
                unit='%'
                trend={dashboardMetrics.trends?.disk?.toString()}
                color='pink'
              />
              <CurrentMetricsCard
                icon={Database}
                label='Total Space'
                value={staticInfo?.disk_info?.total_gb || 1000}
                unit='GB'
                color='blue'
              />
              <CurrentMetricsCard
                icon={Activity}
                label='Read Speed'
                value={dashboardMetrics.current.diskReadSpeed || 0}
                unit='MB/s'
                trend="-"
                color='green'
              />
              <CurrentMetricsCard
                icon={TrendingUp}
                label='Write Speed'
                value={dashboardMetrics.current.diskWriteSpeed || 0}
                unit='MB/s'
                trend="-"
                color='orange'
              />
            </>
          )}
        </div>
      </div>

      {/* Storage Charts */}
      {historicalMetrics?.data && historicalMetrics.data.length > 0 ? (
        <div className='grid grid-cols-1 lg:grid-cols-2 gap-6'>
          <Card className='p-6'>
            <div className='flex justify-between items-center mb-4'>
              <h4 className='text-sm font-medium text-gray-400'>Disk Usage</h4>
              <TimeRangeSelector 
                timeRange={storageTimeRange} 
                onTimeRangeChange={setStorageTimeRange} 
              />
            </div>
            <div className='h-80'>
              <MetricsLineChart
                data={storageMetrics?.data || historicalMetrics?.data}
                metricType='disk'
                title=''
                color='#ec4899'
                unit='%'
                timeRange={storageTimeRange}
              />
            </div>
          </Card>

          <Card className='p-6'>
            <CardHeader>
              <CardTitle>Storage Volumes</CardTitle>
            </CardHeader>
            <CardContent>
              <div className='space-y-4'>
                <div>
                  <div className='flex justify-between text-sm mb-2'>
                    <span className='text-gray-400'>Root (/)</span>
                    <span>{dashboardMetrics?.current.disk?.toFixed(1) || 0}%</span>
                  </div>
                  <div className='w-full bg-gray-700 rounded-full h-2'>
                    <div
                      className='bg-pink-500 h-2 rounded-full'
                      style={{ width: `${dashboardMetrics?.current.disk || 0}%` }}
                    />
                  </div>
                  <div className='text-xs text-gray-500 mt-1'>
                    {((1000 * (dashboardMetrics?.current.disk || 0)) / 100).toFixed(0)} GB used of
                    1000 GB
                  </div>
                </div>
                <div>
                  <div className='flex justify-between text-sm mb-2'>
                    <span className='text-gray-400'>Home (/home)</span>
                    <span>-</span>
                  </div>
                  <div className='w-full bg-gray-700 rounded-full h-2'>
                    <div className='bg-blue-500 h-2 rounded-full' style={{ width: '0%' }} />
                  </div>
                  <div className='text-xs text-gray-500 mt-1'>- GB used of - GB</div>
                </div>
                <div>
                  <div className='flex justify-between text-sm mb-2'>
                    <span className='text-gray-400'>Data (/data)</span>
                    <span>-</span>
                  </div>
                  <div className='w-full bg-gray-700 rounded-full h-2'>
                    <div className='bg-orange-500 h-2 rounded-full' style={{ width: '0%' }} />
                  </div>
                  <div className='text-xs text-gray-500 mt-1'>- TB used of - TB</div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      ) : (
        <Card className='p-12 text-center'>
          <Database className='w-16 h-16 text-gray-600 mx-auto mb-4' />
          <h3 className='text-xl font-bold mb-2'>No Storage Data</h3>
          <p className='text-gray-400'>Storage metrics will appear here once available.</p>
        </Card>
      )}

      {/* Storage Details */}
      <Card className='p-6'>
        <CardHeader>
          <CardTitle className='flex items-center gap-2'>
            <HardDrive className='w-5 h-5' />
            Storage Details
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className='grid grid-cols-1 md:grid-cols-2 gap-6'>
            <div className='space-y-4'>
              <h4 className='font-semibold text-gray-300'>Disk Information</h4>
              <div className='space-y-2 text-sm'>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Primary Disk:</span>
                  <span>-</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Capacity:</span>
                  <span>-</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>File System:</span>
                  <span>-</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Mount Point:</span>
                  <span>/</span>
                </div>
              </div>
            </div>
            <div className='space-y-4'>
              <h4 className='font-semibold text-gray-300'>I/O Performance</h4>
              <div className='space-y-2 text-sm'>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Read Speed:</span>
                  <span>{(dashboardMetrics?.current?.diskReadSpeed || 0).toFixed(1)} MB/s</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Write Speed:</span>
                  <span>{(dashboardMetrics?.current?.diskWriteSpeed || 0).toFixed(1)} MB/s</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>IOPS:</span>
                  <span className='text-green-400'>-</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Latency:</span>
                  <span className='text-green-400'>-</span>
                </div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
