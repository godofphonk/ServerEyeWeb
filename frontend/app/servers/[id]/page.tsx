"use client";

import { useEffect, useState } from "react";
import { useRouter, useParams } from "next/navigation";
import { motion } from "framer-motion";
import { 
  ArrowLeft, 
  Share2,
  RefreshCw,
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
import LiveMetricsPanel from "@/components/LiveMetricsPanel";
import ShareServerModal from "@/components/ShareServerModal";
import MetricsTabs from "@/components/tabs/MetricsTabs";

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

  // Load server info only once
  useEffect(() => {
    if (isAuthenticated && serverId && !server) {
      loadServerInfo().then(setServer).catch(console.error);
    }
  }, [isAuthenticated, serverId]);

  // Load metrics when server is loaded or timeRange changes
  useEffect(() => {
    if (isAuthenticated && serverId && server) {
      loadMetrics();
    }
  }, [isAuthenticated, serverId, timeRange, server]);

  const loadMetrics = async () => {
    try {
      setLoading(true);
      console.log('[ServerDetail] Loading metrics for timeRange:', timeRange);
      
      const [dashboardData, metricsData] = await Promise.all([
        loadDashboardMetrics(),
        loadHistoricalMetrics()
      ]);

      setDashboardMetrics(dashboardData);
      setHistoricalMetrics(metricsData);
      console.log('[ServerDetail] Metrics loaded successfully');
    } catch (error) {
      console.error("Failed to load metrics:", error);
    } finally {
      setLoading(false);
    }
  };

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
    const end = new Date();
    const startTime = new Date(end.getTime() - 5 * 60 * 1000);
    const response = await apiClient.get<any>(`/servers/${serverId}/metrics/tiered?start=${startTime.toISOString()}&end=${end.toISOString()}&granularity=1m`);
    console.log(`[ServerDetail] Dashboard metrics loaded in ${(performance.now() - start).toFixed(0)}ms`, response);
    
    // Transform API response to expected format
    const result: DashboardMetrics = {
      current: {
        cpu: response.summary?.avgCpu || 0,
        memory: response.summary?.avgMemory || 0,
        disk: response.summary?.avgDisk || 0,
        network: response.dataPoints?.[response.dataPoints.length - 1]?.network?.avg || 0,
        load: response.dataPoints?.[response.dataPoints.length - 1]?.loadAverage?.avg || 0,
        temperature: response.dataPoints?.[response.dataPoints.length - 1]?.temperature_details?.cpu_temperature || 
                   response.dataPoints?.[response.dataPoints.length - 1]?.temperature?.avg || 0,
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

  const loadHistoricalMetrics = async () => {
    const perfStart = performance.now();
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

    console.log(`[ServerDetail] Loading historical metrics for ${timeRange}...`);
    const response = await apiClient.get<any>(
      `/servers/${serverId}/metrics/tiered?start=${start.toISOString()}&end=${end.toISOString()}&granularity=1h`
    );
    console.log(`[ServerDetail] Historical metrics loaded in ${(performance.now() - perfStart).toFixed(0)}ms`, response);
    
    // Transform API response - use dataPoints instead of data
    const result: MetricsResponse = {
      serverId: serverId,
      serverName: response.serverName,
      timeRange: {
        start: response.startTime || new Date().toISOString(),
        end: response.endTime || new Date().toISOString()
      },
      granularity: response.granularity || '1m',
      data: response.dataPoints || [],
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
                    <h1 className="text-3xl font-bold">{server.serverName || server.hostname || server.serverId}</h1>
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

          {/* Metrics Tabs */}
          <div className="mb-8">
            <MetricsTabs 
              dashboardMetrics={dashboardMetrics}
              historicalMetrics={historicalMetrics}
              server={server}
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
