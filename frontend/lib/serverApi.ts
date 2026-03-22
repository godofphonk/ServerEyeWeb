import { apiClient } from './api';
import { fetchWithRetry } from './retryUtils';
import { ServerStaticInfo, MonitoredServer, MetricsResponse } from '@/types';

// Cache for monitored servers
let cachedServers: MonitoredServer[] | null = null;
let cacheTimestamp = 0;
const CACHE_DURATION = 30000; // 30 seconds

// Get cached monitored servers or fetch new ones
export async function getCachedMonitoredServers(): Promise<MonitoredServer[]> {
  const now = Date.now();

  console.log('[ServerAPI] getCachedMonitoredServers called');

  if (cachedServers && now - cacheTimestamp < CACHE_DURATION) {
    console.log('[ServerAPI] Returning cached servers:', cachedServers);
    return cachedServers;
  }

  console.log('[ServerAPI] Fetching servers from API...');
  try {
    cachedServers = await apiClient.get<MonitoredServer[]>('/monitoredservers');
    console.log('[ServerAPI] Servers fetched:', cachedServers);
    cacheTimestamp = now;
    return cachedServers;
  } catch (error: any) {
    console.error('[ServerAPI] Failed to fetch servers:', error);
    
    // For 401 errors, don't clear cache - let auth interceptor handle it
    if (error.response?.status === 401) {
      console.log('[ServerAPI] 401 error, preserving cache and letting auth interceptor handle');
      throw error; // Re-throw to let caller handle it
    }
    
    // For other errors, clear cache and return empty array
    cachedServers = null;
    cacheTimestamp = 0;
    throw error;
  }
}

// Static server info endpoint
export async function getServerStaticInfo(serverKey: string): Promise<ServerStaticInfo> {
  try {
    const response = await fetchWithRetry(
      () => apiClient.get<ServerStaticInfo>(`/servers/by-key/${serverKey}/static-info`),
      {
        maxRetries: 2,
        initialDelay: 1000,
      }
    );
    return response;
  } catch (error: any) {
    console.error(`[ServerAPI] Failed to get static info for ${serverKey}:`, error);
    
    // For 401 errors, let auth interceptor handle it
    if (error.response?.status === 401) {
      throw error;
    }
    
    // For server errors (500, 502, 503, 504), rethrow the original error to preserve response status
    if (error.response?.status >= 500) {
      throw error;
    }
    
    throw new Error(`Failed to load server static info: ${error.message}`);
  }
}

// Get monitored servers with static info
export async function getServersWithStaticInfo(): Promise<
  Array<MonitoredServer & { staticInfo?: ServerStaticInfo }>
> {
  // Get monitored servers from cache
  const monitoredServers = await getCachedMonitoredServers();

  // For each server, use the ServerKey from database directly
  const serversWithStatic = await Promise.allSettled(
    monitoredServers.map(async server => {
      try {
        // Only load static info if serverKey exists
        if (!server.serverKey) {
          return {
            ...server,
          };
        }

        const staticInfo = await getServerStaticInfoCached(server.serverKey);

        return {
          ...server,
          staticInfo,
        };
      } catch (error: any) {
        console.error(`Failed to load static info for server ${server.serverId}:`, error);
        
        // For server errors (500, 502, 503, 504), rethrow to trigger retry at higher level
        if (error.response?.status >= 500) {
          throw error;
        }
        
        // For wrapped errors, check if they contain server error info
        if (error.message?.includes('Request failed with status code 5')) {
          // Extract the original error if it's wrapped
          const statusMatch = error.message.match(/status code (\d{3})/);
          const statusCode = statusMatch ? parseInt(statusMatch[1]) : 0;
          if (statusCode >= 500) {
            // Create a mock error with response status for retry mechanism
            const mockError = new Error(error.message);
            (mockError as any).response = { status: statusCode };
            throw mockError;
          }
        }
        
        // For other errors, return server without static info
        return {
          ...server,
          staticInfo: undefined,
        };
      }
    }),
  );

  // Check if any server errors occurred and rethrow the first one
  const serverError = serversWithStatic.find(result => 
    result.status === 'rejected' && 
    result.reason?.response?.status >= 500
  );
  
  if (serverError && serverError.status === 'rejected') {
    throw serverError.reason;
  }

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
  const servers = await getCachedMonitoredServers();
  const server = servers.find(s => s.serverId === serverId);

  if (!server || !server.serverKey) {
    throw new Error(`Server key not found for serverId: ${serverId}`);
  }

  return server.serverKey;
}

// Get server static info with caching
const staticInfoCache = new Map<string, { data: ServerStaticInfo; timestamp: number }>();
const STATIC_INFO_CACHE_DURATION = 5 * 60 * 1000; // 5 minutes

