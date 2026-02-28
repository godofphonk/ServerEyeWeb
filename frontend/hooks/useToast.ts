'use client';

import { useToastContext } from '@/context/ToastContext';
import { ToastType } from '@/types';

export function useToast() {
  const { addToast } = useToastContext();

  return {
    success: (title: string, message?: string, duration?: number) => {
      addToast('success', title, message, duration);
    },
    error: (title: string, message?: string, duration?: number) => {
      addToast('error', title, message, duration);
    },
    warning: (title: string, message?: string, duration?: number) => {
      addToast('warning', title, message, duration);
    },
    info: (title: string, message?: string, duration?: number) => {
      addToast('info', title, message, duration);
    },
    toast: (type: ToastType, title: string, message?: string, duration?: number) => {
      addToast(type, title, message, duration);
    },
  };
}
