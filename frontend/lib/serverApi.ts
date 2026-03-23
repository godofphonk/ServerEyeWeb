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

  if (cachedServers && now - cacheTimestamp < CACHE_DURATION) {
    return cachedServers;
  }

  try {
    cachedServers = await apiClient.get<MonitoredServer[]>('/monitoredservers');
    cacheTimestamp = now;
    return cachedServers;
  } catch (error: any) {
    // For 401 errors, don't clear cache - let auth interceptor handle it
    if (error.response?.status === 401) {
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

// Helper function to chunk array for parallel processing
function chunkArray<T>(array: T[], chunkSize: number): T[][] {
  const chunks: T[][] = [];
  for (let i = 0; i < array.length; i += chunkSize) {
    chunks.push(array.slice(i, i + chunkSize));
  }
  return chunks;
}

// Get monitored servers with static info (optimized with chunked parallel requests)
export async function getServersWithStaticInfo(): Promise<
  Array<MonitoredServer & { staticInfo?: ServerStaticInfo }>
> {
  // Get monitored servers from cache
  const monitoredServers = await getCachedMonitoredServers();

  // Process servers in chunks to avoid overwhelming the backend
  // 5 concurrent requests at a time for optimal performance
  const CHUNK_SIZE = 5;
  const chunks = chunkArray(monitoredServers, CHUNK_SIZE);
  
  const allResults: Array<MonitoredServer & { staticInfo?: ServerStaticInfo }> = [];
  
  // Process each chunk sequentially, but requests within chunk are parallel
  for (const chunk of chunks) {
    const chunkResults = await Promise.allSettled(
      chunk.map(async server => {
        try {
          // Only load static info if serverKey exists
          if (!server.serverKey) {
            return {
              ...server,
            };
          }

          // Use cached version with deduplication
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

    // Check if any server errors occurred in this chunk
    const serverError = chunkResults.find(result => 
      result.status === 'rejected' && 
      result.reason?.response?.status >= 500
    );
    
    if (serverError && serverError.status === 'rejected') {
      throw serverError.reason;
    }

    // Collect successful results from this chunk
    const successfulResults = chunkResults
      .filter((result): result is PromiseFulfilledResult<any> => result.status === 'fulfilled')
      .map(result => result.value);
    
    allResults.push(...successfulResults);
  }

  return allResults;
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

  if (!server) {
    throw new Error(`Server not found for serverId: ${serverId}`);
  }

  // If serverKey exists, use it
  if (server.serverKey) {
    return server.serverKey;
  }

  // If no serverKey, try using serverId (for servers added via Telegram)
  return serverId;
}

// Get server static info with caching and deduplication
const staticInfoCache = new Map<string, { data: ServerStaticInfo; timestamp: number }>();
const STATIC_INFO_CACHE_DURATION = 5 * 60 * 1000; // 5 minutes

// In-flight requests map for deduplication
const inflightRequests = new Map<string, Promise<ServerStaticInfo>>();

export async function getServerStaticInfoCached(serverKey: string): Promise<ServerStaticInfo> {
  const cached = staticInfoCache.get(serverKey);
  const now = Date.now();

  // Return cached data if still valid
  if (cached && now - cached.timestamp < STATIC_INFO_CACHE_DURATION) {
    return cached.data;
  }

  // Check if request is already in-flight (deduplication)
  const inflightRequest = inflightRequests.get(serverKey);
  if (inflightRequest) {
    return inflightRequest;
  }

  // Create new request
  const requestPromise = getServerStaticInfo(serverKey)
    .then(data => {
      // Update cache
      staticInfoCache.set(serverKey, { data, timestamp: now });
      // Remove from in-flight
      inflightRequests.delete(serverKey);
      return data;
    })
    .catch(error => {
      // Remove from in-flight on error
      inflightRequests.delete(serverKey);
      throw error;
    });

  // Store in-flight request
  inflightRequests.set(serverKey, requestPromise);

  return requestPromise;
}

// Clear servers cache (useful after deletion)
export function clearServersCache() {
  cachedServers = null;
  cacheTimestamp = 0;
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
    return cached.data;
  }

  // Fetch fresh tiered data with optional granularity
  const url = granularity 
    ? `/servers/by-key/${serverKey}/metrics/tiered?start=${start}&end=${end}&granularity=${granularity}`
    : `/servers/by-key/${serverKey}/metrics/tiered?start=${start}&end=${end}`;
  
  const response = await apiClient.get<any>(url);

  // Fix status - if we have data points, status should be success
  const fixedResponse = {
    ...response,
    status: response.dataPoints && response.dataPoints.length > 0 ? 'success' : response.status
  };

  // Update cache
  metricsCache.set(cacheKey, { data: fixedResponse, timestamp: now });

  return fixedResponse;
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
    return cached.data;
  }

  // Fetch fresh data
  const response = await apiClient.get<any>(
    `/servers/by-key/${serverKey}/metrics?start=${startTime}&end=${endTime}&granularity=${granularity}`,
  );

  // Fix status - if we have data points, status should be success
  const fixedResponse = {
    ...response,
    status: response.dataPoints && response.dataPoints.length > 0 ? 'success' : response.status
  };

  // Update cache
  metricsCache.set(cacheKey, { data: fixedResponse, timestamp: now });

  return fixedResponse;
}

// Clear metrics cache
export function clearMetricsCache() {
  metricsCache.clear();
}

// Get metrics using new serverKey endpoint
export async function getServerMetrics(
  serverKey: string,
  startTime: string,
  endTime: string,
  granularity: string,
): Promise<any> {
  const params = new URLSearchParams({
    start: startTime,
    end: endTime,
    granularity: granularity,
  });

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
  const startTime = options?.start?.toISOString() || new Date(Date.now() - 60 * 60 * 1000).toISOString();
  const endTime = options?.end?.toISOString() || new Date().toISOString();
  const granularity = options?.granularity || 'minute';

  const [staticInfo, metrics] = await Promise.all([
    getServerStaticInfoCached(serverKey),
    getServerMetrics(serverKey, startTime, endTime, granularity),
  ]);

  return {
    staticInfo,
    metrics,
  };
}
