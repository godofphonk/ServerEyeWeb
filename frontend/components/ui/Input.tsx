'use client';

import { forwardRef } from 'react';
import { motion, HTMLMotionProps } from 'framer-motion';
import { cn } from '@/lib/utils';

interface InputProps extends Omit<HTMLMotionProps<'input'>, 'whileFocus'> {
  label?: string;
  error?: string;
  helperText?: string;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, helperText, className, ...props }, ref) => {
    return (
      <div className='w-full'>
        {label && (
          <label className='block text-sm font-medium mb-2'>
            {label}
            {props.required && <span className='text-red-400 ml-1'>*</span>}
          </label>
        )}
        <div className='relative'>
          <motion.input
            ref={ref}
            whileFocus={{ scale: 1.01 }}
            transition={{ duration: 0.2 }}
            className={cn(
              'w-full px-4 py-3 bg-white/5 backdrop-blur-sm border border-white/10 rounded-xl',
              'text-white placeholder-gray-400',
              'focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent focus:bg-white/10',
              'transition-all duration-200',
              'disabled:opacity-50 disabled:cursor-not-allowed',
              error && 'border-red-500 focus:ring-red-500',
              className,
            )}
            {...props}
          />
          <motion.div
            className='absolute inset-0 bg-gradient-to-r from-purple-500/10 to-blue-500/10 rounded-xl pointer-events-none opacity-0'
            animate={{ opacity: error ? 1 : 0 }}
            transition={{ duration: 0.2 }}
          />
        </div>
        {error && <p className='mt-2 text-sm text-red-400'>{error}</p>}
        {helperText && !error && <p className='mt-2 text-sm text-gray-400'>{helperText}</p>}
      </div>
    );
  },
);

Input.displayName = 'Input';
