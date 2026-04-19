'use client';

import { Monitor, Thermometer, Clock, Cpu } from 'lucide-react';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import CurrentMetricsCard from '@/components/charts/CurrentMetricsCard';
import { DashboardMetrics, MetricsResponse, ServerStaticInfo } from '@/types';

interface SystemTabProps {
  dashboardMetrics: DashboardMetrics | null;
  historicalMetrics?: MetricsResponse | null;
  server: any; // eslint-disable-line @typescript-eslint/no-explicit-any
  staticInfo?: ServerStaticInfo | null;
}

export default function SystemTab({
  dashboardMetrics,
  historicalMetrics: _historicalMetrics,
  server,
  staticInfo,
}: SystemTabProps) {
  // Use uptime from dashboardMetrics (calculated from uptime_seconds from API)
  const uptimeHours = dashboardMetrics?.uptime || 0;
  const uptimeNumeric = uptimeHours;
  const uptimeUnit = uptimeHours >= 24 ? 'days' : 'hours';
  const uptimeDisplay = uptimeHours >= 24
    ? `${Math.floor(uptimeHours / 24)}d ${Math.floor(uptimeHours % 24)}h`
    : `${Math.floor(uptimeHours)}h`;
  const _processes = 'N/A'; // Not available in current API

  // GPU temperature from historicalMetrics temperatureDetails
  const gpuTemp =
    _historicalMetrics?.temperatureDetails?.gpu_temperature ||
    dashboardMetrics?.current?.gpu_temperature ||
    0;

  return (
    <div className='space-y-6'>
      {/* System Overview Cards */}
      <div>
        <h3 className='text-lg font-semibold mb-4 flex items-center gap-2'>
          <Monitor className='w-5 h-5 text-cyan-400' />
          System Overview
        </h3>
        <div className='grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4'>
          <CurrentMetricsCard
            icon={Thermometer}
            label='GPU Temperature'
            value={gpuTemp}
            unit='°C'
            trend='N/A'
            color='orange'
          />
          <CurrentMetricsCard
            icon={Clock}
            label='Uptime'
            value={uptimeNumeric}
            unit={uptimeUnit}
            color='blue'
          />
          <CurrentMetricsCard
            icon={Cpu}
            label='Processes'
            value={0}
            unit='running'
            trend='N/A'
            color='green'
          />
          <CurrentMetricsCard
            icon={Monitor}
            label='System Load'
            value={dashboardMetrics?.current?.load || 0}
            unit='avg'
            trend='N/A'
            color='purple'
          />
        </div>
      </div>

      {/* System Information */}
      <div className='grid grid-cols-1 lg:grid-cols-2 gap-6'>
        <Card className='p-6'>
          <CardHeader>
            <CardTitle className='flex items-center gap-2'>
              <Monitor className='w-5 h-5' />
              System Information
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className='space-y-4'>
              <div className='grid grid-cols-2 gap-4 text-sm'>
                <div>
                  <span className='text-gray-400'>Hostname:</span>
                  <p className='font-medium'>
                    {staticInfo?.hostname || server?.hostname || 'Unknown'}
                  </p>
                </div>
                <div>
                  <span className='text-gray-400'>Operating System:</span>
                  <p className='font-medium'>
                    {staticInfo?.operating_system || server?.operatingSystem || 'Linux'}
                  </p>
                </div>
                <div>
                  <span className='text-gray-400'>Kernel Version:</span>
                  <p className='font-medium'>{staticInfo?.kernel || 'N/A'}</p>
                </div>
                <div>
                  <span className='text-gray-400'>Architecture:</span>
                  <p className='font-medium'>{staticInfo?.architecture || 'N/A'}</p>
                </div>
                <div>
                  <span className='text-gray-400'>Uptime:</span>
                  <p className='font-medium'>{uptimeDisplay}</p>
                </div>
                <div>
                  <span className='text-gray-400'>Last Boot:</span>
                  <p className='font-medium'>
                    {server?.lastSeen ? new Date(server.lastSeen).toLocaleString() : 'N/A'}
                  </p>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card className='p-6'>
          <CardHeader>
            <CardTitle className='flex items-center gap-2'>
              <Thermometer className='w-5 h-5' />
              Temperature Sensors
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className='space-y-4'>
              <div className='p-3 bg-gray-800 rounded-lg'>
                <div className='flex justify-between items-center'>
                  <span className='font-medium'>CPU Temperature</span>
                  <span className='text-lg font-bold text-red-400'>
                    {dashboardMetrics?.current.temperature?.toFixed(1) || 'N/A'}°C
                  </span>
                </div>
                <div className='text-xs text-gray-500 mt-1'>Real-time CPU temperature</div>
              </div>
              <div className='p-3 bg-gray-800 rounded-lg'>
                <div className='flex justify-between items-center'>
                  <span className='font-medium'>GPU Temperature</span>
                  <span className='text-lg font-bold text-orange-400'>
                    {dashboardMetrics?.current?.gpu_temperature?.toFixed(1) || 'N/A'}°C
                  </span>
                </div>
                <div className='text-xs text-gray-500 mt-1'>Real-time GPU temperature</div>
              </div>
              <div className='p-3 bg-gray-800 rounded-lg'>
                <div className='flex justify-between items-center'>
                  <span className='font-medium'>System Temperature</span>
                  <span className='text-lg font-bold text-blue-400'>N/A</span>
                </div>
                <div className='text-xs text-gray-500 mt-1'>Not available in current API</div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Process Information */}
      <Card className='p-6'>
        <CardHeader>
          <CardTitle className='flex items-center gap-2'>
            <Cpu className='w-5 h-5' />
            Process Information
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className='grid grid-cols-1 md:grid-cols-3 gap-6'>
            <div className='space-y-4'>
              <h4 className='font-semibold text-gray-300'>Process Summary</h4>
              <div className='space-y-2 text-sm'>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Total Processes:</span>
                  <span>N/A</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Running:</span>
                  <span className='text-green-400'>N/A</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Sleeping:</span>
                  <span className='text-blue-400'>N/A</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Stopped:</span>
                  <span className='text-gray-400'>N/A</span>
                </div>
              </div>
            </div>
            <div className='space-y-4'>
              <h4 className='font-semibold text-gray-300'>Top Processes</h4>
              <div className='space-y-2 text-sm'>
                <div className='flex justify-between'>
                  <span>N/A</span>
                  <span className='text-gray-400'>Not available</span>
                </div>
                <div className='flex justify-between'>
                  <span>N/A</span>
                  <span className='text-gray-400'>Not available</span>
                </div>
                <div className='flex justify-between'>
                  <span>N/A</span>
                  <span className='text-gray-400'>Not available</span>
                </div>
                <div className='flex justify-between'>
                  <span>N/A</span>
                  <span className='text-gray-400'>Not available</span>
                </div>
              </div>
            </div>
            <div className='space-y-4'>
              <h4 className='font-semibold text-gray-300'>Memory Usage</h4>
              <div className='space-y-2 text-sm'>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Total Process Memory:</span>
                  <span>N/A</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Highest Usage:</span>
                  <span>N/A</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Average per Process:</span>
                  <span>N/A</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Memory Leaks:</span>
                  <span className='text-green-400'>N/A</span>
                </div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
