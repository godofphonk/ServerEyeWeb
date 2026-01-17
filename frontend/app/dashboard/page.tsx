"use client";

import { useEffect, useState } from "react";
import { motion } from "framer-motion";
import { Activity, Cpu, HardDrive, Server as ServerIcon, Plus, RefreshCw, Trash2 } from "lucide-react";
import { useAuth } from "@/context/AuthContext";
import { useRouter } from "next/navigation";
import { apiClient } from "@/lib/api";
import { Server, Metric } from "@/types";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";

export default function DashboardPage() {
  const { user, isAuthenticated, loading } = useAuth();
  const router = useRouter();
  const [servers, setServers] = useState<Server[]>([]);
  const [metrics, setMetrics] = useState<Record<string, Metric[]>>({});
  const [isLoadingServers, setIsLoadingServers] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [deleteModal, setDeleteModal] = useState<{ isOpen: boolean; server: Server | null }>({
    isOpen: false,
    server: null,
  });
  const [isDeleting, setIsDeleting] = useState(false);

  useEffect(() => {
    if (!loading && !isAuthenticated) {
      router.push("/login");
    }
  }, [isAuthenticated, loading, router]);

  useEffect(() => {
    if (isAuthenticated) {
      loadServers();
    }
  }, [isAuthenticated]);

  const loadServers = async () => {
    try {
      setIsLoadingServers(true);
      // TODO: Replace with actual API call
      // const response = await apiClient.get<{servers: Server[]}>('/servers');
      // setServers(response.servers || []);
      
      // Mock data for now
      const mockServers: Server[] = [
        {
          id: '1',
          userId: user?.id || '1',
          name: 'Production Server',
          hostname: 'prod-server-01',
          ipAddress: '192.168.1.100',
          os: 'Ubuntu 22.04',
          status: 'online',
          apiKey: 'mock-key-1',
          lastHeartbeat: new Date().toISOString(),
          tags: ['production', 'web'],
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString()
        }
      ];
      setServers(mockServers);
      
      // Load metrics for each server
      if (mockServers.length > 0) {
        for (const server of mockServers) {
          loadServerMetrics(server.id);
        }
      }
    } catch (error) {
      console.error("Failed to load servers:", error);
      setServers([]);
    } finally {
      setIsLoadingServers(false);
    }
  };

  const loadServerMetrics = async (serverId: string) => {
    try {
      // TODO: Replace with actual API call
      // const serverMetrics = await apiClient.get<Metric[]>(`/metrics/${serverId}/latest`);
      // setMetrics(prev => ({ ...prev, [serverId]: serverMetrics || [] }));
      
      // Mock metrics for now
      const mockMetrics: Metric[] = [
        {
          serverId,
          type: 'cpu_usage',
          value: 45.2,
          unit: '%',
          timestamp: new Date().toISOString()
        },
        {
          serverId,
          type: 'memory_usage',
          value: 68.5,
          unit: '%',
          timestamp: new Date().toISOString()
        }
      ];
      setMetrics(prev => ({ ...prev, [serverId]: mockMetrics }));
    } catch (error) {
      console.error(`Failed to load metrics for server ${serverId}:`, error);
      // Set empty array for servers without metrics
      setMetrics(prev => ({ ...prev, [serverId]: [] }));
    }
  };

  const handleRefresh = async () => {
    setIsRefreshing(true);
    await loadServers();
    setIsRefreshing(false);
  };

  const handleDeleteClick = (e: React.MouseEvent, server: Server) => {
    e.stopPropagation(); // Prevent navigation to server details
    setDeleteModal({ isOpen: true, server });
  };

  const handleDeleteConfirm = async () => {
    if (!deleteModal.server) return;
    
    try {
      setIsDeleting(true);
      // TODO: Replace with actual API call
      // await apiClient.delete(`/servers/${deleteModal.server.id}`);
      console.log('Deleting server:', deleteModal.server.id);
      setDeleteModal({ isOpen: false, server: null });
      await loadServers(); // Reload servers list
    } catch (error) {
      console.error("Failed to delete server:", error);
      alert("Failed to delete server. Please try again.");
    } finally {
      setIsDeleting(false);
    }
  };

  const handleDeleteCancel = () => {
    setDeleteModal({ isOpen: false, server: null });
  };

  const getMetricValue = (serverId: string, type: string): string => {
    const serverMetrics = metrics[serverId] || [];
    
    // Map display types to actual metric types
    let metricType = type;
    switch(type) {
      case 'cpu':
        metricType = 'cpu_temperature';
        break;
      case 'memory':
        metricType = 'memory_usage';
        break;
      case 'disk':
        metricType = 'disk_usage';
        break;
    }
    
    const metric = serverMetrics.find(m => m.type === metricType);
    if (!metric) return "N/A";
    
    // Format value based on type
    if (metricType === 'memory_usage' || metricType === 'disk_usage') {
      return `${Math.round(metric.value)}%`;
    }
    if (metricType === 'cpu_temperature') {
      return `${Math.round(metric.value)}°C`;
    }
    
    return `${metric.value}${metric.unit || ''}`;
  };

  const getAverageCpu = (): string => {
    if (servers.length === 0) return "N/A";
    
    let totalCpu = 0;
    let serverCount = 0;
    
    servers.forEach(server => {
      const serverMetrics = metrics[server.id] || [];
      const cpuMetric = serverMetrics.find(m => m.type === 'cpu_temperature');
      if (cpuMetric) {
        totalCpu += cpuMetric.value;
        serverCount++;
      }
    });
    
    if (serverCount === 0) return "N/A";
    return `${Math.round(totalCpu / serverCount)}°C`;
  };

  if (loading || !isAuthenticated) {
    return (
      <div className="min-h-screen bg-black flex items-center justify-center">
        <div className="text-white">Loading...</div>
      </div>
    );
  }

  return (
    <main className="min-h-screen bg-black text-white">
      <div className="absolute inset-0 bg-gradient-to-br from-blue-600/10 via-purple-600/10 to-pink-600/10" />
      
      <div className="relative z-10">
        {/* Page Header */}
        <div className="border-b border-white/10 bg-black/50 backdrop-blur-sm">
          <div className="container mx-auto px-6 py-6">
            <div className="flex items-center justify-between">
              <div>
                <h1 className="text-3xl font-bold mb-2">Dashboard</h1>
                <p className="text-gray-400">Welcome back, {user?.username}</p>
              </div>
              <div className="flex gap-4">
                <Button
                  variant="secondary"
                  onClick={handleRefresh}
                  disabled={isRefreshing}
                >
                  <RefreshCw className={`w-5 h-5 mr-2 ${isRefreshing ? 'animate-spin' : ''}`} />
                  Refresh
                </Button>
                <Button onClick={() => router.push("/dashboard/servers/new")}>
                  <Plus className="w-5 h-5 mr-2" />
                  Add Server
                </Button>
              </div>
            </div>
          </div>
        </div>

        {/* Stats Overview */}
        <div className="container mx-auto px-6 py-8">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
            {[
              { label: "Total Servers", value: servers.length, icon: ServerIcon, color: "blue" },
              { label: "Online", value: servers.filter(s => s.status === 'online').length, icon: Activity, color: "green" },
              { label: "Offline", value: servers.filter(s => s.status === 'offline').length, icon: Activity, color: "red" },
              { label: "Avg CPU", value: getAverageCpu(), icon: Cpu, color: "purple" },
            ].map((stat, i) => (
              <motion.div
                key={i}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.1 }}
              >
                <Card>
                  <CardContent>
                    <div className="flex items-center justify-between">
                      <div>
                        <p className="text-sm text-gray-400 mb-1">{stat.label}</p>
                        <p className="text-3xl font-bold">{stat.value}</p>
                      </div>
                      <div className={`w-12 h-12 bg-${stat.color}-500/20 rounded-xl flex items-center justify-center`}>
                        <stat.icon className={`w-6 h-6 text-${stat.color}-400`} />
                      </div>
                    </div>
                  </CardContent>
                </Card>
              </motion.div>
            ))}
          </div>

          {/* Servers List */}
          <div>
            <h2 className="text-2xl font-bold mb-6">Your Servers</h2>
            
            {isLoadingServers ? (
              <div className="text-center py-12">
                <div className="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
              </div>
            ) : servers.length === 0 ? (
              <Card>
                <CardContent className="text-center py-12">
                  <ServerIcon className="w-16 h-16 mx-auto mb-4 text-gray-600" />
                  <h3 className="text-xl font-bold mb-2">No servers yet</h3>
                  <p className="text-gray-400 mb-6">Add your first server to start monitoring</p>
                  <Button onClick={() => router.push("/dashboard/servers/new")}>
                    <Plus className="w-5 h-5 mr-2" />
                    Add Server
                  </Button>
                </CardContent>
              </Card>
            ) : (
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {servers.map((server, i) => (
                  <motion.div
                    key={server.id}
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: i * 0.1 }}
                    onClick={() => router.push(`/servers/${server.id}`)}
                    className="cursor-pointer"
                  >
                    <Card hover>
                      <CardHeader>
                        <div className="flex items-center justify-between">
                          <div>
                            <CardTitle>{server.name}</CardTitle>
                            <p className="text-sm text-gray-400 mt-1">{server.hostname}</p>
                          </div>
                          <div className="flex items-center gap-3">
                            <div className="flex items-center gap-2">
                              <div className={`w-3 h-3 rounded-full ${server.status === 'online' ? 'bg-green-500' : 'bg-red-500'} animate-pulse`} />
                              <span className="text-sm capitalize">{server.status}</span>
                            </div>
                            <button
                              onClick={(e) => handleDeleteClick(e, server)}
                              className="p-2 hover:bg-red-500/20 rounded-lg transition-colors group"
                              title="Delete server"
                            >
                              <Trash2 className="w-4 h-4 text-gray-400 group-hover:text-red-400" />
                            </button>
                          </div>
                        </div>
                      </CardHeader>
                      <CardContent>
                        <div className="grid grid-cols-3 gap-4">
                          <div>
                            <p className="text-xs text-gray-400 mb-1">CPU</p>
                            <p className="text-lg font-semibold">{getMetricValue(server.id, 'cpu')}</p>
                          </div>
                          <div>
                            <p className="text-xs text-gray-400 mb-1">Memory</p>
                            <p className="text-lg font-semibold">{getMetricValue(server.id, 'memory')}</p>
                          </div>
                          <div>
                            <p className="text-xs text-gray-400 mb-1">Disk</p>
                            <p className="text-lg font-semibold">{getMetricValue(server.id, 'disk')}</p>
                          </div>
                        </div>
                        {server.tags && server.tags.length > 0 && (
                          <div className="mt-4 flex flex-wrap gap-2">
                            {server.tags.map((tag, i) => (
                              <span key={i} className="px-2 py-1 bg-white/5 border border-white/10 rounded-lg text-xs">
                                {tag}
                              </span>
                            ))}
                          </div>
                        )}
                      </CardContent>
                    </Card>
                  </motion.div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Delete Confirmation Modal */}
        {deleteModal.isOpen && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
            <motion.div
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              className="bg-gray-900 border border-red-500/20 rounded-xl p-6 max-w-md w-full mx-4"
            >
              <div className="flex items-center gap-3 mb-4">
                <div className="w-12 h-12 bg-red-500/20 rounded-full flex items-center justify-center">
                  <Trash2 className="w-6 h-6 text-red-400" />
                </div>
                <div>
                  <h3 className="text-xl font-bold">Delete Server</h3>
                  <p className="text-sm text-gray-400">This action cannot be undone</p>
                </div>
              </div>
              
              <div className="bg-red-500/10 border border-red-500/20 rounded-lg p-4 mb-6">
                <p className="text-sm text-gray-300">
                  Are you sure you want to delete <span className="font-semibold text-white">{deleteModal.server?.name}</span>?
                </p>
                <p className="text-xs text-gray-400 mt-2">
                  All metrics and data associated with this server will be permanently deleted.
                </p>
              </div>

              <div className="flex gap-3">
                <Button
                  variant="secondary"
                  onClick={handleDeleteCancel}
                  className="flex-1"
                  disabled={isDeleting}
                >
                  Cancel
                </Button>
                <Button
                  onClick={handleDeleteConfirm}
                  className="flex-1 bg-red-500 hover:bg-red-600"
                  disabled={isDeleting}
                >
                  {isDeleting ? (
                    <>
                      <div className="w-4 h-4 border-2 border-white/20 border-t-white rounded-full animate-spin mr-2" />
                      Deleting...
                    </>
                  ) : (
                    <>
                      <Trash2 className="w-4 h-4 mr-2" />
                      Delete Server
                    </>
                  )}
                </Button>
              </div>
            </motion.div>
          </div>
        )}
      </div>
    </main>
  );
}
