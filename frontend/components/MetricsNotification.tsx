import { useEffect, useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Info, X } from 'lucide-react';

interface MetricsNotificationProps {
  message: string | null;
  onClose?: () => void;
}

export function MetricsNotification({ message, onClose }: MetricsNotificationProps) {
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    if (message) {
      setIsVisible(true);

      // Auto-hide after 5 seconds
      const timer = setTimeout(() => {
        setIsVisible(false);
        onClose?.();
      }, 5000);

      return () => clearTimeout(timer);
    }
  }, [message, onClose]);

  const handleClose = () => {
    setIsVisible(false);
    onClose?.();
  };

  if (!message) return null;

  return (
    <AnimatePresence>
      {isVisible && (
        <motion.div
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -20 }}
          className='fixed top-4 right-4 z-50 max-w-md'
        >
          <div className='bg-blue-500/10 border border-blue-500/30 rounded-lg p-4 backdrop-blur-sm'>
            <div className='flex items-start gap-3'>
              <Info className='w-5 h-5 text-blue-400 flex-shrink-0 mt-0.5' />
              <div className='flex-1'>
                <p className='text-sm text-blue-100'>{message}</p>
              </div>
              <button
                onClick={handleClose}
                className='text-blue-400 hover:text-blue-300 transition-colors'
              >
                <X className='w-4 h-4' />
              </button>
            </div>
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
