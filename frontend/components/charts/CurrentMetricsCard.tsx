"use client";

import { LucideIcon } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/Card';

interface CurrentMetricsCardProps {
  icon: LucideIcon;
  label: string;
  value: number;
  unit: string;
  trend?: string;
  color: string;
}

export default function CurrentMetricsCard({ 
  icon: Icon, 
  label, 
  value, 
  unit, 
  trend,
  color 
}: CurrentMetricsCardProps) {
  const getTrendColor = (trend?: string) => {
    if (!trend) return 'text-gray-400';
    const numericTrend = parseFloat(trend);
    if (numericTrend > 0) return 'text-red-400';
    if (numericTrend < 0) return 'text-green-400';
    return 'text-gray-400';
  };

  const getTrendSymbol = (trend?: string) => {
    if (!trend) return '';
    const numericTrend = parseFloat(trend);
    if (numericTrend > 0) return '↑';
    if (numericTrend < 0) return '↓';
    return '→';
  };

  return (
    <Card className="hover:bg-white/5 transition-all duration-300">
      <CardContent className="p-6">
        <div className="flex items-center justify-between mb-4">
          <div className={`w-12 h-12 rounded-xl bg-${color}-500/20 flex items-center justify-center`}>
            <Icon className={`w-6 h-6 text-${color}-400`} />
          </div>
          {trend && (
            <div className={`text-sm font-semibold ${getTrendColor(trend)}`}>
              {getTrendSymbol(trend)} {trend}
            </div>
          )}
        </div>
        <div className="space-y-1">
          <p className="text-sm text-gray-400">{label}</p>
          <p className="text-3xl font-bold">
            {value.toFixed(1)}
            <span className="text-lg text-gray-400 ml-1">{unit}</span>
          </p>
        </div>
      </CardContent>
    </Card>
  );
}
