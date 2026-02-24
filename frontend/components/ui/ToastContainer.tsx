'use client';

import { AnimatePresence } from 'framer-motion';
import { useToastContext } from '@/context/ToastContext';
import { Toast } from './Toast';

export function ToastContainer() {
  const { toasts, removeToast } = useToastContext();

  return (
    <div className="fixed top-4 right-4 z-[9999] flex flex-col gap-3 pointer-events-none">
      <AnimatePresence>
        {toasts.map((toast) => (
          <div key={toast.id} className="pointer-events-auto">
            <Toast toast={toast} onClose={removeToast} />
          </div>
        ))}
      </AnimatePresence>
    </div>
  );
}
