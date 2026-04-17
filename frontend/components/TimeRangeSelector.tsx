'use client';

import { Clock, Calendar, Lock } from 'lucide-react';

interface TimeRangeSelectorProps {
  timeRange: '1h' | '6h' | '24h' | '7d' | '30d';
  onTimeRangeChange: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
  retentionDays?: number;
}

const timeRanges = [
  { value: '1h' as const, label: '1 час', icon: Clock, days: 0 },
  { value: '6h' as const, label: '6 часов', icon: Clock, days: 0 },
  { value: '24h' as const, label: '24 часа', icon: Clock, days: 1 },
  { value: '7d' as const, label: '7 дней', icon: Calendar, days: 7 },
  { value: '30d' as const, label: '30 дней', icon: Calendar, days: 30 },
];

export default function TimeRangeSelector({
  timeRange,
  onTimeRangeChange,
  retentionDays,
}: TimeRangeSelectorProps) {
  return (
    <div className='flex items-center gap-1 bg-gray-800/50 rounded-md p-0.5'>
      {timeRanges.map(({ value, label, icon: Icon, days }) => {
        const isLocked = retentionDays !== undefined && days > retentionDays;

        return (
          <button
            key={value}
            onClick={() => !isLocked && onTimeRangeChange(value)}
            disabled={isLocked}
            className={`
              flex items-center gap-1 px-2 py-1 rounded text-xs font-medium transition-all relative
              ${
                timeRange === value && !isLocked
                  ? 'bg-blue-600 text-white shadow-lg'
                  : isLocked
                  ? 'text-gray-500 cursor-not-allowed'
                  : 'text-gray-400 hover:text-white hover:bg-gray-700/50'
              }
            `}
            title={isLocked ? `Available on plans with ${days}+ days retention` : undefined}
          >
            {isLocked ? <Lock className='w-3 h-3' /> : <Icon className='w-3 h-3' />}
            <span>{label}</span>
          </button>
        );
      })}
    </div>
  );
}
