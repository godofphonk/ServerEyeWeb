'use client';

import { useEffect, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import { motion } from 'framer-motion';
import {
  ArrowLeft,
  Share2,
  RefreshCw,
  AlertCircle,
  Cpu,
  HardDrive,
  MemoryStick,
  Wifi,
} from 'lucide-react';
import { useAuth } from '@/context/AuthContext';
import {
  getServerStaticInfoCached,
  getServerKey,
  getCachedMonitoredServers,
  getCachedTieredMetrics,
} from '@/lib/serverApi';
import { MonitoredServer, DashboardMetrics, MetricsResponse, ServerStaticInfo } from '@/types';

interface FlatMetricsDataPoint {
  timestamp: string;
  cpu_avg?: number;
  cpu_max?: number;
  cpu_min?: number;
  memory_avg?: number;
  memory_max?: number;
  memory_min?: number;
  disk_avg?: number;
  disk_max?: number;
  disk_min?: number;
  network_avg?: number;
  network_max?: number;
  network_min?: number;
  temp_avg?: number;
  temp_max?: number;
  temp_min?: number;
  load_avg?: number;
  load_max?: number;
  load_min?: number;
  sample_count?: number;
}

import { Card, CardContent } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import ShareServerModal from '@/components/ShareServerModal';
import MetricsTabs from '@/components/tabs/MetricsTabs';

export default function ServerDetailPage() {
  const router = useRouter();
  const params = useParams();
  const serverId = params.id as string;
  const { user, loading: authLoading } = useAuth();

  const [server, setServer] = useState<MonitoredServer | null>(null);
  const [staticInfo, setStaticInfo] = useState<ServerStaticInfo | null>(null);
  const [dashboardMetrics, setDashboardMetrics] = useState<DashboardMetrics | null>(null);
  // Unified historical metrics state - all charts use the same data
  const [historicalMetrics, setHistoricalMetrics] = useState<MetricsResponse | null>(null);
  const networkDetails = useState<any>(null)[0]; // eslint-disable-line @typescript-eslint/no-explicit-any
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('cpu');
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [showShareModal, setShowShareModal] = useState(false);

  // Redirect to login if not authenticated
  useEffect(() => {
    if (!authLoading && !user) {
      router.push('/login');
      return;
    }
  }, [user, authLoading, router]);

  // Unified data loading - server info + metrics in one place
  useEffect(() => {
    if (user && serverId) {
      const loadAllData = async () => {
        // Only show loading for initial data load, not time range changes
        const isFirstLoad = !server || !staticInfo;

        try {
          if (isFirstLoad) {
            setLoading(true);
          }

          // Load server info and static data only if not loaded
          if (!server || !staticInfo) {
            const serverData = await getServerInfo();
            const staticData = await getServerStaticInfoCached(serverData.serverKey || serverId);
            setServer(serverData);
            setStaticInfo(staticData);
          }

          // Load dashboard metrics and historical metrics (optimized - 1 request instead of 8)
          const [dashboardMetrics, historicalMetrics] = await Promise.all([
            loadDashboardMetrics(),
            loadHistoricalMetrics('1h'), // Load default 1h data initially
          ]);

          // Set dashboard metrics
          setDashboardMetrics(dashboardMetrics);

          // Set historical metrics for all charts from single response
          setHistoricalMetrics(historicalMetrics);
        } catch (_error) {
          // ignore error
        } finally {
          // Only hide loading for initial load
          if (isFirstLoad) {
            setLoading(false);
          }
        }
      };

      loadAllData();
    }
  }, [user, serverId]);

  // Helper function to get server info (optimized)
  const getServerInfo = async () => {
    const servers = await getCachedMonitoredServers();
    const serverData = servers.find(s => s.serverId === serverId);

    if (!serverData) {
      throw new Error('Server not found');
    }
    return serverData;
  };

  const loadDashboardMetrics = async () => {
    // Get serverKey using helper function
    const serverKey = await getServerKey(serverId);

    const end = new Date();
    const startTime = new Date(end.getTime() - 5 * 60 * 1000);

    // Use tiered endpoint for consistency with historical metrics
    const response = (await getCachedTieredMetrics(
      serverKey,
      startTime.toISOString(),
      end.toISOString(),
      'minute', // Use minute granularity for dashboard
    )) as MetricsResponse;

    // Transform API response to expected format
    const lastDataPoint = response.data?.[response.data.length - 1];

    const result: DashboardMetrics = {
      current: {
        cpu: response.summary?.avgCpu || lastDataPoint?.cpu.avg || 0,
        memory: response.summary?.avgMemory || lastDataPoint?.memory.avg || 0,
        disk: response.summary?.avgDisk || lastDataPoint?.disk.avg || 0,
        network: lastDataPoint?.network.avg || 0,
        load: lastDataPoint?.loadAverage.avg || 0,
        temperature: response.summary?.avgTemperature || lastDataPoint?.temperature.avg || 0,
        gpu_temperature: 0,
      },
      trends: {
        cpu: response.summary?.avgCpu || 0,
        memory: response.summary?.avgMemory || 0,
        disk: response.summary?.avgDisk || 0,
        network: 0,
        load: 0,
        temperature: 0,
      },
      timestamp: response.timeRange.end || new Date().toISOString(),
      alerts: [],
    };

    return result;
  };

  // Функция для округления timestamp до заданной гранулярности
  const roundToGranularity = (date: Date, granularityMinutes: number): number => {
    const ms = date.getTime();
    const granularityMs = granularityMinutes * 60 * 1000;
    return Math.floor(ms / granularityMs) * granularityMs;
  };

  // Функция для заполнения пропущенных данных нулями
  const fillMissingDataPoints = (
    dataPoints: any[], // eslint-disable-line @typescript-eslint/no-explicit-any
    start: Date,
    end: Date,
    granularityMinutes: number,
    _range: string,
    _expectedPoints: number,
  ) => {
    // Округляем start и end до гранулярности
    const startRounded = roundToGranularity(start, granularityMinutes);
    const endRounded = roundToGranularity(end, granularityMinutes);

    if (!dataPoints || dataPoints.length === 0) {
      // Если данных нет вообще, создаем пустой массив с нулями
      const result = [];
      let current = startRounded;
      while (current <= endRounded) {
        result.push({
          timestamp: new Date(current).toISOString(),
          cpu_avg: 0,
          cpu_max: 0,
          cpu_min: 0,
          memory_avg: 0,
          memory_max: 0,
          memory_min: 0,
          disk_avg: 0,
          disk_max: 0,
          disk_min: 0,
          network_avg: 0,
          network_max: 0,
          network_min: 0,
          temp_avg: 0,
          temp_max: 0,
          temp_min: 0,
          load_avg: 0,
          load_max: 0,
          load_min: 0,
          sample_count: 0,
        });
        current += granularityMinutes * 60 * 1000;
      }
      return result;
    }

    // Создаем карту существующих точек с округленными timestamp
    const dataMap = new Map();
    dataPoints.forEach(point => {
      const timestamp = roundToGranularity(new Date(point.timestamp), granularityMinutes);
      dataMap.set(timestamp, point);
    });

    // Находим реальный период данных (когда агент был установлен)
    const firstDataPoint = new Date(dataPoints[0].timestamp);

    // Для коротких периодов (1ч, 6ч) используем все данные как есть
    // Для длинных периодов (24ч, 7д, 30д) заполняем нулями после реальных данных
    const periodHours = (endRounded - startRounded) / (1000 * 60 * 60);

    let actualStart: number = startRounded;
    const useLongPeriodLogic = periodHours >= 24; // 24ч и больше

    if (useLongPeriodLogic) {
      // Для длинных периодов проверяем есть ли данные за весь период
      const totalPeriodMs = endRounded - startRounded;
      const dataPeriodMs = firstDataPoint.getTime() - startRounded;

      // Если агент установлен менее чем за 70% периода, используем startRounded
      if (dataPeriodMs > totalPeriodMs * 0.7) {
        actualStart = startRounded;
      } else {
        actualStart = Math.max(startRounded, firstDataPoint.getTime());
      }
    } else {
      // use default actualStart
    }

    // Заполняем все временные слоты от actualStart до endRounded
    const result = [];
    let current = actualStart;
    while (current <= endRounded) {
      const existingPoint = dataMap.get(current);

      if (existingPoint) {
        result.push(existingPoint);
      } else {
        // Добавляем нулевые данные для пропущенных точек
        result.push({
          timestamp: new Date(current).toISOString(),
          cpu_avg: 0,
          cpu_max: 0,
          cpu_min: 0,
          memory_avg: 0,
          memory_max: 0,
          memory_min: 0,
          disk_avg: 0,
          disk_max: 0,
          disk_min: 0,
          network_avg: 0,
          network_max: 0,
          network_min: 0,
          temp_avg: 0,
          temp_max: 0,
          temp_min: 0,
          load_avg: 0,
          load_max: 0,
          load_min: 0,
          sample_count: 0,
        });
      }

      current += granularityMinutes * 60 * 1000;
    }

    return result;
  };

  const loadHistoricalMetrics = async (range: '1h' | '6h' | '24h' | '7d' | '30d' = '1h') => {
    const now = new Date();

    // Round to nearest minute for better alignment with Go API data
    const end = new Date(now);
    end.setSeconds(0, 0); // Round to minute
    const start = new Date(end);

    switch (range) {
      case '1h':
        start.setHours(start.getHours() - 1);
        break;
      case '6h':
        start.setHours(start.getHours() - 6);
        break;
      case '24h':
        start.setHours(start.getHours() - 24);
        break;
      case '7d':
        start.setDate(start.getDate() - 7);
        break;
      case '30d':
        start.setDate(start.getDate() - 30);
        break;
    }

    // Get serverKey using helper function
    const serverKey = await getServerKey(serverId);

    // Calculate granularity based on range (API optimized)
    const granularityConfig = {
      '1h': { granularity: '1m', minutes: 1, expectedPoints: 60 },
      '6h': { granularity: '10m', minutes: 10, expectedPoints: 36 },
      '24h': { granularity: '30m', minutes: 30, expectedPoints: 48 },
      '7d': { granularity: '2h', minutes: 120, expectedPoints: 84 },
      '30d': { granularity: '6h', minutes: 360, expectedPoints: 120 },
    };

    const config = granularityConfig[range] || granularityConfig['1h'];
    const { granularity, expectedPoints } = config;

    // Use tiered endpoint for all ranges with optimized granularity
    const response = (await getCachedTieredMetrics(
      serverKey,
      start.toISOString(),
      end.toISOString(),
      granularity,
    )) as MetricsResponse;

    // Детальное логирование timestamps для проверки актуальности данных
    if (response.data && response.data.length > 0) {
      // Фильтр переустановки агента отключен - показываем все данные
    }

    // Детальное логирование первых и последних точек данных
    if (response.data && response.data.length > 0) {
      // data points available for logging
    }

    if (response.data?.length > 0) {
      // Проверяем если данных меньше ожидаемых
      if (response.data.length < expectedPoints * 0.1) {
        // Меньше 10% от ожидаемых
        // Ограниченные данные
      }
    }

    // Заполняем пропущенные данные нулями
    const filledDataPoints = fillMissingDataPoints(
      response.data || [],
      start,
      end,
      config.minutes,
      range,
      expectedPoints,
    );

    // Transform API response - convert flat structure to nested for charts
    const transformedDataPoints = filledDataPoints.map((point: FlatMetricsDataPoint) => ({
      timestamp: point.timestamp,
      cpu: { avg: point.cpu_avg || 0, max: point.cpu_max || 0, min: point.cpu_min || 0 },
      memory: {
        avg: point.memory_avg || 0,
        max: point.memory_max || 0,
        min: point.memory_min || 0,
      },
      disk: { avg: point.disk_avg || 0, max: point.disk_max || 0, min: point.disk_min || 0 },
      network: {
        avg: point.network_avg || 0,
        max: point.network_max || 0,
        min: point.network_min || point.network_avg || 0,
      },
      temperature: {
        avg: point.temp_avg || 0,
        max: point.temp_max || 0,
        min: point.temp_min || point.temp_avg || 0,
      },
      loadAverage: {
        avg: point.load_avg || 0,
        max: point.load_max || 0,
        min: point.load_min || point.load_avg || 0,
      },
      sampleCount: point.sample_count || 0,
    }));

    const result: MetricsResponse = {
      serverId: serverId,
      serverName: response.serverName,
      timeRange: {
        start: response.startTime || new Date().toISOString(),
        end: response.endTime || new Date().toISOString(),
      },
      granularity: response.granularity || '1m',
      data: transformedDataPoints,
      totalPoints: response.totalPoints || 0,
      summary: response.summary || null,
      message: response.message || null,
      isCached: response.isCached || false,
      startTime: response.startTime,
      endTime: response.endTime,
    };

    return result;
  };

  const handleRefresh = async () => {
    setIsRefreshing(true);
    // Force reload server info and metrics
    setServer(null);
    setStaticInfo(null);
    // This will trigger useEffect to reload data
    setIsRefreshing(false);
  };

  if (authLoading || loading) {
    return (
      <div className='min-h-screen bg-black flex items-center justify-center'>
        <div className='text-white'>Loading server details...</div>
      </div>
    );
  }

  if (!server) {
    return (
      <div className='min-h-screen bg-black flex items-center justify-center'>
        <div className='text-center'>
          <AlertCircle className='w-16 h-16 text-red-400 mx-auto mb-4' />
          <h2 className='text-2xl font-bold text-white mb-2'>Server Not Found</h2>
          <p className='text-gray-400 mb-6'>The server you're looking for doesn't exist.</p>
          <Button onClick={() => router.push('/dashboard')}>
            <ArrowLeft className='w-4 h-4 mr-2' />
            Back to Dashboard
          </Button>
        </div>
      </div>
    );
  }

  return (
    <main className='min-h-screen bg-black text-white'>
      <div className='absolute inset-0 bg-gradient-to-br from-blue-600/10 via-purple-600/10 to-pink-600/10' />

      <div className='relative z-10'>
        {/* Header */}
        <div className='border-b border-white/10 bg-black/50 backdrop-blur-sm'>
          <div className='container mx-auto px-6 py-6'>
            <div className='flex items-center justify-between'>
              <div className='flex items-center gap-4'>
                <Button variant='secondary' onClick={() => router.back()}>
                  <ArrowLeft className='w-5 h-5' />
                </Button>
                <div>
                  <div className='flex items-center gap-3'>
                    <h1 className='text-3xl font-bold'>
                      {staticInfo?.hostname ||
                        server.serverName ||
                        server.hostname ||
                        server.serverId}
                    </h1>
                    <div className='flex items-center gap-2'>
                      <div
                        className={`w-3 h-3 rounded-full ${server.isActive ? 'bg-green-500' : 'bg-red-500'} animate-pulse`}
                      />
                      <span className='text-sm capitalize'>
                        {server.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </div>
                  </div>
                  <p className='text-gray-400 mt-1'>
                    {staticInfo?.operating_system || server.operatingSystem}
                  </p>
                  {staticInfo && (
                    <p className='text-sm text-gray-500 mt-1'>
                      Agent v{staticInfo.agent_version} • Last updated:{' '}
                      {new Date(staticInfo.last_updated).toLocaleString()}
                    </p>
                  )}
                </div>
              </div>
              <div className='flex gap-3'>
                <Button variant='secondary' onClick={handleRefresh} disabled={isRefreshing}>
                  <RefreshCw className={`w-5 h-5 mr-2 ${isRefreshing ? 'animate-spin' : ''}`} />
                  Refresh
                </Button>
                <Button onClick={() => setShowShareModal(true)}>
                  <Share2 className='w-5 h-5 mr-2' />
                  Share
                </Button>
              </div>
            </div>

            {/* Server Info */}
            <div className='mt-4 flex items-center gap-6 text-sm text-gray-400'>
              <span>Server ID: {server.serverId}</span>
              <span>Server Key: {server.serverKey || 'N/A'}</span>
              <span>Last seen: {new Date(server.lastSeen).toLocaleString()}</span>
              <span>Added: {new Date(server.addedAt).toLocaleDateString()}</span>
            </div>
          </div>
        </div>

        {/* Content */}
        <div className='container mx-auto px-6 py-8'>
          {/* System Information */}
          {staticInfo && (
            <div className='mb-8'>
              <h2 className='text-2xl font-bold mb-6'>System Information</h2>
              <div className='grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6'>
                <Card>
                  <CardContent className='p-6'>
                    <div className='flex items-center gap-3 mb-4'>
                      <Cpu className='w-8 h-8 text-blue-400' />
                      <h3 className='font-semibold'>CPU</h3>
                    </div>
                    <p className='text-sm text-gray-400 mb-2'>
                      {staticInfo.cpu_info?.model || 'N/A'}
                    </p>
                    <div className='space-y-1 text-sm'>
                      <p>Cores: {staticInfo.cpu_info?.cores || 'N/A'}</p>
                      <p>Threads: {staticInfo.cpu_info?.threads || 'N/A'}</p>
                      <p>Frequency: {staticInfo.cpu_info?.frequency_mhz || 'N/A'} MHz</p>
                    </div>
                  </CardContent>
                </Card>

                <Card>
                  <CardContent className='p-6'>
                    <div className='flex items-center gap-3 mb-4'>
                      <MemoryStick className='w-8 h-8 text-green-400' />
                      <h3 className='font-semibold'>Memory</h3>
                    </div>
                    <div className='space-y-1 text-sm'>
                      <p>Total: {staticInfo.memory_info?.total_gb || 'N/A'} GB</p>
                      <p>Type: {staticInfo.memory_info?.type || 'N/A'}</p>
                      <p>Speed: {staticInfo.memory_info?.speed_mhz || 'N/A'} MHz</p>
                    </div>
                  </CardContent>
                </Card>

                <Card>
                  <CardContent className='p-6'>
                    <div className='flex items-center gap-3 mb-4'>
                      <HardDrive className='w-8 h-8 text-purple-400' />
                      <h3 className='font-semibold'>Storage</h3>
                    </div>
                    <div className='space-y-2 text-sm'>
                      {(staticInfo.disk_info || []).map((disk, i) => (
                        <div key={i} className='flex justify-between'>
                          <span>{disk.device}</span>
                          <span>{disk.size_gb} GB</span>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>

                <Card>
                  <CardContent className='p-6'>
                    <div className='flex items-center gap-3 mb-4'>
                      <Wifi className='w-8 h-8 text-yellow-400' />
                      <h3 className='font-semibold'>Network</h3>
                    </div>
                    <div className='space-y-2 text-sm'>
                      {(staticInfo.network_interfaces || []).slice(0, 3).map((iface, i) => (
                        <div key={i} className='flex items-center justify-between'>
                          <span>{iface.name}</span>
                          <span
                            className={`w-2 h-2 rounded-full ${iface.status === 'up' ? 'bg-green-400' : 'bg-red-400'}`}
                          />
                        </div>
                      ))}
                      {(staticInfo.network_interfaces || []).length > 3 && (
                        <p className='text-gray-400'>
                          +{(staticInfo.network_interfaces || []).length - 3} more
                        </p>
                      )}
                    </div>
                  </CardContent>
                </Card>
              </div>
            </div>
          )}
          {/* Alerts */}
          {dashboardMetrics?.alerts && dashboardMetrics.alerts.length > 0 && (
            <div className='mb-6'>
              {dashboardMetrics.alerts.map((alert, i) => (
                <motion.div
                  key={i}
                  initial={{ opacity: 0, y: -10 }}
                  animate={{ opacity: 1, y: 0 }}
                  className={`p-4 rounded-lg border mb-3 ${
                    alert.type === 'error'
                      ? 'bg-red-500/10 border-red-500/20'
                      : 'bg-yellow-500/10 border-yellow-500/20'
                  }`}
                >
                  <div className='flex items-center gap-3'>
                    <AlertCircle
                      className={`w-5 h-5 ${alert.type === 'error' ? 'text-red-400' : 'text-yellow-400'}`}
                    />
                    <div className='flex-1'>
                      <p className='font-semibold'>{alert.message}</p>
                      <p className='text-sm text-gray-400 mt-1'>
                        {new Date(alert.timestamp).toLocaleString()}
                      </p>
                    </div>
                  </div>
                </motion.div>
              ))}
            </div>
          )}

          {/* Metrics Tabs */}
          <div className='mb-8'>
            <MetricsTabs
              dashboardMetrics={dashboardMetrics}
              historicalMetrics={historicalMetrics}
              staticInfo={staticInfo}
              server={server}
              networkDetails={networkDetails}
              activeTab={activeTab}
              onActiveTabChange={setActiveTab}
              loadHistoricalMetrics={loadHistoricalMetrics}
            />
          </div>
        </div>
      </div>

      {/* Share Modal */}
      {server && (
        <ShareServerModal
          isOpen={showShareModal}
          onClose={() => setShowShareModal(false)}
          serverId={server.serverId}
          serverName={server.hostname}
          currentAccessLevel={server.accessLevel}
        />
      )}
    </main>
  );
}
