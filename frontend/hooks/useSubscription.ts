import { useState, useEffect } from 'react';
import { usePathname } from 'next/navigation';
import { billingApi, Subscription } from '@/lib/billingApi';
import { useAuth } from '@/context/AuthContext';
import { logger } from '@/lib/telemetry/logger';

export function useSubscription() {
  const [subscription, setSubscription] = useState<Subscription | null>(null);
  const [loading, setLoading] = useState(true);
  const [hasPremium, setHasPremium] = useState(false);
  const { isAuthenticated } = useAuth();
  const pathname = usePathname();

  useEffect(() => {
    const loadSubscription = async () => {
      // Don't try to load subscription if user is not authenticated or on auth pages
      if (!isAuthenticated || pathname === '/login' || pathname === '/register') {
        setSubscription(null);
        setHasPremium(false);
        setLoading(false);
        return;
      }

      try {
        logger.debug('Loading subscription data');
        const sub = await billingApi.getCurrentSubscription();

        // Log the raw subscription data
        logger.info('Raw subscription data received', {
          subscription: sub,
          planType: sub?.planType,
          planName: sub?.planName,
          status: sub?.status,
        });

        setSubscription(sub);
        const isPremium = sub
          ? sub.planType > 0 || sub.planName === 'Pro' || sub.planName === 'Enterprise'
          : false;
        setHasPremium(isPremium);

        if (sub) {
          logger.info('Subscription loaded', {
            planType: sub.planType,
            planName: sub.planName,
            status: sub.status,
            hasPremium: isPremium,
          });
        } else {
          logger.debug('No active subscription found');
        }
      } catch (error) {
        // 401 is normal for subscription endpoint (user may not have subscription or token expired)
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const isAuthError = (error as any)?.response?.status === 401;
        if (isAuthError) {
          logger.debug('Subscription endpoint returned 401 (user may not have subscription)');
        } else {
          logger.error('Failed to load subscription', error as Error);
        }
        setSubscription(null);
        setHasPremium(false);
      } finally {
        setLoading(false);
      }
    };

    loadSubscription();
  }, [isAuthenticated, pathname]);

  return {
    subscription,
    loading,
    hasPremium,
    isPro: subscription?.planType === 1,
    isEnterprise: subscription?.planType === 2,
  };
}
