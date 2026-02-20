import { apiClient } from './api';
import { ServerStaticInfo, MonitoredServer, MetricsResponse } from '@/types';

// Static server info endpoint
export async function getServerStaticInfo(serverKey: string): Promise<ServerStaticInfo> {
  console.log(`[StaticInfo] Fetching static info for serverKey: ${serverKey}`);
  const response = await apiClient.get<ServerStaticInfo>(`/servers/by-key/${serverKey}/static-info`);
  return response;
}

// Get monitored servers with static info
export async function getServersWithStaticInfo(): Promise<Array<MonitoredServer & { staticInfo?: ServerStaticInfo }>> {
  // First get monitored servers to get server keys/IDs
  const monitoredServers = await apiClient.get<MonitoredServer[]>('/monitoredservers');
  
  // For each server, use the ServerKey from database directly
  const serversWithStatic = await Promise.allSettled(
    monitoredServers.map(async (server) => {
      try {
        // Use ServerKey from database directly, fallback to serverId if needed
        const serverKey = server.serverKey || server.serverId;
        console.log(`[ServersWithStatic] Server: ${server.serverId}, serverKey: ${server.serverKey}, using: ${serverKey}`);
        const staticInfo = await getServerStaticInfo(serverKey);
        
        return {
          ...server,
          staticInfo,
        };
      } catch (error) {
        console.error(`Failed to load static info for server ${server.serverId}:`, error);
        // Return server without static info if endpoint fails
        return {
          ...server,
          staticInfo: undefined,
        };
      }
    })
  );
  
  // Filter out rejected promises and return successful results
  return serversWithStatic
    .filter((result): result is PromiseFulfilledResult<any> => result.status === 'fulfilled')
    .map(result => result.value);
}

// Helper function to convert serverKey for Go API
export function convertToGoApiKey(serverKey: string): string {
  // If serverKey starts with 'srv_', extract the part after it
  if (serverKey.startsWith('srv_')) {
    return serverKey.substring(4); // Remove 'srv_' prefix
  }
  return serverKey;
}

// Helper function to get serverKey from serverId
export async function getServerKey(serverId: string): Promise<string> {
  const servers = await apiClient.get<MonitoredServer[]>('/monitoredservers');
  const server = servers.find(s => s.serverId === serverId);
  
  if (!server || !server.serverKey) {
    throw new Error(`Server key not found for serverId: ${serverId}`);
  }
  
  return server.serverKey;
}

// Get server static info with caching
const staticInfoCache = new Map<string, { data: ServerStaticInfo; timestamp: number }>();
const CACHE_DURATION = 5 * 60 * 1000; // 5 minutes

export async function getServerStaticInfoCached(serverKey: string): Promise<ServerStaticInfo> {
  const cached = staticInfoCache.get(serverKey);
  const now = Date.now();
  
  // Return cached data if still valid
  if (cached && (now - cached.timestamp) < CACHE_DURATION) {
    console.log(`[StaticInfo] Using cached data for ${serverKey}`);
    return cached.data;
  }
  
  // Fetch fresh data
  console.log(`[StaticInfo] Fetching fresh data for ${serverKey}`);
  const data = await getServerStaticInfo(serverKey);
  
  // Update cache
  staticInfoCache.set(serverKey, { data, timestamp: now });
  
  return data;
}

// Clear static info cache (useful for manual refresh)
export function clearStaticInfoCache(serverKey?: string) {
  if (serverKey) {
    staticInfoCache.delete(serverKey);
    console.log(`[StaticInfo] Cleared cache for ${serverKey}`);
  } else {
    staticInfoCache.clear();
    console.log(`[StaticInfo] Cleared all cache`);
  }
}

// Get metrics using new serverKey endpoint
export async function getServerMetrics(serverKey: string, options?: {
  start?: Date;
  end?: Date;
  granularity?: string;
}): Promise<MetricsResponse> {
  // Default time range (last 5 minutes)
  const end = options?.end || new Date();
  const start = options?.start || new Date(end.getTime() - 5 * 60 * 1000);
  const granularity = options?.granularity || 'minute';
  
  const params = new URLSearchParams({
    start: start.toISOString(),
    end: end.toISOString(),
    granularity: granularity,
  });
  
  console.log(`[ServerMetrics] Fetching metrics for serverKey: ${serverKey}`);
  const response = await apiClient.get<any>(
    `/servers/by-key/${serverKey}/metrics?${params.toString()}`
  );
  
  return response;
}

// Parallel request for both static info and metrics
export async function getServerCompleteData(serverKey: string, options?: {
  start?: Date;
  end?: Date;
  granularity?: string;
}) {
  const [staticInfo, metrics] = await Promise.all([
    getServerStaticInfoCached(serverKey),
    getServerMetrics(serverKey, options)
  ]);
  
  return {
    staticInfo,
    metrics,
  };
}
