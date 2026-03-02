'use client';

import { useEffect, useState } from 'react';
import { Card } from '@/components/ui/Card';
import {
  AlertCircle,
  Info,
  AlertTriangle,
  XCircle,
  Search,
  RefreshCw,
  ExternalLink,
} from 'lucide-react';

interface LogEntry {
  timestamp: string;
  level: 'debug' | 'info' | 'warn' | 'error' | 'fatal';
  message: string;
  service: string;
  request_id?: string;
  user_id?: string;
  caller?: string;
}

const levelConfig = {
  debug: { color: 'text-gray-400', bg: 'bg-gray-500/10', icon: Info },
  info: { color: 'text-blue-400', bg: 'bg-blue-500/10', icon: Info },
  warn: { color: 'text-yellow-400', bg: 'bg-yellow-500/10', icon: AlertTriangle },
  error: { color: 'text-red-400', bg: 'bg-red-500/10', icon: XCircle },
  fatal: { color: 'text-red-600', bg: 'bg-red-600/20', icon: AlertCircle },
};

export default function LogViewer() {
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [filteredLogs, setFilteredLogs] = useState<LogEntry[]>([]);
  const [selectedLevel, setSelectedLevel] = useState<string>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [autoRefresh, setAutoRefresh] = useState(true);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadLogs();

    if (autoRefresh) {
      const interval = setInterval(loadLogs, 5000); // Refresh every 5 seconds
      return () => clearInterval(interval);
    }
  }, [autoRefresh, selectedLevel]);

  useEffect(() => {
    filterLogs();
  }, [logs, selectedLevel, searchQuery]);

  const loadLogs = async () => {
    try {
      setLoading(true);

      const token = localStorage.getItem('jwt_token');
      const level = selectedLevel === 'all' ? 'all' : selectedLevel;

      const response = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL}/api/admin/logs?limit=50&level=${level}`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        },
      );

      if (!response.ok) {
        throw new Error('Failed to fetch logs');
      }

      const result = await response.json();
      console.log('[LogViewer] Logs loaded:', result);

      setLogs(result.data?.logs || []);
    } catch (error) {
      console.error('[LogViewer] Failed to load logs:', error);
    } finally {
      setLoading(false);
    }
  };

  const filterLogs = () => {
    let filtered = logs;

    // Filter by search query only (level already filtered in API)
    if (searchQuery) {
      filtered = filtered.filter(
        log =>
          log.message.toLowerCase().includes(searchQuery.toLowerCase()) ||
          log.service.toLowerCase().includes(searchQuery.toLowerCase()) ||
          log.request_id?.toLowerCase().includes(searchQuery.toLowerCase()),
      );
    }

    setFilteredLogs(filtered);
  };

  const formatTime = (timestamp: string) => {
    const date = new Date(timestamp);
    return date.toLocaleTimeString('ru-RU', {
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
  };

  return (
    <Card className='bg-white/5 border-white/10 p-6'>
      <div className='flex items-center justify-between mb-6'>
        <h2 className='text-xl font-bold'>Recent Logs</h2>

        <div className='flex items-center gap-3'>
          <button
            onClick={() => setAutoRefresh(!autoRefresh)}
            className={`px-3 py-1.5 rounded-lg text-sm flex items-center gap-2 transition-colors ${
              autoRefresh
                ? 'bg-green-500/20 text-green-400 border border-green-500/30'
                : 'bg-gray-500/20 text-gray-400 border border-gray-500/30'
            }`}
          >
            <RefreshCw className={`w-4 h-4 ${autoRefresh ? 'animate-spin' : ''}`} />
            Auto-refresh
          </button>

          <a
            href='http://localhost:3001/explore?orgId=1&left=%7B%22datasource%22:%22loki%22%7D'
            target='_blank'
            rel='noopener noreferrer'
            className='px-3 py-1.5 rounded-lg text-sm flex items-center gap-2 bg-purple-500/20 text-purple-400 border border-purple-500/30 hover:bg-purple-500/30 transition-colors'
          >
            <ExternalLink className='w-4 h-4' />
            Open in Grafana
          </a>
        </div>
      </div>

      {/* Filters */}
      <div className='flex flex-col md:flex-row gap-4 mb-6'>
        <div className='flex-1 relative'>
          <Search className='absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400' />
          <input
            type='text'
            placeholder='Search logs...'
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            className='w-full pl-10 pr-4 py-2 bg-white/5 border border-white/10 rounded-lg text-white placeholder-gray-400 focus:outline-none focus:border-blue-500/50'
          />
        </div>

        <div className='flex gap-2'>
          {['all', 'error', 'warn', 'info'].map(level => (
            <button
              key={level}
              onClick={() => setSelectedLevel(level)}
              className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                selectedLevel === level
                  ? 'bg-blue-500/20 text-blue-400 border border-blue-500/30'
                  : 'bg-white/5 text-gray-400 border border-white/10 hover:bg-white/10'
              }`}
            >
              {level.charAt(0).toUpperCase() + level.slice(1)}
            </button>
          ))}
        </div>
      </div>

      {/* Logs List */}
      <div className='space-y-2 max-h-[600px] overflow-y-auto'>
        {loading && logs.length === 0 ? (
          <div className='text-center py-8 text-gray-400'>Loading logs...</div>
        ) : filteredLogs.length === 0 ? (
          <div className='text-center py-8 text-gray-400'>No logs found</div>
        ) : (
          filteredLogs.map((log, index) => {
            const config = levelConfig[log.level];
            const Icon = config.icon;

            return (
              <div
                key={index}
                className={`p-4 rounded-lg border border-white/10 ${config.bg} hover:bg-white/10 transition-colors`}
              >
                <div className='flex items-start gap-3'>
                  <Icon className={`w-5 h-5 mt-0.5 ${config.color}`} />

                  <div className='flex-1 min-w-0'>
                    <div className='flex items-center gap-3 mb-1'>
                      <span className={`text-sm font-semibold uppercase ${config.color}`}>
                        {log.level}
                      </span>
                      <span className='text-sm text-gray-400'>{formatTime(log.timestamp)}</span>
                      <span className='text-sm text-gray-500 px-2 py-0.5 bg-white/5 rounded'>
                        {log.service}
                      </span>
                    </div>

                    <p className='text-white mb-2'>{log.message}</p>

                    <div className='flex flex-wrap gap-3 text-xs text-gray-400'>
                      {log.caller && (
                        <span className='flex items-center gap-1'>
                          <span className='text-gray-500'>Caller:</span>
                          <code className='px-2 py-0.5 bg-white/5 rounded'>{log.caller}</code>
                        </span>
                      )}
                      {log.request_id && (
                        <span className='flex items-center gap-1'>
                          <span className='text-gray-500'>Request:</span>
                          <code className='px-2 py-0.5 bg-white/5 rounded'>{log.request_id}</code>
                        </span>
                      )}
                      {log.user_id && (
                        <span className='flex items-center gap-1'>
                          <span className='text-gray-500'>User:</span>
                          <code className='px-2 py-0.5 bg-white/5 rounded'>{log.user_id}</code>
                        </span>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            );
          })
        )}
      </div>

      <div className='mt-4 text-center text-sm text-gray-400'>
        Showing {filteredLogs.length} of {logs.length} logs
      </div>
    </Card>
  );
}
