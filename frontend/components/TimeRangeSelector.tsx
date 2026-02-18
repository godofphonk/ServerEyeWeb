'use client';

import { Clock, Calendar } from 'lucide-react';

interface TimeRangeSelectorProps {
  timeRange: '1h' | '6h' | '24h' | '7d' | '30d';
  onTimeRangeChange: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
}

const timeRanges = [
  { value: '1h' as const, label: '1 час', icon: Clock },
  { value: '6h' as const, label: '6 часов', icon: Clock },
  { value: '24h' as const, label: '24 часа', icon: Clock },
  { value: '7d' as const, label: '7 дней', icon: Calendar },
  { value: '30d' as const, label: '30 дней', icon: Calendar },
];

export default function TimeRangeSelector({ timeRange, onTimeRangeChange }: TimeRangeSelectorProps) {
  return (
    <div className="flex items-center gap-1 bg-gray-800/50 rounded-md p-0.5">
      {timeRanges.map(({ value, label, icon: Icon }) => (
        <button
          key={value}
          onClick={() => onTimeRangeChange(value)}
          className={`
            flex items-center gap-1 px-2 py-1 rounded text-xs font-medium transition-all
            ${timeRange === value
              ? 'bg-blue-600 text-white shadow-lg'
              : 'text-gray-400 hover:text-white hover:bg-gray-700/50'
            }
          `}
        >
          <Icon className="w-3 h-3" />
          <span>{label}</span>
        </button>
      ))}
    </div>
  );
}
