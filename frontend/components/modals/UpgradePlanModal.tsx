'use client';

import { useRouter } from 'next/navigation';
import { motion, AnimatePresence } from 'framer-motion';
import { X, ArrowUpRight } from 'lucide-react';
import { Button } from '@/components/ui/Button';

interface UpgradePlanModalProps {
  isOpen: boolean;
  onClose: () => void;
  limitType?: string;
  currentCount?: number;
  maxAllowed?: number;
  planName?: string;
}

export default function UpgradePlanModal({
  isOpen,
  onClose,
  limitType = 'servers',
  currentCount = 0,
  maxAllowed = 0,
  planName = 'Free',
}: UpgradePlanModalProps) {
  const router = useRouter();

  const handleUpgrade = () => {
    router.push('/pricing');
    onClose();
  };

  return (
    <AnimatePresence>
      {isOpen && (
        <div className='fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm'>
          <motion.div
            initial={{ opacity: 0, scale: 0.95, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.95, y: 20 }}
            transition={{ type: 'spring', stiffness: 300, damping: 30 }}
            className='relative w-full max-w-md rounded-2xl bg-gray-900 border border-yellow-500/20 p-6 shadow-2xl shadow-yellow-500/20 overflow-hidden'
          >
            <motion.div
              className='absolute inset-0 bg-gradient-to-br from-yellow-500/5 to-orange-500/5'
              animate={{ opacity: [0, 1, 0] }}
              transition={{ duration: 3, repeat: Infinity, ease: 'easeInOut' }}
            />
            <motion.button
              whileHover={{ scale: 1.1, rotate: 90 }}
              whileTap={{ scale: 0.9 }}
              onClick={onClose}
              className='absolute right-4 top-4 text-gray-400 hover:text-white transition-colors z-10'
            >
              <X className='h-5 w-5' />
            </motion.button>

            <div className='mb-6 relative z-10'>
              <motion.div
                whileHover={{ scale: 1.1 }}
                className='mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-yellow-500/20 border border-yellow-500/30'
              >
                <motion.div
                  animate={{ rotate: [0, 10, -10, 0] }}
                  transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
                >
                  <ArrowUpRight className='h-6 w-6 text-yellow-500' />
                </motion.div>
              </motion.div>
              <h2 className='text-xl font-semibold text-white mb-2'>Upgrade Your Plan</h2>
              <p className='text-gray-400'>
                Your <span className='text-white font-medium'>{planName}</span> plan allows up to{' '}
                <span className='text-white font-medium'>{maxAllowed}</span> {limitType}. You
                currently have <span className='text-white font-medium'>{currentCount}</span>{' '}
                {limitType}.
              </p>
            </div>

            <div className='flex gap-3 relative z-10'>
              <Button variant='secondary' onClick={onClose} className='flex-1'>
                Cancel
              </Button>
              <Button
                onClick={handleUpgrade}
                className='flex-1 bg-gradient-to-r from-yellow-500 to-orange-500 hover:from-yellow-600 hover:to-orange-600 shadow-lg shadow-yellow-500/30'
              >
                View Plans
              </Button>
            </div>
          </motion.div>
        </div>
      )}
    </AnimatePresence>
  );
}
