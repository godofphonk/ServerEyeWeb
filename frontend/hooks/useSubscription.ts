import { useState, useEffect } from 'react';
import { useAuth } from '@/context/AuthContext';
import { billingApi, Subscription } from '@/lib/billingApi';

export function useSubscription() {
  const [subscription, setSubscription] = useState<Subscription | null>(null);
  const [loading, setLoading] = useState(true);
  const [hasPremium, setHasPremium] = useState(false);
  const { isAuthenticated } = useAuth();

  useEffect(() => {
    const loadSubscription = async () => {
      // Don't try to load subscription if user is not authenticated
      if (!isAuthenticated) {
        setSubscription(null);
        setHasPremium(false);
        setLoading(false);
        return;
      }

      try {
        const sub = await billingApi.getCurrentSubscription();
        setSubscription(sub);
        setHasPremium(sub ? sub.planType > 0 : false); // 0 = Free, >0 = Paid
      } catch (error) {
        console.error('Failed to load subscription:', error);
        setSubscription(null);
        setHasPremium(false);
      } finally {
        setLoading(false);
      }
    };

    loadSubscription();
  }, [isAuthenticated]);

  return {
    subscription,
    loading,
    hasPremium,
    isPro: subscription?.planType === 1,
    isEnterprise: subscription?.planType === 2,
  };
}
