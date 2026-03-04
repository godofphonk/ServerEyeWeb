'use client';

import React, { useState, useEffect, useRef, useCallback } from 'react';
import { apiClient } from '@/lib/api';
import {
  getServersWithStaticInfo,
  getServerMetrics,
  clearServersCache,
  clearMetricsCache,
} from '@/lib/serverApi';
import { motion } from 'framer-motion';
import {
  Activity,
  Cpu,
  HardDrive,
  Server as ServerIcon,
  Plus,
  RefreshCw,
  Trash2,
} from 'lucide-react';
import { useAuth } from '@/context/AuthContext';
import { useRouter } from 'next/navigation';
import { useToast } from '@/hooks/useToast';
import { hasUserAccess } from '@/lib/authUtils';
import {
  MonitoredServer,
  DashboardMetrics,
  HistoricalMetricsResponse,
  ServerStaticInfo,
} from '@/types';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { EmailVerificationBanner } from '@/components/auth/EmailVerificationBanner';

export default function DashboardPage() {
  // Use AuthContext for authentication
  const {
    user,
    isAuthenticated,
    loading: authLoading,
    isEmailVerified,
    refreshUserData,
  } = useAuth();
  const router = useRouter();
  const toast = useToast();
  
  // Защита от множественных редиректов
  const redirectAttempted = useRef(false);
  const oauthCallbackHandled = useRef(false);

  console.log('[Dashboard] Auth state:', {
    user: user?.email,
    isAuthenticated,
    isEmailVerified,
    authLoading,
  });

  const [servers, setServers] = useState<
    Array<MonitoredServer & { staticInfo?: ServerStaticInfo }>
  >([]);
  const [metrics, setMetrics] = useState<Record<string, DashboardMetrics | null>>({});
  const [isLoadingServers, setIsLoadingServers] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [deleteModal, setDeleteModal] = useState<{
    isOpen: boolean;
    server: (MonitoredServer & { staticInfo?: ServerStaticInfo }) | null;
  }>({
    isOpen: false,
    server: null,
  });
  const [isDeleting, setIsDeleting] = useState(false);
  const [loadingMetrics, setLoadingMetrics] = useState<Set<string>>(new Set());

  // Auto-login for development - CORS is fixed!
  const autoLoginAttempted = useRef(false);
  const loadServersCalled = useRef(false); // Защита от множественных вызовов

  // Handle OAuth callback parameters
  useEffect(() => {
    if (typeof window !== 'undefined' && !oauthCallbackHandled.current) {
      const urlParams = new URLSearchParams(window.location.search);
      const error = urlParams.get('error');
      
      if (error) {
        oauthCallbackHandled.current = true;
        
        // Show appropriate error message
        let errorMessage = 'OAuth authentication failed. Please try again.';
        if (error === 'access_denied') {
          errorMessage = 'OAuth access was denied. Please try again.';
        } else if (error === 'backend_error') {
          errorMessage = 'Failed to connect OAuth account. Please try again.';
        } else if (error === 'callback_exception') {
          errorMessage = 'OAuth callback failed. Please try again.';
        }
        
        toast.error('OAuth Connection Failed', errorMessage);
        
        // Clean URL
        window.history.replaceState({}, document.title, '/dashboard');
      } else if (urlParams.has('success') || urlParams.has('linked')) {
        oauthCallbackHandled.current = true;
        
        // Show success message
        toast.success('OAuth Account Connected', 'Your OAuth account has been successfully connected.');
        
        // Refresh user data to update connected accounts
        refreshUserData();
        
        // Clean URL
        window.history.replaceState({}, document.title, '/dashboard');
      }
    }
  }, []); // Убираем зависимости чтобы избежать бесконечного цикла

  const loadServerMetrics = useCallback(
    async (serverKey: string) => {
      if (loadingMetrics.has(serverKey)) {
        return;
      }

      setLoadingMetrics(prev => new Set(prev).add(serverKey));

      try {
        // Use new serverKey endpoint
        const response = await apiClient.get<any>(
          `/servers/by-key/${serverKey}/metrics?granularity=minute`,
        );

        // Transform API response to expected format
        const dashboardMetrics: DashboardMetrics = {
          current: {
            cpu: response.summary?.avgCpu || 0,
            memory: response.summary?.avgMemory || 0,
            disk: response.summary?.avgDisk || 0,
            network: response.dataPoints?.[response.dataPoints?.length - 1]?.network?.avg || 0,
            load: response.dataPoints?.[response.dataPoints?.length - 1]?.loadAverage?.avg || 0,
            temperature:
              response.dataPoints?.[response.dataPoints?.length - 1]?.temperature_details
                ?.cpu_temperature ||
              response.dataPoints?.[response.dataPoints?.length - 1]?.temperature?.avg ||
              0,
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
        };

        setMetrics(prev => ({ ...prev, [serverKey]: dashboardMetrics }));
        console.log(
          `[Dashboard] Successfully loaded metrics for server ${serverKey}`,
          dashboardMetrics,
        );
      } catch (error: any) {
        console.error(`Failed to load metrics for server ${serverKey}:`, error);
        // Set empty metrics to prevent continuous loading
        setMetrics(prev => ({ ...prev, [serverKey]: null }));
      } finally {
        setLoadingMetrics(prev => {
          const newSet = new Set(prev);
          newSet.delete(serverKey);
          return newSet;
        });
      }
    },
    [loadingMetrics],
  );

  const loadServers = useCallback(async () => {
    console.log('[Dashboard] loadServers called');

    try {
      setIsLoadingServers(true);
      console.log('[Dashboard] Loading servers...');

      // Load servers with static info in parallel
      console.log('[Dashboard] Calling getServersWithStaticInfo...');
      const serversWithStatic = await getServersWithStaticInfo();
      console.log('[Dashboard] Servers loaded:', serversWithStatic);
      setServers(serversWithStatic || []);

      // Load metrics for each server using serverKey
      if (serversWithStatic && serversWithStatic.length > 0) {
        for (const server of serversWithStatic) {
          // Use serverKey for metrics
          if (server.serverKey) {
            loadServerMetrics(server.serverKey);
          }
        }
      }
    } catch (error) {
      console.error('Failed to load servers:', error);
      setServers([]);
    } finally {
      setIsLoadingServers(false);
      console.log('[Dashboard] Loading servers finished');
    }
  }, []); // Empty dependencies - function is stable

  useEffect(() => {
    console.log('[Dashboard] useEffect triggered:', {
      authLoading,
      isAuthenticated,
      user: !!user,
      userId: user?.id,
      redirectAttempted: redirectAttempted.current,
      loadServersCalled: loadServersCalled.current
    });
    
    if (!authLoading && !isAuthenticated && !redirectAttempted.current) {
      console.log('[Dashboard] User not authenticated, redirecting to login');
      redirectAttempted.current = true;
      setIsLoadingServers(false);
      router.push('/login');
    } else if (!authLoading && isAuthenticated && !redirectAttempted.current) {
      // Проверяем доступ с учетом OAuth пользователей
      const userHasAccess = hasUserAccess(user, isEmailVerified);
      
      if (!userHasAccess) {
        console.log('[Dashboard] Email not verified, redirecting to verify-email');
        redirectAttempted.current = true;
        setIsLoadingServers(false);
        router.push('/verify-email');
      } else if (!loadServersCalled.current) {
        console.log('[Dashboard] Loading servers...');
        redirectAttempted.current = false;
        loadServersCalled.current = true;
        loadServers();
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAuthenticated, authLoading, isEmailVerified, user, router]); // Добавили isEmailVerified и user

  const handleRefresh = async () => {
    setIsRefreshing(true);
    setIsLoadingServers(false);
    loadServersCalled.current = false; // Сбрасываем флаг для повторной загрузки

    // Clear caches and reload
    clearServersCache();
    clearMetricsCache();
    await loadServers();

    setIsRefreshing(false);
  };

  const handleRefreshMetrics = async (serverId: string) => {
    await loadServerMetrics(serverId);
  };

  const handleDeleteClick = (
    e: React.MouseEvent,
    server: MonitoredServer & { staticInfo?: ServerStaticInfo },
  ) => {
    e.stopPropagation(); // Prevent navigation to server details
    setDeleteModal({ isOpen: true, server });
  };

  const handleDeleteConfirm = async () => {
    if (!deleteModal.server) return;

    try {
      setIsDeleting(true);
      await apiClient.delete(`/monitoredservers/${deleteModal.server.id}`);

      // Clear all caches and reload servers list
      clearServersCache();
      clearMetricsCache();
      await loadServers();

      toast.success(
        'Server Deleted',
        `Server ${deleteModal.server.hostname} has been successfully deleted`,
      );

      setDeleteModal({ isOpen: false, server: null });
    } catch (error: any) {
      console.error('Failed to delete server:', error);
      const errorMessage =
        error?.response?.data?.message || error?.message || 'Unknown error occurred';

      toast.error('Delete Failed', `Failed to delete server: ${errorMessage}`);
    } finally {
      setIsDeleting(false);
    }
  };

  const handleDeleteCancel = () => {
    setDeleteModal({ isOpen: false, server: null });
  };

  const getMetricValue = (serverId: string, type: 'cpu' | 'memory' | 'disk'): string => {
    const dashboardMetrics = metrics[serverId];

    // Check if metrics failed to load
    if (dashboardMetrics === null) {
      return 'Error';
    }

    if (!dashboardMetrics?.current) return 'N/A';

    const value = dashboardMetrics.current[type];

    switch (type) {
      case 'cpu':
        return `${Math.round(value)}%`;
      case 'memory':
      case 'disk':
        return `${Math.round(value)}%`;
      default:
        return 'N/A';
    }
  };

  const getAverageCpu = (): string => {
    if (servers.length === 0) return 'N/A';

    let totalCpu = 0;
    let serverCount = 0;

    servers.forEach(server => {
      const dashboardMetrics = metrics[server.serverId];
      if (dashboardMetrics?.current?.cpu) {
        totalCpu += dashboardMetrics.current.cpu;
        serverCount++;
      }
    });

    if (serverCount === 0) return 'N/A';
    return `${Math.round(totalCpu / serverCount)}%`;
  };

  if (authLoading || (!isAuthenticated && !authLoading)) {
    return (
      <div className='min-h-screen bg-black flex items-center justify-center'>
        <div className='text-white'>{authLoading ? 'Loading...' : 'Redirecting...'}</div>
      </div>
    );
  }

  return (
    <main className='min-h-screen bg-black text-white'>
      <div className='absolute inset-0 bg-gradient-to-br from-blue-600/10 via-purple-600/10 to-pink-600/10' />

      <div className='relative z-10'>
        {/* Page Header */}
        <div className='border-b border-white/10 bg-black/50 backdrop-blur-sm'>
          <div className='container mx-auto px-6 py-6'>
            <div className='flex items-center justify-between'>
              <div>
                <h1 className='text-3xl font-bold mb-2'>Dashboard</h1>
                <p className='text-gray-400'>Welcome back, {user?.username || 'User'}</p>
              </div>
              <div className='flex gap-4'>
                <Button variant='secondary' onClick={handleRefresh} disabled={isRefreshing}>
                  <RefreshCw className={`w-5 h-5 mr-2 ${isRefreshing ? 'animate-spin' : ''}`} />
                  Refresh
                </Button>
                <Button onClick={() => router.push('/dashboard/servers/new')}>
                  <Plus className='w-5 h-5 mr-2' />
                  Add Server
                </Button>
              </div>
            </div>
          </div>
        </div>

        {/* Email Verification Banner */}
        <div className='container mx-auto px-6 pt-6'>
          {!isEmailVerified && user?.email && (
            <EmailVerificationBanner
              email={user.email}
              onVerified={async () => {
                console.log('[Dashboard] Email verification completed - refreshing user data');
                try {
                  await refreshUserData();
                } catch (error) {
                  console.log('[Dashboard] Failed to refresh user data:', error);
                  // Fallback to page reload
                  window.location.reload();
                }
              }}
            />
          )}
        </div>

        {/* Stats Overview */}
        <div className='container mx-auto px-6 py-8'>
          <div className='grid grid-cols-1 md:grid-cols-4 gap-6 mb-8'>
            {[
              { label: 'Total Servers', value: servers.length, icon: ServerIcon, color: 'blue' },
              {
                label: 'Active',
                value: servers.filter(s => s.isActive).length,
                icon: Activity,
                color: 'green',
              },
              {
                label: 'Inactive',
                value: servers.filter(s => !s.isActive).length,
                icon: Activity,
                color: 'red',
              },
              { label: 'Avg CPU', value: getAverageCpu(), icon: Cpu, color: 'purple' },
            ].map((stat, i) => (
              <motion.div
                key={i}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.1 }}
              >
                <Card>
                  <CardContent>
                    <div className='flex items-center justify-between'>
                      <div>
                        <p className='text-sm text-gray-400 mb-1'>{stat.label}</p>
                        <p className='text-3xl font-bold'>{stat.value}</p>
                      </div>
                      <div
                        className={`w-12 h-12 bg-${stat.color}-500/20 rounded-xl flex items-center justify-center`}
                      >
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
            <h2 className='text-2xl font-bold mb-6'>Your Servers</h2>

            {isLoadingServers ? (
              <div className='text-center py-12'>
                <div className='inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500'></div>
              </div>
            ) : servers.length === 0 ? (
              <Card>
                <CardContent className='text-center py-12'>
                  <ServerIcon className='w-16 h-16 mx-auto mb-4 text-gray-600' />
                  <h3 className='text-xl font-bold mb-2'>No servers yet</h3>
                  <p className='text-gray-400 mb-6'>Add your first server to start monitoring</p>
                  <Button onClick={() => router.push('/dashboard/servers/new')}>
                    <Plus className='w-5 h-5 mr-2' />
                    Add Server
                  </Button>
                </CardContent>
              </Card>
            ) : (
              <div className='grid grid-cols-1 lg:grid-cols-2 gap-6'>
                {servers.map((server, i) => (
                  <motion.div
                    key={server.serverId}
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ delay: i * 0.1 }}
                    onClick={() => router.push(`/servers/${server.serverId}`)}
                    className='cursor-pointer'
                  >
                    <Card hover>
                      <CardHeader>
                        <div className='flex items-center justify-between'>
                          <div>
                            <CardTitle>
                              {server.staticInfo?.hostname ||
                                server.serverName ||
                                server.hostname ||
                                server.serverId}
                            </CardTitle>
                            <p className='text-sm text-gray-400 mt-1'>
                              {server.staticInfo?.operating_system ||
                                server.operatingSystem ||
                                'Unknown OS'}
                            </p>
                            {server.staticInfo && (
                              <p className='text-xs text-gray-500 mt-1'>
                                {server.staticInfo.cpu_info?.cores || 'N/A'} cores /{' '}
                                {server.staticInfo.memory_info?.total_gb || 'N/A'}GB RAM
                              </p>
                            )}
                          </div>
                          <div className='flex items-center gap-3'>
                            <div className='flex items-center gap-2'>
                              <div
                                className={`w-3 h-3 rounded-full ${server.isActive ? 'bg-green-500' : 'bg-red-500'} animate-pulse`}
                              />
                              <span className='text-sm capitalize'>
                                {server.isActive ? 'active' : 'inactive'}
                              </span>
                            </div>
                            <button
                              onClick={e => handleDeleteClick(e, server)}
                              className='p-2 hover:bg-red-500/20 rounded-lg transition-colors group'
                              title='Delete server'
                            >
                              <Trash2 className='w-4 h-4 text-gray-400 group-hover:text-red-400' />
                            </button>
                          </div>
                        </div>
                      </CardHeader>
                      <CardContent>
                        <div className='grid grid-cols-3 gap-4'>
                          <div>
                            <p className='text-xs text-gray-400 mb-1'>CPU</p>
                            <p
                              className={`text-lg font-semibold ${metrics[server.serverKey] === null ? 'text-red-400' : ''}`}
                            >
                              {getMetricValue(server.serverKey, 'cpu')}
                            </p>
                          </div>
                          <div>
                            <p className='text-xs text-gray-400 mb-1'>Memory</p>
                            <p
                              className={`text-lg font-semibold ${metrics[server.serverKey] === null ? 'text-red-400' : ''}`}
                            >
                              {getMetricValue(server.serverKey, 'memory')}
                            </p>
                          </div>
                          <div>
                            <p className='text-xs text-gray-400 mb-1'>Disk</p>
                            <p
                              className={`text-lg font-semibold ${metrics[server.serverKey] === null ? 'text-red-400' : ''}`}
                            >
                              {getMetricValue(server.serverKey, 'disk')}
                            </p>
                          </div>
                        </div>
                        {metrics[server.serverKey] === null && (
                          <div className='mt-2 text-xs text-red-400'>
                            Metrics unavailable - server may be offline
                          </div>
                        )}
                        {!server.serverKey && (
                          <div className='mt-2 text-xs text-yellow-400'>
                            Server key unavailable - metrics disabled
                          </div>
                        )}
                        <div className='mt-4 flex items-center justify-between text-xs text-gray-400'>
                          <span>Access: {server.accessLevel}</span>
                          <div className='flex items-center gap-2'>
                            <span>Last seen: {new Date(server.lastSeen).toLocaleString()}</span>
                            <button
                              onClick={e => {
                                e.stopPropagation();
                                if (server.serverKey) {
                                  handleRefreshMetrics(server.serverKey);
                                }
                              }}
                              className='p-1 rounded hover:bg-gray-700 transition-colors'
                              title='Обновить метрики'
                            >
                              <RefreshCw className='w-3 h-3 text-gray-400 hover:text-blue-400' />
                            </button>
                          </div>
                        </div>
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
          <div className='fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm'>
            <motion.div
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              className='bg-gray-900 border border-red-500/20 rounded-xl p-6 max-w-md w-full mx-4'
            >
              <div className='flex items-center gap-3 mb-4'>
                <div className='w-12 h-12 bg-red-500/20 rounded-full flex items-center justify-center'>
                  <Trash2 className='w-6 h-6 text-red-400' />
                </div>
                <div>
                  <h3 className='text-xl font-bold'>Delete Server</h3>
                  <p className='text-sm text-gray-400'>This action cannot be undone</p>
                </div>
              </div>

              <div className='bg-red-500/10 border border-red-500/20 rounded-lg p-4 mb-6'>
                <p className='text-sm text-gray-300'>
                  Are you sure you want to delete{' '}
                  <span className='font-semibold text-white'>{deleteModal.server?.hostname}</span>?
                </p>
                <p className='text-xs text-gray-400 mt-2'>
                  All metrics and data associated with this server will be permanently deleted.
                </p>
              </div>

              <div className='flex gap-3'>
                <Button
                  variant='secondary'
                  onClick={handleDeleteCancel}
                  className='flex-1'
                  disabled={isDeleting}
                >
                  Cancel
                </Button>
                <Button
                  onClick={handleDeleteConfirm}
                  className='flex-1 bg-red-500 hover:bg-red-600'
                  disabled={isDeleting}
                >
                  {isDeleting ? (
                    <>
                      <div className='w-4 h-4 border-2 border-white/20 border-t-white rounded-full animate-spin mr-2' />
                      Deleting...
                    </>
                  ) : (
                    <>
                      <Trash2 className='w-4 h-4 mr-2' />
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
