'use client';

import { useRouter } from 'next/navigation';
import { X, ArrowUpRight } from 'lucide-react';

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

  if (!isOpen) return null;

  const handleUpgrade = () => {
    router.push('/pricing');
    onClose();
  };

  return (
    <div className='fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm'>
      <div className='relative w-full max-w-md rounded-lg bg-gray-900 border border-gray-800 p-6 shadow-xl'>
        <button
          onClick={onClose}
          className='absolute right-4 top-4 text-gray-400 hover:text-white transition-colors'
        >
          <X className='h-5 w-5' />
        </button>

        <div className='mb-6'>
          <div className='mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-yellow-500/20'>
            <ArrowUpRight className='h-6 w-6 text-yellow-500' />
          </div>
          <h2 className='text-xl font-semibold text-white mb-2'>Upgrade Your Plan</h2>
          <p className='text-gray-400'>
            Your <span className='text-white font-medium'>{planName}</span> plan allows up to{' '}
            <span className='text-white font-medium'>{maxAllowed}</span> {limitType}. You currently
            have <span className='text-white font-medium'>{currentCount}</span> {limitType}.
          </p>
        </div>

        <div className='flex gap-3'>
          <button
            onClick={onClose}
            className='flex-1 rounded-lg px-4 py-2.5 text-sm font-medium text-white bg-gray-800 hover:bg-gray-700 transition-colors'
          >
            Cancel
          </button>
          <button
            onClick={handleUpgrade}
            className='flex-1 rounded-lg px-4 py-2.5 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 transition-colors'
          >
            View Plans
          </button>
        </div>
      </div>
    </div>
  );
}
