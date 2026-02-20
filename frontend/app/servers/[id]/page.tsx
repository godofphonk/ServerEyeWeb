"use client";

import { useEffect, useState } from "react";
import { useRouter, useParams } from "next/navigation";
import { motion } from "framer-motion";
import { 
  ArrowLeft, 
  Share2,
  RefreshCw,
  AlertCircle,
  Cpu,
  HardDrive,
  MemoryStick,
  Wifi,
  Monitor
} from "lucide-react";
// Temporarily disable AuthContext to prevent conflicts
// import { useAuth } from "@/context/AuthContext";
import { apiClient } from "@/lib/api";
import { isAuthenticated as checkAuthToken, autoLoginForDev } from "@/lib/auth";
import { getServerStaticInfoCached, getServerCompleteData, getServerKey } from "@/lib/serverApi";
import { 
  MonitoredServer, 
  DashboardMetrics, 
  MetricsResponse,
  MetricAlert,
  ServerStaticInfo
} from "@/types";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import LiveMetricsPanel from "@/components/LiveMetricsPanel";
import ShareServerModal from "@/components/ShareServerModal";
import MetricsTabs from "@/components/tabs/MetricsTabs";

export default function ServerDetailPage() {
  const router = useRouter();
  const params = useParams();
  const serverId = params.id as string;
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [authLoading, setAuthLoading] = useState(true);
  
  const [server, setServer] = useState<MonitoredServer | null>(null);
  const [staticInfo, setStaticInfo] = useState<ServerStaticInfo | null>(null);
  const [dashboardMetrics, setDashboardMetrics] = useState<DashboardMetrics | null>(null);
  const [historicalMetrics, setHistoricalMetrics] = useState<MetricsResponse | null>(null);
  const [cpuHistoricalMetrics, setCpuHistoricalMetrics] = useState<MetricsResponse | null>(null);
  const [cpuUsageHistoricalMetrics, setCpuUsageHistoricalMetrics] = useState<MetricsResponse | null>(null);
  const [cpuLoadHistoricalMetrics, setCpuLoadHistoricalMetrics] = useState<MetricsResponse | null>(null);
  const [memoryHistoricalMetrics, setMemoryHistoricalMetrics] = useState<MetricsResponse | null>(null);
  const [networkHistoricalMetrics, setNetworkHistoricalMetrics] = useState<MetricsResponse | null>(null);
  const [diskHistoricalMetrics, setDiskHistoricalMetrics] = useState<MetricsResponse | null>(null);
  const [networkDetails, setNetworkDetails] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [timeRange, setTimeRange] = useState<'1h' | '6h' | '24h' | '7d' | '30d'>('1h');
  const [cpuTimeRange, setCpuTimeRange] = useState<'1h' | '6h' | '24h' | '7d' | '30d'>('1h');
  const [cpuUsageTimeRange, setCpuUsageTimeRange] = useState<'1h' | '6h' | '24h' | '7d' | '30d'>('1h');
  const [cpuLoadTimeRange, setCpuLoadTimeRange] = useState<'1h' | '6h' | '24h' | '7d' | '30d'>('1h');
  const [memoryTimeRange, setMemoryTimeRange] = useState<'1h' | '6h' | '24h' | '7d' | '30d'>('1h');
  const [networkTimeRange, setNetworkTimeRange] = useState<'1h' | '6h' | '24h' | '7d' | '30d'>('1h');
  const [diskTimeRange, setDiskTimeRange] = useState<'1h' | '6h' | '24h' | '7d' | '30d'>('1h');
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [showShareModal, setShowShareModal] = useState(false);

  // Auto-login for development
  useEffect(() => {
    const initAuth = async () => {
      if (!checkAuthToken()) {
        console.log('[ServerDetail] No token found, attempting auto-login...');
        const success = await autoLoginForDev();
        setIsAuthenticated(success);
      } else {
        setIsAuthenticated(true);
      }
      setAuthLoading(false);
    };
    initAuth();
  }, []);

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push("/login");
    }
  }, [isAuthenticated, authLoading, router]);

  // Load server info and static data only once
  useEffect(() => {
    if (isAuthenticated && serverId && !server && !staticInfo) {
      loadServerData().then(({ serverData, staticData }) => {
        setServer(serverData);
        setStaticInfo(staticData);
      }).catch(console.error);
    }
  }, [isAuthenticated, serverId, server, staticInfo]);

  // Load metrics when server is loaded or timeRange changes
  useEffect(() => {
    if (isAuthenticated && serverId && server && staticInfo) {
      loadMetrics();
    }
  }, [isAuthenticated, serverId, timeRange, server, staticInfo]);

  // Load CPU metrics when cpuTimeRange changes
  useEffect(() => {
    if (isAuthenticated && serverId && server && staticInfo && cpuTimeRange) {
      loadCpuMetrics();
    }
  }, [isAuthenticated, serverId, server, staticInfo, cpuTimeRange]);

  // Load CPU Usage metrics when cpuUsageTimeRange changes
  useEffect(() => {
    if (isAuthenticated && serverId && server && staticInfo && cpuUsageTimeRange) {
      loadCpuUsageMetrics();
    }
  }, [isAuthenticated, serverId, server, staticInfo, cpuUsageTimeRange]);

  // Load CPU Load metrics when cpuLoadTimeRange changes
  useEffect(() => {
    if (isAuthenticated && serverId && server && staticInfo && cpuLoadTimeRange) {
      loadCpuLoadMetrics();
    }
  }, [isAuthenticated, serverId, server, staticInfo, cpuLoadTimeRange]);

  // Load Memory metrics when memoryTimeRange changes
  useEffect(() => {
    if (isAuthenticated && serverId && server && staticInfo && memoryTimeRange) {
      loadMemoryMetrics();
    }
  }, [isAuthenticated, serverId, server, staticInfo, memoryTimeRange]);

  // Load Network metrics when networkTimeRange changes
  useEffect(() => {
    if (isAuthenticated && serverId && server && staticInfo && networkTimeRange) {
      loadNetworkMetrics();
    }
  }, [isAuthenticated, serverId, server, staticInfo, networkTimeRange]);

  // Load Disk metrics when diskTimeRange changes
  useEffect(() => {
    if (isAuthenticated && serverId && server && staticInfo && diskTimeRange) {
      loadDiskMetrics();
    }
  }, [isAuthenticated, serverId, server, staticInfo, diskTimeRange]);

  const loadMetrics = async () => {
    try {
      setLoading(true);
      console.log('[ServerDetail] Loading metrics for timeRange:', timeRange);
      
      // Загружаем dashboard metrics и сохраняем полный API ответ
      const [dashboardMetrics, apiResponse] = await Promise.all([
        loadDashboardMetrics(),
        apiClient.get<any>(`/servers/${serverId}/metrics/tiered`)
      ]);
      
      // Загружаем исторические данные для каждого типа метрик отдельно
      const [cpuMetrics, memoryMetrics, networkMetrics, diskMetrics] = await Promise.all([
        loadHistoricalMetrics(cpuTimeRange),
        loadHistoricalMetrics(memoryTimeRange),
        loadHistoricalMetrics(networkTimeRange),
        loadHistoricalMetrics(diskTimeRange)
      ]);

      console.log('[ServerDetail] API response keys:', Object.keys(apiResponse));
      console.log('[ServerDetail] networkDetails in API response:', apiResponse.networkDetails);
      
      setDashboardMetrics(dashboardMetrics);
      setHistoricalMetrics(cpuMetrics); // Временно используем CPU как основной
      setCpuHistoricalMetrics(cpuMetrics);
      setMemoryHistoricalMetrics(memoryMetrics);
      setNetworkHistoricalMetrics(networkMetrics);
      setDiskHistoricalMetrics(diskMetrics);
      
      // Устанавливаем networkDetails из полного API ответа
      if (apiResponse.networkDetails) {
        console.log('[ServerDetail] Setting networkDetails:', apiResponse.networkDetails);
        setNetworkDetails(apiResponse.networkDetails);
      } else {
        console.log('[ServerDetail] No networkDetails in API response');
      }
      console.log('[ServerDetail] Metrics loaded successfully');
    } catch (error) {
      console.error("Failed to load metrics:", error);
    } finally {
      setLoading(false);
    }
  };

  const loadCpuMetrics = async () => {
    try {
      console.log('[ServerDetail] Loading CPU metrics for cpuTimeRange:', cpuTimeRange);
      const cpuMetrics = await loadHistoricalMetrics(cpuTimeRange);
      setCpuHistoricalMetrics(cpuMetrics);
      console.log('[ServerDetail] CPU metrics loaded successfully');
    } catch (error) {
      console.error("Failed to load CPU metrics:", error);
    }
  };

  const loadCpuUsageMetrics = async () => {
    try {
      console.log('[ServerDetail] Loading CPU Usage metrics for cpuUsageTimeRange:', cpuUsageTimeRange);
      const cpuUsageMetrics = await loadHistoricalMetrics(cpuUsageTimeRange);
      setCpuUsageHistoricalMetrics(cpuUsageMetrics);
      console.log('[ServerDetail] CPU Usage metrics loaded successfully');
    } catch (error) {
      console.error("Failed to load CPU Usage metrics:", error);
    }
  };

  const loadCpuLoadMetrics = async () => {
    try {
      console.log('[ServerDetail] Loading CPU Load metrics for cpuLoadTimeRange:', cpuLoadTimeRange);
      const cpuLoadMetrics = await loadHistoricalMetrics(cpuLoadTimeRange);
      setCpuLoadHistoricalMetrics(cpuLoadMetrics);
      console.log('[ServerDetail] CPU Load metrics loaded successfully');
    } catch (error) {
      console.error("Failed to load CPU Load metrics:", error);
    }
  };

  const loadMemoryMetrics = async () => {
    try {
      console.log('[ServerDetail] Loading Memory metrics for memoryTimeRange:', memoryTimeRange);
      const memoryMetrics = await loadHistoricalMetrics(memoryTimeRange);
      setMemoryHistoricalMetrics(memoryMetrics);
      console.log('[ServerDetail] Memory metrics loaded successfully');
    } catch (error) {
      console.error("Failed to load Memory metrics:", error);
    }
  };

  const loadNetworkMetrics = async () => {
    try {
      console.log('[ServerDetail] Loading Network metrics for networkTimeRange:', networkTimeRange);
      const networkMetrics = await loadHistoricalMetrics(networkTimeRange);
      setNetworkHistoricalMetrics(networkMetrics);
      console.log('[ServerDetail] Network metrics loaded successfully');
    } catch (error) {
      console.error("Failed to load Network metrics:", error);
    }
  };

  const loadDiskMetrics = async () => {
    try {
      console.log('[ServerDetail] Loading Disk metrics for diskTimeRange:', diskTimeRange);
      const diskMetrics = await loadHistoricalMetrics(diskTimeRange);
      setDiskHistoricalMetrics(diskMetrics);
      console.log('[ServerDetail] Disk metrics loaded successfully');
    } catch (error) {
      console.error("Failed to load Disk metrics:", error);
    }
  };

  const loadServerData = async () => {
    try {
      setLoading(true);
      
      // First find server in monitored servers list to get ServerKey
      const servers = await apiClient.get<MonitoredServer[]>('/monitoredservers');
      const serverData = servers.find(s => s.serverId === serverId);
      
      if (!serverData) {
        throw new Error('Server not found');
      }
      
      // Use ServerKey from database
      const serverKey = serverData.serverKey || serverData.serverId;
      const staticData = await getServerStaticInfoCached(serverKey);
      
      return { serverData, staticData };
    } catch (error) {
      console.error("Failed to load server data:", error);
      throw error;
    } finally {
      setLoading(false);
    }
  };

  const loadServerInfo = async () => {
    const start = performance.now();
    console.log('[ServerDetail] Loading server info for:', serverId);
    const servers = await apiClient.get<MonitoredServer[]>('/monitoredservers');
    const serverData = servers.find(s => s.serverId === serverId);
    console.log(`[ServerDetail] Server info loaded in ${(performance.now() - start).toFixed(0)}ms`);
    if (!serverData) {
      throw new Error('Server not found');
    }
    return serverData;
  };

  const loadDashboardMetrics = async () => {
    const start = performance.now();
    console.log('[ServerDetail] Loading dashboard metrics...');
    
    // Get serverKey using helper function
    const serverKey = await getServerKey(serverId);
    
    const end = new Date();
    const startTime = new Date(end.getTime() - 5 * 60 * 1000);
    const response = await apiClient.get<any>(`/servers/by-key/${serverKey}/metrics?start=${startTime.toISOString()}&end=${end.toISOString()}&granularity=minute`);
    
    console.log(`[ServerDetail] Dashboard metrics loaded in ${(performance.now() - start).toFixed(0)}ms`, response);
    console.log('[ServerDetail] Full API response keys:', Object.keys(response));
    console.log('[ServerDetail] networkDetails in response:', response.networkDetails);
    
    // Transform API response to expected format
    const lastDataPoint = response.dataPoints?.[response.dataPoints.length - 1];
    const result: DashboardMetrics = {
      current: {
        cpu: response.summary?.avgCpu || lastDataPoint?.cpu_avg || 0,
        memory: response.summary?.avgMemory || lastDataPoint?.memory_avg || 0,
        disk: response.summary?.avgDisk || lastDataPoint?.disk_avg || 0,
        network: lastDataPoint?.network_avg || 0,
        load: lastDataPoint?.load_avg || 0,
        temperature: lastDataPoint?.temp_avg || 0,
        gpu_temperature: response.temperatureDetails?.gpu_temperature || 0,
      },
      trends: {
        cpu: response.summary?.avgCpu || 0,
        memory: response.summary?.avgMemory || 0,
        disk: response.summary?.avgDisk || 0,
        network: 0,
        load: 0,
        temperature: 0,
      },
      timestamp: response.endTime || new Date().toISOString(),
      alerts: []
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
  const fillMissingDataPoints = (dataPoints: any[], start: Date, end: Date, granularityMinutes: number) => {
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
          cpu_avg: 0, cpu_max: 0, cpu_min: 0,
          memory_avg: 0, memory_max: 0, memory_min: 0,
          disk_avg: 0, disk_max: 0, disk_min: 0,
          network_avg: 0, network_max: 0, network_min: 0,
          temp_avg: 0, temp_max: 0, temp_min: 0,
          load_avg: 0, load_max: 0, load_min: 0,
          sample_count: 0
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

    // Заполняем все временные слоты
    const result = [];
    let current = startRounded;
    while (current <= endRounded) {
      const existingPoint = dataMap.get(current);
      
      if (existingPoint) {
        result.push(existingPoint);
      } else {
        // Добавляем нулевые данные для пропущенных точек
        result.push({
          timestamp: new Date(current).toISOString(),
          cpu_avg: 0, cpu_max: 0, cpu_min: 0,
          memory_avg: 0, memory_max: 0, memory_min: 0,
          disk_avg: 0, disk_max: 0, disk_min: 0,
          network_avg: 0, network_max: 0, network_min: 0,
          temp_avg: 0, temp_max: 0, temp_min: 0,
          load_avg: 0, load_max: 0, load_min: 0,
          sample_count: 0
        });
      }
      
      current += granularityMinutes * 60 * 1000;
    }

    return result;
  };

  const loadHistoricalMetrics = async (range: '1h' | '6h' | '24h' | '7d' | '30d' = '1h') => {
    const perfStart = performance.now();
    const end = new Date();
    const start = new Date();
    
    switch(range) {
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

    // Determine granularity based on time range
    let granularity = '1h';
    switch(range) {
      case '1h':
        granularity = '1m';
        break;
      case '6h':
        granularity = '5m';
        break;
      case '24h':
        granularity = '15m';
        break;
      case '7d':
        granularity = '1h';
        break;
      case '30d':
        granularity = '6h';
        break;
    }

    console.log(`[ServerDetail] Loading historical metrics for ${range}...`);
    console.log(`[ServerDetail] Time range: ${start.toISOString()} to ${end.toISOString()}`);
    console.log(`[ServerDetail] Local time: ${start.toLocaleString()} to ${end.toLocaleString()}`);
    console.log(`[ServerDetail] Granularity: ${granularity}`);
    
    // Get serverKey using helper function
    const serverKey = await getServerKey(serverId);
    
    const response = await apiClient.get<any>(
      `/servers/by-key/${serverKey}/metrics?start=${start.toISOString()}&end=${end.toISOString()}&granularity=${granularity}`
    );
    console.log(`[ServerDetail] Historical metrics loaded in ${(performance.now() - perfStart).toFixed(0)}ms`);
    console.log(`[ServerDetail] Data points count: ${response.dataPoints?.length || 0}`);
    if (response.dataPoints?.length > 0) {
      console.log(`[ServerDetail] First point: ${response.dataPoints[0].timestamp}`);
      console.log(`[ServerDetail] Last point: ${response.dataPoints[response.dataPoints.length - 1].timestamp}`);
      
      // Проверяем если данных меньше ожидаемых
      const expectedPoints = range === '1h' ? 60 : 
                           range === '6h' ? 72 : 
                           range === '24h' ? 96 : 
                           range === '7d' ? 168 : 120;
      
      if (response.dataPoints.length < expectedPoints * 0.1) { // Меньше 10% от ожидаемых
        console.warn(`[ServerDetail] Limited data available: ${response.dataPoints.length}/${expectedPoints} points`);
      }
    }
    
    // Определяем гранулярность в минутах для заполнения
    const granularityMinutes = granularity === '1m' ? 1 :
                               granularity === '5m' ? 5 :
                               granularity === '15m' ? 15 :
                               granularity === '1h' ? 60 :
                               granularity === '6h' ? 360 : 1;
    
    // Заполняем пропущенные данные нулями
    const filledDataPoints = fillMissingDataPoints(response.dataPoints || [], start, end, granularityMinutes);
    console.log(`[ServerDetail] After filling: ${filledDataPoints.length} points`);
    
    // Transform API response - convert flat structure to nested for charts
    const transformedDataPoints = filledDataPoints.map((point: any) => ({
      timestamp: point.timestamp,
      cpu: { avg: point.cpu_avg, max: point.cpu_max, min: point.cpu_min },
      memory: { avg: point.memory_avg, max: point.memory_max, min: point.memory_min },
      disk: { avg: point.disk_avg, max: point.disk_max, min: point.disk_min },
      network: { avg: point.network_avg, max: point.network_max, min: point.network_min || point.network_avg },
      temperature: { avg: point.temp_avg, max: point.temp_max, min: point.temp_min || point.temp_avg },
      loadAverage: { avg: point.load_avg, max: point.load_max, min: point.load_min || point.load_avg },
      sampleCount: point.sample_count
    }));

    const result: MetricsResponse = {
      serverId: serverId,
      serverName: response.serverName,
      timeRange: {
        start: response.startTime || new Date().toISOString(),
        end: response.endTime || new Date().toISOString()
      },
      granularity: response.granularity || '1m',
      data: transformedDataPoints,
      totalPoints: response.totalPoints || 0,
      summary: response.summary || null,
      message: response.message || null,
      isCached: response.isCached || false,
      startTime: response.startTime,
      endTime: response.endTime
    };
    
    return result;
  };

  const handleRefresh = async () => {
    setIsRefreshing(true);
    await loadServerData();
    setIsRefreshing(false);
  };

  if (authLoading || loading) {
    return (
      <div className="min-h-screen bg-black flex items-center justify-center">
        <div className="text-white">Loading server details...</div>
      </div>
    );
  }

  if (!server) {
    return (
      <div className="min-h-screen bg-black flex items-center justify-center">
        <div className="text-center">
          <AlertCircle className="w-16 h-16 text-red-400 mx-auto mb-4" />
          <h2 className="text-2xl font-bold text-white mb-2">Server Not Found</h2>
          <p className="text-gray-400 mb-6">The server you're looking for doesn't exist.</p>
          <Button onClick={() => router.push('/dashboard')}>
            <ArrowLeft className="w-4 h-4 mr-2" />
            Back to Dashboard
          </Button>
        </div>
      </div>
    );
  }

  return (
    <main className="min-h-screen bg-black text-white">
      <div className="absolute inset-0 bg-gradient-to-br from-blue-600/10 via-purple-600/10 to-pink-600/10" />
      
      <div className="relative z-10">
        {/* Header */}
        <div className="border-b border-white/10 bg-black/50 backdrop-blur-sm">
          <div className="container mx-auto px-6 py-6">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-4">
                <Button variant="secondary" onClick={() => router.back()}>
                  <ArrowLeft className="w-5 h-5" />
                </Button>
                <div>
                  <div className="flex items-center gap-3">
                    <h1 className="text-3xl font-bold">{staticInfo?.hostname || server.serverName || server.hostname || server.serverId}</h1>
                    <div className="flex items-center gap-2">
                      <div className={`w-3 h-3 rounded-full ${server.isActive ? 'bg-green-500' : 'bg-red-500'} animate-pulse`} />
                      <span className="text-sm capitalize">{server.isActive ? 'Active' : 'Inactive'}</span>
                    </div>
                  </div>
                  <p className="text-gray-400 mt-1">{staticInfo?.operating_system || server.operatingSystem}</p>
                  {staticInfo && (
                    <p className="text-sm text-gray-500 mt-1">
                      Agent v{staticInfo.agent_version} • Last updated: {new Date(staticInfo.last_updated).toLocaleString()}
                    </p>
                  )}
                </div>
              </div>
              <div className="flex gap-3">
                <Button variant="secondary" onClick={handleRefresh} disabled={isRefreshing}>
                  <RefreshCw className={`w-5 h-5 mr-2 ${isRefreshing ? 'animate-spin' : ''}`} />
                  Refresh
                </Button>
                <Button onClick={() => setShowShareModal(true)}>
                  <Share2 className="w-5 h-5 mr-2" />
                  Share
                </Button>
              </div>
            </div>
            
            {/* Server Info */}
            <div className="mt-4 flex items-center gap-6 text-sm text-gray-400">
              <span>Server ID: {server.serverId}</span>
              <span>Access: {server.accessLevel}</span>
              <span>Last seen: {new Date(server.lastSeen).toLocaleString()}</span>
              <span>Added: {new Date(server.addedAt).toLocaleDateString()}</span>
            </div>
          </div>
        </div>

        {/* Content */}
        <div className="container mx-auto px-6 py-8">
          {/* System Information */}
          {staticInfo && (
            <div className="mb-8">
              <h2 className="text-2xl font-bold mb-6">System Information</h2>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                <Card>
                  <CardContent className="p-6">
                    <div className="flex items-center gap-3 mb-4">
                      <Cpu className="w-8 h-8 text-blue-400" />
                      <h3 className="font-semibold">CPU</h3>
                    </div>
                    <p className="text-sm text-gray-400 mb-2">{staticInfo.cpu_info.model}</p>
                    <div className="space-y-1 text-sm">
                      <p>Cores: {staticInfo.cpu_info.cores}</p>
                      <p>Threads: {staticInfo.cpu_info.threads}</p>
                      <p>Frequency: {staticInfo.cpu_info.frequency_mhz} MHz</p>
                    </div>
                  </CardContent>
                </Card>
                
                <Card>
                  <CardContent className="p-6">
                    <div className="flex items-center gap-3 mb-4">
                      <MemoryStick className="w-8 h-8 text-green-400" />
                      <h3 className="font-semibold">Memory</h3>
                    </div>
                    <div className="space-y-1 text-sm">
                      <p>Total: {staticInfo.memory_info.total_gb} GB</p>
                      <p>Type: {staticInfo.memory_info.type}</p>
                      <p>Speed: {staticInfo.memory_info.speed_mhz} MHz</p>
                    </div>
                  </CardContent>
                </Card>
                
                <Card>
                  <CardContent className="p-6">
                    <div className="flex items-center gap-3 mb-4">
                      <HardDrive className="w-8 h-8 text-purple-400" />
                      <h3 className="font-semibold">Storage</h3>
                    </div>
                    <div className="space-y-2 text-sm">
                      {staticInfo.disk_info.map((disk, i) => (
                        <div key={i} className="flex justify-between">
                          <span>{disk.device}</span>
                          <span>{disk.size_gb} GB</span>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
                
                <Card>
                  <CardContent className="p-6">
                    <div className="flex items-center gap-3 mb-4">
                      <Wifi className="w-8 h-8 text-yellow-400" />
                      <h3 className="font-semibold">Network</h3>
                    </div>
                    <div className="space-y-2 text-sm">
                      {staticInfo.network_interfaces.slice(0, 3).map((iface, i) => (
                        <div key={i} className="flex items-center justify-between">
                          <span>{iface.name}</span>
                          <span className={`w-2 h-2 rounded-full ${iface.status === 'up' ? 'bg-green-400' : 'bg-red-400'}`} />
                        </div>
                      ))}
                      {staticInfo.network_interfaces.length > 3 && (
                        <p className="text-gray-400">+{staticInfo.network_interfaces.length - 3} more</p>
                      )}
                    </div>
                  </CardContent>
                </Card>
              </div>
            </div>
          )}
          {/* Alerts */}
          {dashboardMetrics?.alerts && dashboardMetrics.alerts.length > 0 && (
            <div className="mb-6">
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
                  <div className="flex items-center gap-3">
                    <AlertCircle className={`w-5 h-5 ${alert.type === 'error' ? 'text-red-400' : 'text-yellow-400'}`} />
                    <div className="flex-1">
                      <p className="font-semibold">{alert.message}</p>
                      <p className="text-sm text-gray-400 mt-1">{new Date(alert.timestamp).toLocaleString()}</p>
                    </div>
                  </div>
                </motion.div>
              ))}
            </div>
          )}

          {/* Live Metrics Panel */}
          <div className="mb-8">
            <LiveMetricsPanel serverId={serverId} enabled={server?.isActive} />
          </div>

          {/* Metrics Tabs */}
          <div className="mb-8">
            <MetricsTabs 
              dashboardMetrics={dashboardMetrics}
              historicalMetrics={historicalMetrics}
              cpuHistoricalMetrics={cpuHistoricalMetrics}
              cpuUsageHistoricalMetrics={cpuUsageHistoricalMetrics}
              cpuLoadHistoricalMetrics={cpuLoadHistoricalMetrics}
              memoryHistoricalMetrics={memoryHistoricalMetrics}
              networkHistoricalMetrics={networkHistoricalMetrics}
              diskHistoricalMetrics={diskHistoricalMetrics}
              server={server}
              timeRange={timeRange}
              cpuTimeRange={cpuTimeRange}
              cpuUsageTimeRange={cpuUsageTimeRange}
              cpuLoadTimeRange={cpuLoadTimeRange}
              memoryTimeRange={memoryTimeRange}
              networkTimeRange={networkTimeRange}
              diskTimeRange={diskTimeRange}
              onTimeRangeChange={setTimeRange}
              onCpuTimeRangeChange={setCpuTimeRange}
              onCpuUsageTimeRangeChange={setCpuUsageTimeRange}
              onCpuLoadTimeRangeChange={setCpuLoadTimeRange}
              onMemoryTimeRangeChange={setMemoryTimeRange}
              onNetworkTimeRangeChange={setNetworkTimeRange}
              onDiskTimeRangeChange={setDiskTimeRange}
              networkDetails={networkDetails}
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
