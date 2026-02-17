"use client";

import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { MetricsDataPoint } from '@/types';

interface MetricsLineChartProps {
  data: MetricsDataPoint[];
  metricType: 'cpu' | 'memory' | 'disk' | 'network' | 'temperature' | 'load' | 'cpu_frequency';
  title: string;
  color: string;
  unit?: string;
}

export default function MetricsLineChart({ data, metricType, title, color, unit = '%' }: MetricsLineChartProps) {
  // Early return if no data
  if (!data || data.length === 0) {
    return (
      <div className="w-full h-full flex items-center justify-center">
        <p className="text-gray-400">No data available</p>
      </div>
    );
  }

  const chartData = data.map(point => {
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
      console.warn(`[MetricsLineChart] Missing data for metric ${metricType}:`, point);
      return {
        time: new Date(point.timestamp).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' }),
        avg: 0,
        max: 0,
        min: 0,
      };
    }
    
    return {
      time: new Date(point.timestamp).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' }),
      avg: metricData.avg,
      max: metricData.max,
      min: metricData.min,
    };
  });

  const CustomTooltip = ({ active, payload }: any) => {
    if (active && payload && payload.length) {
      return (
        <div className="bg-gray-900 border border-white/20 rounded-lg p-3 shadow-xl">
          <p className="text-sm text-gray-400 mb-2">{payload[0].payload.time}</p>
          <div className="space-y-1">
            <p className="text-sm">
              <span className="text-blue-400">Avg:</span>{' '}
              <span className="font-semibold">{payload[0].value.toFixed(1)}{unit}</span>
            </p>
            <p className="text-sm">
              <span className="text-green-400">Max:</span>{' '}
              <span className="font-semibold">{payload[1].value.toFixed(1)}{unit}</span>
            </p>
            <p className="text-sm">
              <span className="text-yellow-400">Min:</span>{' '}
              <span className="font-semibold">{payload[2].value.toFixed(1)}{unit}</span>
            </p>
          </div>
        </div>
      );
    }
    return null;
  };

  return (
    <div className="w-full h-full">
      <h3 className="text-lg font-semibold mb-4">{title}</h3>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={chartData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
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
          <Legend 
            wrapperStyle={{ fontSize: '12px' }}
            iconType="line"
          />
          <Line 
            type="monotone" 
            dataKey="avg" 
            stroke="#3b82f6" 
            strokeWidth={2}
            dot={false}
            name="Average"
          />
          <Line 
            type="monotone" 
            dataKey="max" 
            stroke="#10b981" 
            strokeWidth={1}
            strokeDasharray="5 5"
            dot={false}
            name="Maximum"
          />
          <Line 
            type="monotone" 
            dataKey="min" 
            stroke="#fbbf24" 
            strokeWidth={1}
            strokeDasharray="5 5"
            dot={false}
            name="Minimum"
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  );
}
