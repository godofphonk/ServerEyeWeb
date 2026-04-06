import { useState, useCallback, useEffect } from 'react';
import { serverDiscoveryApi } from '@/lib/serverDiscoveryApi';
import { DiscoveredServersResponse, ImportServersResponse } from '@/types';
import { logger } from '@/lib/telemetry/logger';

interface UseTelegramServerDiscoveryOptions {
  autoTrigger?: boolean;
  triggerDelay?: number;
  forceShowModal?: boolean; // New option to force modal show
}

interface UseTelegramServerDiscoveryReturn {
  discovered: DiscoveredServersResponse | null;
  isLoading: boolean;
  error: string | null;
  shouldShowModal: boolean;
  discoverServers: () => Promise<DiscoveredServersResponse | null>;
  discoverServersForced: () => Promise<DiscoveredServersResponse | null>; // Force modal show
  importServers: (serverIds: string[]) => Promise<ImportServersResponse>;
  dismissModal: () => void;
  clearError: () => void;
  reset: () => void;
}

const DISCOVERY_STORAGE_KEY = 'telegram_server_discovery';
const LAST_DISCOVERY_KEY = 'last_telegram_discovery';
const DISMISSAL_KEY = 'telegram_discovery_dismissed';

export function useTelegramServerDiscovery({
  autoTrigger = false,
  triggerDelay = 1000,
  forceShowModal = false,
}: UseTelegramServerDiscoveryOptions = {}): UseTelegramServerDiscoveryReturn {
  const [discovered, setDiscovered] = useState<DiscoveredServersResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [shouldShowModal, setShouldShowModal] = useState(false);

  // Enterprise-level state management with localStorage persistence
  const getDiscoveryState = useCallback(() => {
    if (typeof window === 'undefined') return null;

    try {
      const stored = localStorage.getItem(DISCOVERY_STORAGE_KEY);
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  }, []);

  const setDiscoveryState = useCallback((state: any) => {
    if (typeof window === 'undefined') return;

    try {
      localStorage.setItem(DISCOVERY_STORAGE_KEY, JSON.stringify(state));
    } catch (error) {
      logger.warn('Failed to save discovery state', { error });
    }
  }, []);

  const getLastDiscoveryTime = useCallback(() => {
    if (typeof window === 'undefined') return 0;

    try {
      const stored = localStorage.getItem(LAST_DISCOVERY_KEY);
      return stored ? parseInt(stored, 10) : 0;
    } catch {
      return 0;
    }
  }, []);

  const setLastDiscoveryTime = useCallback(() => {
    if (typeof window === 'undefined') return;

    try {
      localStorage.setItem(LAST_DISCOVERY_KEY, Date.now().toString());
    } catch (error) {
      logger.warn('Failed to save discovery time', { error });
    }
  }, []);

  const isModalDismissed = useCallback(() => {
    // TODO: FIX - Remove this temporary bypass for testing
    // Proper behavior: Check if user has dismissed the modal to prevent spam
    // Always return false during testing to allow repeated discovery
    if (typeof window === 'undefined') return false;

    try {
      const dismissed = localStorage.getItem(DISMISSAL_KEY);
      return dismissed === 'true';
    } catch {
      return true;
    }
  }, []);

  const dismissModal = useCallback(() => {
    setShouldShowModal(false);
    // TODO: FIX - Re-enable modal dismissal persistence after testing
    // Proper behavior: Save dismissal state to prevent spam
    // if (typeof window !== 'undefined') {
    //   localStorage.setItem(DISMISSAL_KEY, 'true');
    // }
  }, []);

  const discoverServersForced = useCallback(async (): Promise<DiscoveredServersResponse | null> => {
    setShouldShowModal(true);
    setError(null);
    setIsLoading(false);
    return null;
  }, []);

  const discoverServers = useCallback(async (): Promise<DiscoveredServersResponse | null> => {
    if (forceShowModal) {
      setShouldShowModal(true);
      setError(null);
      setIsLoading(false);
      return null;
    }

    setIsLoading(true);
    setError(null);

    try {
      const result = await serverDiscoveryApi.findTelegramServers();

      setDiscovered(result);
      setLastDiscoveryTime();

      if (result && result.total_count > 0 && !isModalDismissed()) {
        setShouldShowModal(true);
      } else {
        setShouldShowModal(false);
      }

      setDiscoveryState({
        discovered: result,
        timestamp: Date.now(),
        showModal: shouldShowModal,
      });

      return result;
    } catch (err: any) {
      const errorMessage =
        err.response?.data?.message || err.message || 'Failed to discover Telegram servers';
      setError(errorMessage);
      logger.error('Telegram server discovery failed', err, { errorMessage });

      // Graceful fallback - don't show modal on error
      setShouldShowModal(false);
      return null;
    } finally {
      setIsLoading(false);
    }
  }, [isModalDismissed, forceShowModal, setDiscoveryState, setLastDiscoveryTime]);

  const importServers = useCallback(
    async (serverIds: string[]): Promise<ImportServersResponse> => {
      if (!discovered) {
        throw new Error('No discovered servers available for import');
      }

      setIsLoading(true);
      setError(null);

      try {
        const result = await serverDiscoveryApi.importServers(serverIds);

        if (result.failed_count > 0) {
          logger.warn('Some servers failed to import', {
            failedCount: result.failed_count,
            errors: result.errors,
          });
        }

        // TODO: FIX - For production, consider if we need to refresh discovery data
        // For now, update locally to reflect imported servers
        if (discovered) {
          const updatedDiscovered = {
            ...discovered,
            servers: discovered.servers.map(server => ({
              ...server,
              canImport: !serverIds.includes(server.server_id),
            })),
          };
          setDiscovered(updatedDiscovered);
          setDiscoveryState(updatedDiscovered);
        }

        // Refresh discovery data from server to get accurate state
        // This ensures we have the latest canImport status after import
        setTimeout(() => {
          discoverServers();
        }, 1000); // Wait 1 second for backend to process

        return result;
      } catch (err: any) {
        let errorMessage = err.response?.data?.message || err.message || 'Failed to import servers';

        // TODO: FIX - Improve error handling for production
        // Handle specific case where servers are already imported
        if (err.response?.status === 400 && err.response?.data?.errors) {
          const errors = err.response.data.errors;
          if (errors.every((error: string) => error.includes('already added'))) {
            errorMessage = 'Selected servers are already added to your account';
            // Refresh discovery data to show correct state
            setTimeout(() => {
              discoverServers();
            }, 1000);
          }
        }

        setError(errorMessage);
        logger.error('Telegram server import failed', err, {
          errorMessage,
          serverCount: serverIds.length,
        });
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    [discovered, setDiscoveryState],
  );

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  const reset = useCallback(() => {
    setDiscovered(null);
    setError(null);
    setShouldShowModal(false);
    setIsLoading(false);

    if (typeof window !== 'undefined') {
      localStorage.removeItem(DISCOVERY_STORAGE_KEY);
      localStorage.removeItem(DISMISSAL_KEY);
    }
  }, []);

  useEffect(() => {
    if (!autoTrigger) return;

    const triggerDiscovery = async () => {
      const telegramOAuthCompleted =
        typeof window !== 'undefined' ? sessionStorage.getItem('telegram_oauth_completed') : null;

      if (!telegramOAuthCompleted) {
        return;
      }

      // Don't trigger if already loading
      if (isLoading) return;

      // Don't trigger if modal already showing
      if (shouldShowModal) return;

      // Rate limiting: don't trigger more than once per hour (temporarily disabled for testing)
      const lastDiscovery = getLastDiscoveryTime();
      const oneHourAgo = Date.now() - 60 * 60 * 1000;

      // TODO: Re-enable rate limiting after testing: if (lastDiscovery > oneHourAgo) return;
      void lastDiscovery;
      void oneHourAgo;

      if (isModalDismissed()) {
        return;
      }

      // Clear the flag after using it
      if (typeof window !== 'undefined') {
        sessionStorage.removeItem('telegram_oauth_completed');
      }

      // Delayed trigger to allow page to stabilize
      setTimeout(() => {
        discoverServers();
      }, triggerDelay);
    };

    triggerDiscovery();
  }, [
    autoTrigger,
    triggerDelay,
    isLoading,
    shouldShowModal,
    discoverServers,
    getLastDiscoveryTime,
    isModalDismissed,
  ]);

  // Restore state from localStorage on mount
  useEffect(() => {
    const storedState = getDiscoveryState();
    if (storedState) {
      setDiscovered(storedState.discovered);

      // Only show modal if it was showing before and hasn't been dismissed
      if (storedState.showModal && !isModalDismissed()) {
        setShouldShowModal(true);
      }
    }
  }, [getDiscoveryState, isModalDismissed]);

  return {
    discovered,
    isLoading,
    error,
    shouldShowModal,
    discoverServers,
    discoverServersForced,
    importServers,
    dismissModal,
    clearError,
    reset,
  };
}
