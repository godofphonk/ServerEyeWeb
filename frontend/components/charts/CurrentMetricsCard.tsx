'use client';

import { LucideIcon } from 'lucide-react';
import { motion } from 'framer-motion';
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
  color,
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

  const colorClasses = {
    blue: 'bg-blue-500/20 text-blue-400 border-blue-500/30 shadow-blue-500/30',
    green: 'bg-green-500/20 text-green-400 border-green-500/30 shadow-green-500/30',
    red: 'bg-red-500/20 text-red-400 border-red-500/30 shadow-red-500/30',
    yellow: 'bg-yellow-500/20 text-yellow-400 border-yellow-500/30 shadow-yellow-500/30',
    purple: 'bg-purple-500/20 text-purple-400 border-purple-500/30 shadow-purple-500/30',
    orange: 'bg-orange-500/20 text-orange-400 border-orange-500/30 shadow-orange-500/30',
  };

  const colorClass = colorClasses[color as keyof typeof colorClasses] || colorClasses.blue;

  return (
    <Card className='hover:bg-white/5 transition-all duration-300'>
      <CardContent className='p-6'>
        <div className='flex items-center justify-between mb-4'>
          <motion.div
            whileHover={{ scale: 1.1, rotate: 5 }}
            whileTap={{ scale: 0.95 }}
            className={`w-12 h-12 rounded-xl flex items-center justify-center border ${colorClass} shadow-lg`}
          >
            <Icon className={`w-6 h-6`} />
          </motion.div>
          {trend && (
            <motion.div
              initial={{ opacity: 0, x: -10 }}
              animate={{ opacity: 1, x: 0 }}
              className={`text-sm font-semibold ${getTrendColor(trend)}`}
            >
              {getTrendSymbol(trend)} {trend}
            </motion.div>
          )}
        </div>
        <div className='space-y-1'>
          <p className='text-sm text-gray-400'>{label}</p>
          <motion.p
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            className='text-3xl font-bold'
          >
            {value.toFixed(1)}
            <span className='text-lg text-gray-400 ml-1'>{unit}</span>
          </motion.p>
        </div>
      </CardContent>
    </Card>
  );
}
