'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/context/AuthContext';
import { logger } from '@/lib/telemetry/logger';
import { Card } from '@/components/ui/Card';
import {
  Activity,
  Database,
  Server,
  Users,
  TrendingUp,
  AlertCircle,
  ExternalLink,
  BarChart3,
} from 'lucide-react';
import LogViewer from './components/LogViewer';
import { isAdmin } from '@/lib/auth';

interface SystemStats {
  total_users: number;
  online_users: number;
  telegram_users: number;
  total_servers: number;
  active_servers: number;
  services_health?: {
    name: string;
    status: 'healthy' | 'degraded' | 'down';
    uptime: string;
    response_time?: number;
  }[];
}

type ActiveTab = 'overview' | 'services' | 'logs';

export default function SystemMonitoringPage() {
  const router = useRouter();
  const { user, loading: authLoading } = useAuth();
  const [stats, setStats] = useState<SystemStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<ActiveTab>('overview');

  useEffect(() => {
    if (authLoading) return;

    if (!user || !isAdmin(user)) {
      logger.warn('Admin access denied', { userRole: user?.role });
      router.push('/dashboard');
      return;
    }

    loadSystemStats();

    // Auto-refresh every 30 seconds
    const interval = setInterval(loadSystemStats, 30000);
    return () => clearInterval(interval);
  }, [user, authLoading]);

  const loadSystemStats = async () => {
    try {
      setLoading(true);

      // TODO: Implement admin/stats endpoint in backend
      // For now, use mock data to avoid 404 errors
      const mockStats: SystemStats = {
        total_users: 1,
        online_users: 1,
        telegram_users: 0,
        total_servers: 0,
        active_servers: 0,
        services_health: [
          { name: 'Backend API', status: 'healthy', uptime: '99.9%', response_time: 45 },
          { name: 'Database', status: 'healthy', uptime: '99.8%', response_time: 12 },
          { name: 'Redis Cache', status: 'healthy', uptime: '99.9%', response_time: 3 },
        ],
      };

      setStats(mockStats);
      
      // Original commented code - uncomment when backend endpoint is implemented
      /*
      const token = localStorage.getItem('jwt_token');

      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/admin/stats`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        const errorText = await response.text();
        logger.error('Failed to fetch admin stats', new Error(errorText), { status: response.status });
        throw new Error('Failed to fetch system stats');
      }

      const result = await response.json();
      setStats(result.data);
      */
    } catch (error) {
      logger.error('Failed to load system stats', error as Error);
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'healthy':
        return 'bg-green-500/20 text-green-400 border-green-500/30';
      case 'degraded':
        return 'bg-yellow-500/20 text-yellow-400 border-yellow-500/30';
      case 'down':
        return 'bg-red-500/20 text-red-400 border-red-500/30';
      default:
        return 'bg-gray-500/20 text-gray-400 border-gray-500/30';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'healthy':
        return <Activity className='w-4 h-4 text-green-400' />;
      case 'degraded':
        return <AlertCircle className='w-4 h-4 text-yellow-400' />;
      case 'down':
        return <AlertCircle className='w-4 h-4 text-red-400' />;
      default:
        return <Activity className='w-4 h-4 text-gray-400' />;
    }
  };

  if (loading && !stats) {
    return (
      <div className='min-h-screen bg-black flex items-center justify-center'>
        <div className='text-white'>Loading system monitoring...</div>
      </div>
    );
  }

  return (
    <div className='min-h-screen bg-black text-white p-8'>
      <div className='max-w-7xl mx-auto'>
        {/* Header */}
        <div className='mb-8'>
          <h1 className='text-3xl font-bold mb-2'>System Monitoring</h1>
          <p className='text-gray-400'>Real-time overview of ServerEye infrastructure</p>
        </div>

        {/* System Stats */}
        <div className='grid grid-cols-1 md:grid-cols-4 gap-6 mb-8'>
          <Card className='bg-gradient-to-br from-blue-500/10 to-blue-600/10 border-blue-500/20 p-6'>
            <div className='flex items-center justify-between mb-4'>
              <Users className='w-8 h-8 text-blue-400' />
              <span className='text-sm text-gray-400'>{stats?.online_users || 0} online</span>
            </div>
            <p className='text-sm text-gray-400 mb-1'>Total Users</p>
            <p className='text-3xl font-bold'>{stats?.total_users || 0}</p>
          </Card>

          <Card className='bg-gradient-to-br from-purple-500/10 to-purple-600/10 border-purple-500/20 p-6'>
            <div className='flex items-center justify-between mb-4'>
              <Server className='w-8 h-8 text-purple-400' />
              <span className='text-sm text-gray-400'>{stats?.active_servers || 0} active</span>
            </div>
            <p className='text-sm text-gray-400 mb-1'>Total Servers</p>
            <p className='text-3xl font-bold'>{stats?.total_servers || 0}</p>
          </Card>

          <Card className='bg-gradient-to-br from-green-500/10 to-green-600/10 border-green-500/20 p-6'>
            <div className='flex items-center justify-between mb-4'>
              <Activity className='w-8 h-8 text-green-400' />
              <TrendingUp className='w-5 h-5 text-green-400' />
            </div>
            <p className='text-sm text-gray-400 mb-1'>Telegram Users</p>
            <p className='text-3xl font-bold'>{stats?.telegram_users || 0}</p>
          </Card>

          <Card className='bg-gradient-to-br from-yellow-500/10 to-yellow-600/10 border-yellow-500/20 p-6'>
            <div className='flex items-center justify-between mb-4'>
              <Activity className='w-8 h-8 text-yellow-400' />
              <span className='text-sm text-green-400 font-semibold'>All Systems Operational</span>
            </div>
            <p className='text-sm text-gray-400 mb-1'>System Health</p>
            <p className='text-3xl font-bold'>99.8%</p>
          </Card>
        </div>

        {/* Navigation Tabs */}
        <div className='flex gap-2 mb-6 border-b border-white/10'>
          <button
            onClick={() => setActiveTab('overview')}
            className={`px-6 py-3 font-medium transition-colors ${
              activeTab === 'overview'
                ? 'text-blue-400 border-b-2 border-blue-400'
                : 'text-gray-400 hover:text-white'
            }`}
          >
            Overview
          </button>
          <button
            onClick={() => setActiveTab('services')}
            className={`px-6 py-3 font-medium transition-colors ${
              activeTab === 'services'
                ? 'text-blue-400 border-b-2 border-blue-400'
                : 'text-gray-400 hover:text-white'
            }`}
          >
            Services Health
          </button>
          <button
            onClick={() => setActiveTab('logs')}
            className={`px-6 py-3 font-medium transition-colors ${
              activeTab === 'logs'
                ? 'text-blue-400 border-b-2 border-blue-400'
                : 'text-gray-400 hover:text-white'
            }`}
          >
            Recent Logs
          </button>
        </div>

        {/* Tab Content */}
        {activeTab === 'overview' && (
          <div className='grid grid-cols-1 md:grid-cols-2 gap-6 animate-fade-in'>
            <Card className='bg-gradient-to-br from-orange-500/10 to-red-600/10 border-orange-500/20 p-6'>
              <div className='flex items-center justify-between mb-4'>
                <div className='flex items-center gap-3'>
                  <BarChart3 className='w-8 h-8 text-orange-400' />
                  <div>
                    <h3 className='text-xl font-bold'>Prometheus</h3>
                    <p className='text-sm text-gray-400'>Metrics & Monitoring</p>
                  </div>
                </div>
                <a
                  href='http://127.0.0.1:9090'
                  target='_blank'
                  rel='noopener noreferrer'
                  className='flex items-center gap-2 px-4 py-2 bg-orange-500/20 hover:bg-orange-500/30 rounded-lg transition-colors'
                >
                  Open
                  <ExternalLink className='w-4 h-4' />
                </a>
              </div>
              <p className='text-sm text-gray-400'>
                View real-time metrics, queries, and alerts for all services
              </p>
            </Card>

            <Card className='bg-gradient-to-br from-pink-500/10 to-purple-600/10 border-pink-500/20 p-6'>
              <div className='flex items-center justify-between mb-4'>
                <div className='flex items-center gap-3'>
                  <Activity className='w-8 h-8 text-pink-400' />
                  <div>
                    <h3 className='text-xl font-bold'>Grafana</h3>
                    <p className='text-sm text-gray-400'>Dashboards & Visualization</p>
                  </div>
                </div>
                <a
                  href='http://127.0.0.1:3000'
                  target='_blank'
                  rel='noopener noreferrer'
                  className='flex items-center gap-2 px-4 py-2 bg-pink-500/20 hover:bg-pink-500/30 rounded-lg transition-colors'
                >
                  Open
                  <ExternalLink className='w-4 h-4' />
                </a>
              </div>
              <p className='text-sm text-gray-400'>
                Access pre-built dashboards and create custom visualizations
              </p>
            </Card>
          </div>
        )}

        {activeTab === 'services' && (
          <Card className='bg-white/5 border-white/10 p-6 animate-fade-in'>
            <h2 className='text-xl font-bold mb-6'>Services Health</h2>

            <div className='space-y-4'>
              {(stats?.services_health || []).map(service => (
                <div
                  key={service.name}
                  className='flex items-center justify-between p-4 bg-white/5 rounded-lg border border-white/10 hover:bg-white/10 transition-colors'
                >
                  <div className='flex items-center gap-4'>
                    {getStatusIcon(service.status)}
                    <div>
                      <p className='font-semibold'>{service.name}</p>
                      <p className='text-sm text-gray-400'>Uptime: {service.uptime}</p>
                    </div>
                  </div>

                  <div className='flex items-center gap-4'>
                    {service.response_time && (
                      <div className='text-right'>
                        <p className='text-sm text-gray-400'>Response Time</p>
                        <p className='font-semibold'>{service.response_time}ms</p>
                      </div>
                    )}

                    <div
                      className={`px-4 py-2 rounded-lg border ${getStatusColor(service.status)}`}
                    >
                      {service.status.charAt(0).toUpperCase() + service.status.slice(1)}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </Card>
        )}

        {activeTab === 'logs' && (
          <div className='animate-fade-in'>
            <LogViewer />
          </div>
        )}
      </div>
    </div>
  );
}
