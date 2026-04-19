'use client';

import React from 'react';
import { motion } from 'framer-motion';
import { Crown, Star, Lock } from 'lucide-react';
import { useSubscription } from '@/hooks/useSubscription';

interface SubscriptionBadgeProps {
  className?: string;
}

export function SubscriptionBadge({ className = '' }: SubscriptionBadgeProps) {
  const { subscription, loading, isPro } = useSubscription();

  if (loading) {
    return (
      <motion.div
        whileHover={{ scale: 1.05 }}
        className={`inline-flex items-center px-3 py-1 rounded-full text-sm bg-gray-100 text-gray-600 ${className}`}
      >
        <motion.div
          animate={{ rotate: 360 }}
          transition={{ duration: 1, repeat: Infinity, ease: 'linear' }}
          className='w-4 h-4 mr-2'
        >
          <div className='animate-spin rounded-full border-2 border-gray-300 border-t-gray-600'></div>
        </motion.div>
        Loading...
      </motion.div>
    );
  }

  if (!subscription || subscription.status !== 1) {
    return (
      <motion.div
        whileHover={{ scale: 1.05 }}
        className={`inline-flex items-center px-3 py-1 rounded-full text-sm bg-gray-100 text-gray-600 ${className}`}
      >
        <Lock className='w-4 h-4 mr-2' />
        Free Plan
      </motion.div>
    );
  }

  if (isPro) {
    return (
      <motion.div
        whileHover={{ scale: 1.05 }}
        whileTap={{ scale: 0.95 }}
        className={`inline-flex items-center px-3 py-1 rounded-full text-sm bg-gradient-to-r from-blue-500 to-purple-600 text-white font-medium shadow-lg shadow-purple-500/30 ${className}`}
      >
        <motion.div
          animate={{ rotate: [0, 10, -10, 0] }}
          transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
        >
          <Crown className='w-4 h-4 mr-2' />
        </motion.div>
        Pro Plan
      </motion.div>
    );
  }

  return (
    <motion.div
      whileHover={{ scale: 1.05 }}
      whileTap={{ scale: 0.95 }}
      className={`inline-flex items-center px-3 py-1 rounded-full text-sm bg-gradient-to-r from-yellow-400 to-orange-500 text-white font-medium shadow-lg shadow-orange-500/30 ${className}`}
    >
      <motion.div
        animate={{ rotate: [0, 10, -10, 0] }}
        transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
      >
        <Star className='w-4 h-4 mr-2' />
      </motion.div>
      {subscription.planName || 'Premium'}
    </motion.div>
  );
}
