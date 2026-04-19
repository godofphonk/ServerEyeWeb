'use client';

import { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { CheckCircle, XCircle, AlertTriangle, Info, X } from 'lucide-react';
import { Toast as ToastType } from '@/types';

interface ToastProps {
  toast: ToastType;
  onClose: (id: string) => void;
}

const toastConfig = {
  success: {
    icon: CheckCircle,
    bgColor: 'bg-green-500/10',
    borderColor: 'border-green-500/50',
    iconColor: 'text-green-400',
    progressColor: 'bg-gradient-to-r from-green-500 to-emerald-500',
    shadow: 'shadow-lg shadow-green-500/20',
  },
  error: {
    icon: XCircle,
    bgColor: 'bg-red-500/10',
    borderColor: 'border-red-500/50',
    iconColor: 'text-red-400',
    progressColor: 'bg-gradient-to-r from-red-500 to-rose-500',
    shadow: 'shadow-lg shadow-red-500/20',
  },
  warning: {
    icon: AlertTriangle,
    bgColor: 'bg-yellow-500/10',
    borderColor: 'border-yellow-500/50',
    iconColor: 'text-yellow-400',
    progressColor: 'bg-gradient-to-r from-yellow-500 to-orange-500',
    shadow: 'shadow-lg shadow-yellow-500/20',
  },
  info: {
    icon: Info,
    bgColor: 'bg-blue-500/10',
    borderColor: 'border-blue-500/50',
    iconColor: 'text-blue-400',
    progressColor: 'bg-gradient-to-r from-blue-500 to-cyan-500',
    shadow: 'shadow-lg shadow-blue-500/20',
  },
};

export function Toast({ toast, onClose }: ToastProps) {
  const [progress, setProgress] = useState(100);
  const config = toastConfig[toast.type];
  const Icon = config.icon;
  const duration = toast.duration || 5000;

  useEffect(() => {
    if (duration <= 0) return;

    const interval = setInterval(() => {
      setProgress(prev => {
        const newProgress = prev - 100 / (duration / 100);
        return newProgress <= 0 ? 0 : newProgress;
      });
    }, 100);

    return () => clearInterval(interval);
  }, [duration]);

  return (
    <motion.div
      initial={{ x: 400, opacity: 0, scale: 0.9 }}
      animate={{ x: 0, opacity: 1, scale: 1 }}
      exit={{ x: 400, opacity: 0, scale: 0.9 }}
      transition={{ type: 'spring', stiffness: 300, damping: 30 }}
      whileHover={{ scale: 1.02 }}
      className={`
        w-[400px] rounded-xl border backdrop-blur-xl overflow-hidden relative
        ${config.bgColor} ${config.borderColor} ${config.shadow}
      `}
    >
      <motion.div
        className='absolute inset-0 bg-gradient-to-br from-white/5 to-transparent'
        animate={{ opacity: [0, 1, 0] }}
        transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
      />
      <div className='p-4 relative z-10'>
        <div className='flex items-start gap-3'>
          <motion.div
            className={`flex-shrink-0 ${config.iconColor}`}
            animate={{ rotate: [0, 360] }}
            transition={{ duration: 20, repeat: Infinity, ease: 'linear' }}
          >
            <Icon className='w-6 h-6' />
          </motion.div>

          <div className='flex-1 min-w-0'>
            <h3 className='text-sm font-semibold text-white'>{toast.title}</h3>
            {toast.message && <p className='text-sm text-gray-300 mt-1'>{toast.message}</p>}
          </div>

          <motion.button
            whileHover={{ scale: 1.1, rotate: 90 }}
            whileTap={{ scale: 0.9 }}
            onClick={() => onClose(toast.id)}
            className='flex-shrink-0 text-gray-400 hover:text-white transition-colors'
          >
            <X className='w-5 h-5' />
          </motion.button>
        </div>
      </div>

      {duration > 0 && (
        <div className='h-1 bg-gray-800 relative z-10'>
          <motion.div
            className={`h-full ${config.progressColor}`}
            initial={{ width: '100%' }}
            animate={{ width: `${progress}%` }}
            transition={{ duration: 0.1, ease: 'linear' }}
          />
        </div>
      )}
    </motion.div>
  );
}
