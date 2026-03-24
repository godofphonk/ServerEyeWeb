'use client';

import { useState, useEffect, useMemo } from 'react';
import { Cpu, Activity, Thermometer, Zap, Gauge } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import CurrentMetricsCard from '@/components/charts/CurrentMetricsCard';
import MetricsLineChart from '@/components/charts/MetricsLineChart';
import TimeRangeSelector from '@/components/TimeRangeSelector';
import { DashboardMetrics, MetricsResponse, ServerStaticInfo } from '@/types';

interface CpuTabProps {
  dashboardMetrics: DashboardMetrics | null;
  historicalMetrics: MetricsResponse | null; // Unified metrics for all CPU charts
  staticInfo: ServerStaticInfo | null;
  loadHistoricalMetrics?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => Promise<MetricsResponse>;
}

export default function CpuTab({
  dashboardMetrics,
  historicalMetrics,
  staticInfo,
  loadHistoricalMetrics,
}: CpuTabProps) {
  // Independent time range states for each chart
  const [cpuUsageTimeRange, setCpuUsageTimeRange] = useState<'1h' | '6h' | '24h' | '7d' | '30d'>('1h');
  const [cpuLoadTimeRange, setCpuLoadTimeRange] = useState<'1h' | '6h' | '24h' | '7d' | '30d'>('1h');
  const [cpuTemperatureTimeRange, setCpuTemperatureTimeRange] = useState<'1h' | '6h' | '24h' | '7d' | '30d'>('1h');

  // State for independently loaded metrics
  const [cpuUsageMetrics, setCpuUsageMetrics] = useState<MetricsResponse | null>(null);
  const [cpuLoadMetrics, setCpuLoadMetrics] = useState<MetricsResponse | null>(null);
  const [cpuTemperatureMetrics, setCpuTemperatureMetrics] = useState<MetricsResponse | null>(null);

  // Memoize time ranges to prevent unnecessary effect triggers
  const timeRanges = useMemo(() => ({
    cpuUsage: cpuUsageTimeRange,
    cpuLoad: cpuLoadTimeRange,
    temperature: cpuTemperatureTimeRange,
  }), [cpuUsageTimeRange, cpuLoadTimeRange, cpuTemperatureTimeRange]);

  // Load all metrics in parallel when any time range changes
  useEffect(() => {
    if (!loadHistoricalMetrics) return;

    const loadAllMetrics = async () => {
      try {
        // Load all three metrics in parallel for optimal performance
        const [usageResult, loadResult, tempResult] = await Promise.allSettled([
          loadHistoricalMetrics(cpuUsageTimeRange),
          loadHistoricalMetrics(cpuLoadTimeRange),
          loadHistoricalMetrics(cpuTemperatureTimeRange),
        ]);

        // Handle results individually to prevent one failure from affecting others
        if (usageResult.status === 'fulfilled') {
          setCpuUsageMetrics(usageResult.value);
        } else {
        }

        if (loadResult.status === 'fulfilled') {
          setCpuLoadMetrics(loadResult.value);
        } else {
        }

        if (tempResult.status === 'fulfilled') {
          setCpuTemperatureMetrics(tempResult.value);
        } else {
        }
      } catch (error) {
      }
    };

    loadAllMetrics();
  }, [timeRanges, loadHistoricalMetrics]);
  return (
    <div className='space-y-6'>
      {/* CPU Overview Cards */}
      <div>
        <h3 className='text-lg font-semibold flex items-center gap-2 mb-4'>
          <Cpu className='w-5 h-5 text-blue-400' />
          CPU Overview
        </h3>
        <div className='grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4'>
          {dashboardMetrics?.current && (
            <>
              <CurrentMetricsCard
                icon={Cpu}
                label='CPU Usage'
                value={dashboardMetrics.current.cpu}
                unit='%'
                trend={dashboardMetrics.trends?.cpu?.toString()}
                color='blue'
              />
              <CurrentMetricsCard
                icon={Activity}
                label='CPU Load'
                value={dashboardMetrics.current.load}
                unit=''
                trend={dashboardMetrics.trends?.load?.toString()}
                color='yellow'
              />
              <CurrentMetricsCard
                icon={Thermometer}
                label='CPU Temperature'
                value={dashboardMetrics.current.temperature}
                unit='°C'
                trend={dashboardMetrics.trends?.temperature?.toString()}
                color='red'
              />
              <CurrentMetricsCard
                icon={Zap}
                label='CPU Cores'
                value={8}
                unit='cores'
                color='purple'
              />
            </>
          )}
        </div>
      </div>

      {/* CPU Charts */}
      {historicalMetrics?.data && historicalMetrics.data.length > 0 ? (
        <div>
          <h3 className='text-lg font-semibold flex items-center gap-2 mb-4'>
            <Activity className='w-5 h-5 text-green-400' />
            CPU Performance
          </h3>
          <div className='grid grid-cols-1 lg:grid-cols-2 gap-6'>
            <Card className='p-6'>
              <div className='flex justify-between items-center mb-4'>
                <h4 className='text-sm font-medium text-gray-400'>CPU Usage</h4>
                <TimeRangeSelector
                  timeRange={cpuUsageTimeRange}
                  onTimeRangeChange={setCpuUsageTimeRange}
                />
              </div>
              <div className='h-80'>
                <MetricsLineChart
                  data={cpuUsageMetrics?.data || historicalMetrics?.data}
                  metricType='cpu'
                  title=''
                  color='#3b82f6'
                  unit='%'
                  timeRange={cpuUsageTimeRange}
                />
              </div>
            </Card>

            <Card className='p-6'>
              <div className='flex justify-between items-center mb-4'>
                <h4 className='text-sm font-medium text-gray-400'>CPU Load</h4>
                <TimeRangeSelector
                  timeRange={cpuLoadTimeRange}
                  onTimeRangeChange={setCpuLoadTimeRange}
                />
              </div>
              <div className='h-80'>
                <MetricsLineChart
                  data={cpuLoadMetrics?.data || historicalMetrics?.data}
                  metricType='load'
                  title=''
                  color='#f59e0b'
                  timeRange={cpuLoadTimeRange}
                />
              </div>
            </Card>

            <Card className='p-6'>
              <div className='flex justify-between items-center mb-4'>
                <h4 className='text-sm font-medium text-gray-400'>CPU Temperature</h4>
                <TimeRangeSelector
                  timeRange={cpuTemperatureTimeRange}
                  onTimeRangeChange={setCpuTemperatureTimeRange}
                />
              </div>
              <div className='h-80'>
                <MetricsLineChart
                  data={cpuTemperatureMetrics?.data || historicalMetrics?.data}
                  metricType='temperature'
                  title=''
                  color='#ef4444'
                  unit='°C'
                  timeRange={cpuTemperatureTimeRange}
                />
              </div>
            </Card>
          </div>
        </div>
      ) : (
        <Card className='p-12 text-center'>
          <Cpu className='w-16 h-16 text-gray-600 mx-auto mb-4' />
          <h3 className='text-xl font-bold mb-2'>No CPU Data</h3>
          <p className='text-gray-400'>CPU metrics will appear here once available.</p>
        </Card>
      )}

      {/* CPU Details */}
      <Card className='p-6'>
        <CardHeader>
          <CardTitle className='flex items-center gap-2'>
            <Cpu className='w-5 h-5' />
            CPU Details
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className='grid grid-cols-1 md:grid-cols-2 gap-6'>
            <div className='space-y-4'>
              <h4 className='font-semibold text-gray-300'>Processor Information</h4>
              <div className='space-y-2 text-sm'>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Model:</span>
                  <span>{staticInfo?.cpu_info?.model || 'N/A'}</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Cores:</span>
                  <span>{staticInfo?.cpu_info?.cores || 'N/A'}</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Frequency:</span>
                  <span>
                    {staticInfo?.cpu_info?.frequency_mhz
                      ? `${staticInfo.cpu_info.frequency_mhz} MHz`
                      : 'N/A'}
                  </span>
                </div>
              </div>
            </div>
            <div className='space-y-4'>
              <h4 className='font-semibold text-gray-300'>Performance Metrics</h4>
              <div className='space-y-2 text-sm'>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Average Load:</span>
                  <span>{dashboardMetrics?.current.load?.toFixed(2) || 'N/A'}</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Peak Temperature:</span>
                  <span>{dashboardMetrics?.current.temperature?.toFixed(1) || 'N/A'}°C</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Usage Trend:</span>
                  <span
                    className={dashboardMetrics?.trends?.cpu ? 'text-green-400' : 'text-gray-400'}
                  >
                    {dashboardMetrics?.trends?.cpu ? 'Stable' : 'N/A'}
                  </span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Efficiency:</span>
                  <span className='text-green-400'>Good</span>
                </div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
