'use client';

import React from 'react';
import { Crown, Star } from 'lucide-react';
import { useSubscription } from '@/hooks/useSubscription';

interface SubscriptionBadgeProps {
  className?: string;
}

export function SubscriptionBadge({ className = '' }: SubscriptionBadgeProps) {
  const { subscription, loading, isPro, hasPremium } = useSubscription();

  if (loading) {
    return (
      <div
        className={`inline-flex items-center px-3 py-1 rounded-full text-sm bg-gray-100 text-gray-600 ${className}`}
      >
        <div className='w-4 h-4 mr-2 animate-spin rounded-full border-2 border-gray-300 border-t-gray-600'></div>
        Loading...
      </div>
    );
  }

  if (!subscription || subscription.status !== 1) {
    return (
      <div
        className={`inline-flex items-center px-3 py-1 rounded-full text-sm bg-gray-100 text-gray-600 ${className}`}
      >
        <span className='w-4 h-4 mr-2'>🔓</span>
        Free Plan
      </div>
    );
  }

  if (isPro) {
    return (
      <div
        className={`inline-flex items-center px-3 py-1 rounded-full text-sm bg-gradient-to-r from-blue-500 to-purple-600 text-white font-medium ${className}`}
      >
        <Crown className='w-4 h-4 mr-2' />
        Pro Plan
      </div>
    );
  }

  return (
    <div
      className={`inline-flex items-center px-3 py-1 rounded-full text-sm bg-gradient-to-r from-yellow-400 to-orange-500 text-white font-medium ${className}`}
    >
      <Star className='w-4 h-4 mr-2' />
      {subscription.planName || 'Premium'}
    </div>
  );
}
