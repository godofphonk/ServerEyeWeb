'use client';

import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import { useMemo } from 'react';
import { MetricsDataPoint, ChartTooltipPayload } from '@/types';
import { formatTimeByRange, getTickCountByRange } from '@/utils/timeFormat';

interface MetricsAreaChartProps {
  data: MetricsDataPoint[];
  metricType: 'cpu' | 'memory' | 'disk' | 'network' | 'temperature' | 'load' | 'cpu_frequency';
  title: string;
  color: string;
  unit?: string;
  timeRange?: string;
}

export default function MetricsAreaChart({
  data,
  metricType,
  title,
  color,
  unit = '%',
  timeRange = '1h',
}: MetricsAreaChartProps) {
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
          value: 0,
        };
      }

      return {
        time: formatTimeByRange(point.timestamp, timeRange),
        value: metricData.avg,
      };
    });
  }, [data, metricType, timeRange]);

  // Memoize CustomTooltip to prevent recreation on every render
  const CustomTooltip = useMemo(() => {
    const CustomTooltipComponent = ({ active, payload }: { active?: boolean; payload?: ChartTooltipPayload[] }) => {
       
      if (active && payload && payload.length) {
        return (
          <div className='bg-gray-900 border border-white/20 rounded-lg p-3 shadow-xl'>
            <p className='text-sm text-gray-400 mb-1'>{payload[0].payload.time}</p>
            <p className='text-sm font-semibold'>
              {payload[0].value.toFixed(1)}
              {unit}
            </p>
          </div>
        );
      }
      return null;
    };

    CustomTooltipComponent.displayName = 'CustomTooltip';
    return CustomTooltipComponent;
  }, [unit]);

  return (
    <div className='w-full h-full min-h-[200px]'>
      <h3 className='text-lg font-semibold mb-4'>{title}</h3>
      <ResponsiveContainer width='100%' height={300}>
        <AreaChart data={chartData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
          <defs>
            <linearGradient id={`gradient-${metricType}`} x1='0' y1='0' x2='0' y2='1'>
              <stop offset='5%' stopColor={color} stopOpacity={0.8} />
              <stop offset='95%' stopColor={color} stopOpacity={0.1} />
            </linearGradient>
          </defs>
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
          <Area
            type='monotone'
            dataKey='value'
            stroke={color}
            strokeWidth={2}
            fillOpacity={1}
            fill={`url(#gradient-${metricType})`}
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
}
