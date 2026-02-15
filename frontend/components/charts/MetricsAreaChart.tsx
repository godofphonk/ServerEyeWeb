"use client";

import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { MetricsDataPoint } from '@/types';

interface MetricsAreaChartProps {
  data: MetricsDataPoint[];
  metricType: 'cpu' | 'memory' | 'disk' | 'network' | 'temperature' | 'load';
  title: string;
  color: string;
  unit?: string;
}

export default function MetricsAreaChart({ data, metricType, title, color, unit = '%' }: MetricsAreaChartProps) {
  const chartData = data.map(point => ({
    time: new Date(point.timestamp).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' }),
    value: point[metricType].avg,
  }));

  const CustomTooltip = ({ active, payload }: any) => {
    if (active && payload && payload.length) {
      return (
        <div className="bg-gray-900 border border-white/20 rounded-lg p-3 shadow-xl">
          <p className="text-sm text-gray-400 mb-1">{payload[0].payload.time}</p>
          <p className="text-sm font-semibold">
            {payload[0].value.toFixed(1)}{unit}
          </p>
        </div>
      );
    }
    return null;
  };

  return (
    <div className="w-full h-full">
      <h3 className="text-lg font-semibold mb-4">{title}</h3>
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={chartData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
          <defs>
            <linearGradient id={`gradient-${metricType}`} x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%" stopColor={color} stopOpacity={0.8}/>
              <stop offset="95%" stopColor={color} stopOpacity={0.1}/>
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="3 3" stroke="#ffffff10" />
          <XAxis 
            dataKey="time" 
            stroke="#9ca3af"
            style={{ fontSize: '12px' }}
          />
          <YAxis 
            stroke="#9ca3af"
            style={{ fontSize: '12px' }}
            label={{ value: unit, angle: -90, position: 'insideLeft', style: { fill: '#9ca3af' } }}
          />
          <Tooltip content={<CustomTooltip />} />
          <Area 
            type="monotone" 
            dataKey="value" 
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
