import { useState, useCallback } from 'react';
import { serverDiscoveryApi } from '@/lib/serverDiscoveryApi';
import { DiscoveredServersResponse, AxiosApiError } from '@/types';

export function useServerDiscovery() {
  const [discovered, setDiscovered] = useState<DiscoveredServersResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const checkForServers = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const result = await serverDiscoveryApi.findTelegramServers();
      setDiscovered(result);
      return result;
    } catch (err: unknown) {
      const errorMessage =
        (err as AxiosApiError).response?.data?.message ||
        (err as AxiosApiError).message ||
        'Failed to check for servers';
      setError(errorMessage);
      return null;
    } finally {
      setLoading(false);
    }
  }, []);

  const importServers = useCallback(async (serverIds: string[]) => {
    setLoading(true);
    setError(null);

    try {
      const result = await serverDiscoveryApi.importServers(serverIds);

      if (result.failed_count > 0) {
        // some servers failed to import
      }

      return result;
    } catch (err: unknown) {
      const errorMessage =
        (err as AxiosApiError).response?.data?.message ||
        (err as AxiosApiError).message ||
        'Failed to import servers';
      setError(errorMessage);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  const clearDiscovered = useCallback(() => {
    setDiscovered(null);
  }, []);

  return {
    discovered,
    loading,
    error,
    checkForServers,
    importServers,
    clearError,
    clearDiscovered,
  };
}
