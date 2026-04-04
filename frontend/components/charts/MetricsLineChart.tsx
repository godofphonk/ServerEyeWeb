'use client';

import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import { useMemo } from 'react';
import { MetricsDataPoint } from '@/types';
import { formatTimeByRange, getTickCountByRange } from '@/utils/timeFormat';

interface MetricsLineChartProps {
  data: MetricsDataPoint[];
  metricType: 'cpu' | 'memory' | 'disk' | 'network' | 'temperature' | 'load' | 'cpu_frequency';
  title: string;
  color: string;
  unit?: string;
  timeRange?: string;
}

export default function MetricsLineChart({
  data,
  metricType,
  title,
  color,
  unit = '%',
  timeRange = '1h',
}: MetricsLineChartProps) {
  // Memoize chart data to prevent unnecessary recalculations
  const chartData = useMemo(() => {
    return data.map(point => {
      // Handle different field names (loadAverage vs load, cpu_frequency)
      let metricData;
      if (metricType === 'load') {
        metricData = point.loadAverage;
      } else if (metricType === 'cpu_frequency') {
        metricData = point.cpu_frequency;
      } else {
        metricData = point[metricType];
      }

      if (!metricData || typeof metricData.avg === 'undefined') {
        return {
          time: formatTimeByRange(point.timestamp, timeRange),
          avg: 0,
          max: 0,
          min: 0,
        };
      }

      return {
        time: formatTimeByRange(point.timestamp, timeRange),
        avg: metricData.avg,
        max: metricData.max,
        min: metricData.min,
      };
    });
  }, [data, metricType, timeRange]);

  // Memoize CustomTooltip to prevent recreation on every render
  const CustomTooltip = useMemo(() => {
    const CustomTooltipComponent = ({ active, payload }: any) => {
      if (active && payload && payload.length) {
        return (
          <div className='bg-gray-900 border border-white/20 rounded-lg p-3 shadow-xl'>
            <p className='text-sm text-gray-400 mb-2'>{payload[0]?.payload?.time}</p>
            <div className='space-y-1'>
              <p className='text-sm'>
                <span className='text-blue-400'>Avg:</span>{' '}
                <span className='font-semibold'>
                  {payload[0]?.value?.toFixed(1) || '0'}
                  {unit}
                </span>
              </p>
              <p className='text-sm'>
                <span className='text-green-400'>Max:</span>{' '}
                <span className='font-semibold'>
                  {payload[0]?.payload?.max?.toFixed(1) || '0'}
                  {unit}
                </span>
              </p>
              <p className='text-sm'>
                <span className='text-red-400'>Min:</span>{' '}
                <span className='font-semibold'>
                  {payload[0]?.payload?.min?.toFixed(1) || '0'}
                  {unit}
                </span>
              </p>
            </div>
          </div>
        );
      }
      return null;
    };

    CustomTooltipComponent.displayName = 'CustomTooltip';
    return CustomTooltipComponent;
  }, [unit]);

  // Early return if no data
  if (!data || data.length === 0) {
    return (
      <div className='w-full h-full flex items-center justify-center'>
        <p className='text-gray-400'>No data available</p>
      </div>
    );
  }

  return (
    <div className='w-full h-full min-h-[200px]'>
      <h3 className='text-lg font-semibold mb-4'>{title}</h3>
      <ResponsiveContainer width='100%' height={300}>
        <LineChart data={chartData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
          <CartesianGrid strokeDasharray='3 3' stroke='#ffffff10' />
          <XAxis
            dataKey='time'
            stroke='#9ca3af'
            style={{ fontSize: '12px' }}
            tickCount={getTickCountByRange(timeRange)}
          />
          <YAxis
            stroke='#9ca3af'
            style={{ fontSize: '12px' }}
            label={{ value: unit, angle: -90, position: 'insideLeft', style: { fill: '#9ca3af' } }}
          />
          <Tooltip content={<CustomTooltip />} />
          <Legend wrapperStyle={{ fontSize: '12px' }} iconType='line' />
          <Line
            type='monotone'
            dataKey='avg'
            stroke='#3b82f6'
            strokeWidth={2}
            dot={false}
            name='Average'
          />
          <Line
            type='monotone'
            dataKey='max'
            stroke='#10b981'
            strokeWidth={1}
            strokeDasharray='5 5'
            dot={false}
            name='Maximum'
          />
          <Line
            type='monotone'
            dataKey='min'
            stroke='#fbbf24'
            strokeWidth={1}
            strokeDasharray='5 5'
            dot={false}
            name='Minimum'
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
