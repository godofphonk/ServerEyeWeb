'use client';

import { useAuth } from '@/context/AuthContext';

export interface PlanLimits {
  maxServers: number | undefined;
  currentServers: number | undefined;
  metricsRetentionDays: number | undefined;
  planName: string | undefined;
  planType: string | undefined;
  hasActiveSubscription: boolean | undefined;
  canAddServer: boolean;
}

/**
 * Hook for accessing plan limits from the authenticated user.
 * Returns undefined if user is not authenticated.
 */
export function usePlanLimits(): PlanLimits | undefined {
  const { user } = useAuth();

  if (!user) {
    return undefined;
  }

  // -1 indicates unlimited (Enterprise)
  const canAddServer =
    user.maxServers === -1 || (user.currentServers ?? 0) < (user.maxServers ?? 0);

  return {
    maxServers: user.maxServers,
    currentServers: user.currentServers,
    metricsRetentionDays: user.metricsRetentionDays,
    planName: user.planName,
    planType: user.planType,
    hasActiveSubscription: user.hasActiveSubscription,
    canAddServer,
  };
}