export async function getServerStaticInfoCached(serverKey: string): Promise<ServerStaticInfo> {
  const cached = staticInfoCache.get(serverKey);
  const now = Date.now();

  // Return cached data if still valid
  if (cached && now - cached.timestamp < STATIC_INFO_CACHE_DURATION) {
    return cached.data;
  }

  // Fetch fresh data
  const data = await getServerStaticInfo(serverKey);

  // Update cache
  staticInfoCache.set(serverKey, { data, timestamp: now });

  return data;
}

// Clear servers cache (useful after deletion)
export function clearServersCache() {
  cachedServers = null;
  cacheTimestamp = 0;
  console.log('[ServerAPI] Servers cache cleared');
}

// Clear static info cache (useful for manual refresh)
export function clearStaticInfoCache(serverKey?: string) {
  if (serverKey) {
    staticInfoCache.delete(serverKey);
  } else {
    staticInfoCache.clear();
  }
}

// Cache for metrics
const metricsCache = new Map<string, { data: any; timestamp: number }>();
const METRICS_CACHE_DURATION = 10000; // 10 seconds for metrics

// Get cached tiered metrics for graphs (1 hour default)
export async function getCachedTieredMetrics(
  serverKey: string,
  startTime?: string,
  endTime?: string,
  granularity?: string,
): Promise<any> {
  // Default to last 1 hour if not provided
  const end = endTime || new Date().toISOString();
  const start = startTime || new Date(Date.now() - 60 * 60 * 1000).toISOString();
  
  const cacheKey = `tiered-${serverKey}-${start}-${end}${granularity ? `-${granularity}` : ''}`;
  const cached = metricsCache.get(cacheKey);
  const now = Date.now();

  // Return cached data if still valid
  if (cached && now - cached.timestamp < METRICS_CACHE_DURATION) {
    console.log(
      `[TieredMetricsCache] Using cached data for ${serverKey}, points: ${cached.data.dataPoints?.length || 0}`,
    );
    return cached.data;
  }

  console.log(`[TieredMetricsCache] Fetching fresh tiered data for ${serverKey}`);

  // Fetch fresh tiered data with optional granularity
  const url = granularity 
    ? `/servers/by-key/${serverKey}/metrics/tiered?start=${start}&end=${end}&granularity=${granularity}`
    : `/servers/by-key/${serverKey}/metrics/tiered?start=${start}&end=${end}`;
  
  const response = await apiClient.get<any>(url);

  console.log(
    `[TieredMetricsCache] Response: ${response.dataPoints?.length || 0} points, granularity: ${response.granularity}`,
  );

  // Update cache
  metricsCache.set(cacheKey, { data: response, timestamp: now });

  return response;
}

// Get cached metrics or fetch new ones
export async function getCachedMetrics(
  serverKey: string,
  startTime: string,
  endTime: string,
  granularity: string,
): Promise<any> {
  const cacheKey = `${serverKey}-${startTime}-${endTime}-${granularity}`;
  const cached = metricsCache.get(cacheKey);
  const now = Date.now();

  // Return cached data if still valid
  if (cached && now - cached.timestamp < METRICS_CACHE_DURATION) {
    console.log(
      `[MetricsCache] Using cached data for ${serverKey}, points: ${cached.data.dataPoints?.length || 0}`,
    );
    return cached.data;
  }

  console.log(`[MetricsCache] Fetching fresh data for ${serverKey}`);

  // Fetch fresh data
  const response = await apiClient.get<any>(
    `/servers/by-key/${serverKey}/metrics?start=${startTime}&end=${endTime}&granularity=${granularity}`,
  );

  console.log(
    `[MetricsCache] Response: ${response.dataPoints?.length || 0} points, status: ${response.status}`,
  );

  // Update cache
  metricsCache.set(cacheKey, { data: response, timestamp: now });

  return response;
}

// Clear metrics cache
export function clearMetricsCache() {
  metricsCache.clear();
}

// Get metrics using new serverKey endpoint
export async function getServerMetrics(
  serverKey: string,
  options?: {
    start?: Date;
    end?: Date;
    granularity?: string;
  },
): Promise<MetricsResponse> {
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
    `/servers/by-key/${serverKey}/metrics?${params.toString()}`,
  );

  return response;
}

// Parallel request for both static info and metrics
export async function getServerCompleteData(
  serverKey: string,
  options?: {
    start?: Date;
    end?: Date;
    granularity?: string;
  },
) {
  const [staticInfo, metrics] = await Promise.all([
    getServerStaticInfoCached(serverKey),
    getServerMetrics(serverKey, options),
  ]);

  return {
    staticInfo,
    metrics,
  };
}
