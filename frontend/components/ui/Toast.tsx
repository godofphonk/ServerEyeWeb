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
    borderColor: 'border-green-500',
    iconColor: 'text-green-400',
    progressColor: 'bg-green-500'
  },
  error: {
    icon: XCircle,
    bgColor: 'bg-red-500/10',
    borderColor: 'border-red-500',
    iconColor: 'text-red-400',
    progressColor: 'bg-red-500'
  },
  warning: {
    icon: AlertTriangle,
    bgColor: 'bg-yellow-500/10',
    borderColor: 'border-yellow-500',
    iconColor: 'text-yellow-400',
    progressColor: 'bg-yellow-500'
  },
  info: {
    icon: Info,
    bgColor: 'bg-blue-500/10',
    borderColor: 'border-blue-500',
    iconColor: 'text-blue-400',
    progressColor: 'bg-blue-500'
  }
};

export function Toast({ toast, onClose }: ToastProps) {
  const [progress, setProgress] = useState(100);
  const config = toastConfig[toast.type];
  const Icon = config.icon;
  const duration = toast.duration || 5000;

  useEffect(() => {
    if (duration <= 0) return;

    const interval = setInterval(() => {
      setProgress((prev) => {
        const newProgress = prev - (100 / (duration / 100));
        return newProgress <= 0 ? 0 : newProgress;
      });
    }, 100);

    return () => clearInterval(interval);
  }, [duration]);

  return (
    <motion.div
      initial={{ x: 400, opacity: 0 }}
      animate={{ x: 0, opacity: 1 }}
      exit={{ x: 400, opacity: 0 }}
      transition={{ type: 'spring', stiffness: 300, damping: 30 }}
      className={`
        w-[400px] rounded-lg border backdrop-blur-xl shadow-lg overflow-hidden
        ${config.bgColor} ${config.borderColor}
      `}
    >
      <div className="p-4">
        <div className="flex items-start gap-3">
          <div className={`flex-shrink-0 ${config.iconColor}`}>
            <Icon className="w-6 h-6" />
          </div>

          <div className="flex-1 min-w-0">
            <h3 className="text-sm font-semibold text-white">
              {toast.title}
            </h3>
            {toast.message && (
              <p className="text-sm text-gray-300 mt-1">
                {toast.message}
              </p>
            )}
          </div>

          <button
            onClick={() => onClose(toast.id)}
            className="flex-shrink-0 text-gray-400 hover:text-white transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>
      </div>

      {duration > 0 && (
        <div className="h-1 bg-gray-800">
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
