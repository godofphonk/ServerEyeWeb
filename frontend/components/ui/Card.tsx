'use client';

import { ReactNode } from 'react';
import { motion } from 'framer-motion';
import { cn } from '@/lib/utils';

interface CardProps {
  children: ReactNode;
  className?: string;
  hover?: boolean;
}

export function Card({ children, className, hover = false }: CardProps) {
  return (
    <motion.div
      whileHover={hover ? { scale: 1.02, y: -5 } : undefined}
      transition={{ duration: 0.3 }}
      className={cn(
        'bg-gray-900/50 backdrop-blur-xl border border-white/10 rounded-2xl p-6 relative overflow-hidden',
        hover && 'hover:border-white/20 transition-all duration-300',
        className,
      )}
    >
      <motion.div className='absolute inset-0 bg-gradient-to-br from-purple-600/5 to-blue-600/5 opacity-0 hover:opacity-100 transition-opacity' />
      <div className='relative z-10'>{children}</div>
    </motion.div>
  );
}

export function CardHeader({ children, className }: { children: ReactNode; className?: string }) {
  return <div className={cn('mb-4', className)}>{children}</div>;
}

export function CardTitle({ children, className }: { children: ReactNode; className?: string }) {
  return <h3 className={cn('text-2xl font-bold', className)}>{children}</h3>;
}

export function CardDescription({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return <p className={cn('text-gray-400', className)}>{children}</p>;
}

export function CardContent({ children, className }: { children: ReactNode; className?: string }) {
  return <div className={cn(className)}>{children}</div>;
}

export function CardFooter({ children, className }: { children: ReactNode; className?: string }) {
  return <div className={cn('mt-6 pt-4 border-t border-white/10', className)}>{children}</div>;
}
