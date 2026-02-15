"use client";

import { useEffect, useState } from "react";
import { useRouter, useParams } from "next/navigation";
import { motion } from "framer-motion";
import { 
  ArrowLeft, 
  Activity, 
  Cpu, 
  HardDrive, 
  Network, 
  Thermometer, 
  Gauge,
  Share2,
  RefreshCw,
  Clock,
  AlertCircle
} from "lucide-react";
import { useAuth } from "@/context/AuthContext";
import { apiClient } from "@/lib/api";
import { 
  MonitoredServer, 
  DashboardMetrics, 
  MetricsResponse,
  MetricAlert 
} from "@/types";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import CurrentMetricsCard from "@/components/charts/CurrentMetricsCard";
import MetricsLineChart from "@/components/charts/MetricsLineChart";
import MetricsAreaChart from "@/components/charts/MetricsAreaChart";
import LiveMetricsPanel from "@/components/LiveMetricsPanel";
import ShareServerModal from "@/components/ShareServerModal";

export default function ServerDetailPage() {
  const router = useRouter();
  const params = useParams();
  const serverId = params.id as string;
  const { isAuthenticated, loading: authLoading } = useAuth();
  
  const [server, setServer] = useState<MonitoredServer | null>(null);
  const [dashboardMetrics, setDashboardMetrics] = useState<DashboardMetrics | null>(null);
  const [historicalMetrics, setHistoricalMetrics] = useState<MetricsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [timeRange, setTimeRange] = useState<'1h' | '6h' | '24h' | '7d'>('1h');
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [showShareModal, setShowShareModal] = useState(false);

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push("/login");
    }
  }, [isAuthenticated, authLoading, router]);

  useEffect(() => {
    if (isAuthenticated && serverId) {
      loadServerData();
    }
  }, [isAuthenticated, serverId, timeRange]);

  const loadServerData = async () => {
    try {
      setLoading(true);
      
      const [serverData, dashboardData, metricsData] = await Promise.all([
        loadServerInfo(),
        loadDashboardMetrics(),
        loadHistoricalMetrics()
      ]);

      setServer(serverData);
      setDashboardMetrics(dashboardData);
      setHistoricalMetrics(metricsData);
    } catch (error) {
      console.error("Failed to load server data:", error);
    } finally {
      setLoading(false);
    }
  };

  const loadServerInfo = async () => {
    console.log('[ServerDetail] Looking for serverId:', serverId);
    const servers = await apiClient.get<MonitoredServer[]>('/monitoredservers');
    console.log('[ServerDetail] Available servers:', servers);
    const serverData = servers.find(s => s.serverId === serverId);
    console.log('[ServerDetail] Found server:', serverData);
    if (!serverData) {
      throw new Error('Server not found');
    }
    return serverData;
  };

  const loadDashboardMetrics = async () => {
    return await apiClient.get<DashboardMetrics>(`/servers/${serverId}/metrics`);
  };

  const loadHistoricalMetrics = async () => {
    const end = new Date();
    const start = new Date();
    
    switch(timeRange) {
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
    }

    return await apiClient.get<MetricsResponse>(
      `/servers/${serverId}/metrics/history?start=${start.toISOString()}&end=${end.toISOString()}`
    );
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
                    <h1 className="text-3xl font-bold">{server.hostname || server.serverId}</h1>
                    <div className="flex items-center gap-2">
                      <div className={`w-3 h-3 rounded-full ${server.isActive ? 'bg-green-500' : 'bg-red-500'} animate-pulse`} />
                      <span className="text-sm capitalize">{server.isActive ? 'Active' : 'Inactive'}</span>
                    </div>
                  </div>
                  <p className="text-gray-400 mt-1">{server.operatingSystem}</p>
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

          {/* Current Metrics */}
          <div className="mb-8">
            <h2 className="text-2xl font-bold mb-6">Current Metrics</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {dashboardMetrics?.current && (
                <>
                  <CurrentMetricsCard
                    icon={Thermometer}
                    label="CPU Temperature"
                    value={dashboardMetrics.current.cpu}
                    unit="°C"
                    trend={dashboardMetrics.trends.cpu}
                    color="blue"
                  />
                  <CurrentMetricsCard
                    icon={HardDrive}
                    label="Memory Usage"
                    value={dashboardMetrics.current.memory}
                    unit="%"
                    trend={dashboardMetrics.trends.memory}
                    color="purple"
                  />
                  <CurrentMetricsCard
                    icon={HardDrive}
                    label="Disk Usage"
                    value={dashboardMetrics.current.disk}
                    unit="%"
                    trend={dashboardMetrics.trends.disk}
                    color="pink"
                  />
                  <CurrentMetricsCard
                    icon={Network}
                    label="Network"
                    value={dashboardMetrics.current.network}
                    unit="MB/s"
                    trend={dashboardMetrics.trends.network}
                    color="green"
                  />
                  <CurrentMetricsCard
                    icon={Cpu}
                    label="CPU Load"
                    value={dashboardMetrics.current.load}
                    unit=""
                    trend={dashboardMetrics.trends.load}
                    color="yellow"
                  />
                  <CurrentMetricsCard
                    icon={Activity}
                    label="Temperature"
                    value={dashboardMetrics.current.temperature}
                    unit="°C"
                    trend={dashboardMetrics.trends.temperature}
                    color="red"
                  />
                </>
              )}
            </div>
          </div>

          {/* Time Range Selector */}
          <div className="mb-6 flex items-center justify-between">
            <h2 className="text-2xl font-bold">Historical Metrics</h2>
            <div className="flex gap-2">
              {(['1h', '6h', '24h', '7d'] as const).map((range) => (
                <button
                  key={range}
                  onClick={() => setTimeRange(range)}
                  className={`px-4 py-2 rounded-lg font-medium transition-colors ${
                    timeRange === range
                      ? 'bg-blue-500 text-white'
                      : 'bg-white/5 text-gray-400 hover:bg-white/10'
                  }`}
                >
                  {range.toUpperCase()}
                </button>
              ))}
            </div>
          </div>

          {/* Charts */}
          {historicalMetrics?.data && historicalMetrics.data.length > 0 ? (
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <Card className="p-6">
                <div className="h-80">
                  <MetricsLineChart
                    data={historicalMetrics.data}
                    metricType="cpu"
                    title="CPU Temperature"
                    color="#3b82f6"
                    unit="°C"
                  />
                </div>
              </Card>

              <Card className="p-6">
                <div className="h-80">
                  <MetricsAreaChart
                    data={historicalMetrics.data}
                    metricType="memory"
                    title="Memory Usage"
                    color="#a855f7"
                    unit="%"
                  />
                </div>
              </Card>

              <Card className="p-6">
                <div className="h-80">
                  <MetricsAreaChart
                    data={historicalMetrics.data}
                    metricType="disk"
                    title="Disk Usage"
                    color="#ec4899"
                    unit="%"
                  />
                </div>
              </Card>

              <Card className="p-6">
                <div className="h-80">
                  <MetricsLineChart
                    data={historicalMetrics.data}
                    metricType="network"
                    title="Network Traffic"
                    color="#10b981"
                    unit="MB/s"
                  />
                </div>
              </Card>

              <Card className="p-6">
                <div className="h-80">
                  <MetricsLineChart
                    data={historicalMetrics.data}
                    metricType="load"
                    title="CPU Load"
                    color="#f59e0b"
                  />
                </div>
              </Card>

              <Card className="p-6">
                <div className="h-80">
                  <MetricsAreaChart
                    data={historicalMetrics.data}
                    metricType="temperature"
                    title="System Temperature"
                    color="#ef4444"
                    unit="°C"
                  />
                </div>
              </Card>
            </div>
          ) : (
            <Card className="p-12 text-center">
              <Clock className="w-16 h-16 text-gray-600 mx-auto mb-4" />
              <h3 className="text-xl font-bold mb-2">No Historical Data</h3>
              <p className="text-gray-400">Metrics data will appear here once available.</p>
            </Card>
          )}
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
