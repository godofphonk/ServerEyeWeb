'use client';

import { motion, useScroll, useTransform } from 'framer-motion';
import {
  Server,
  Cpu,
  HardDrive,
  Activity,
  Zap,
  TrendingUp,
  Clock,
  AlertCircle,
} from 'lucide-react';
import { useRef } from 'react';

function AnimatedProgressBar({ value, color }: { value: number; color: string }) {
  return (
    <div className='w-full h-2 bg-white/10 rounded-full overflow-hidden'>
      <motion.div
        initial={{ width: 0 }}
        whileInView={{ width: `${value}%` }}
        viewport={{ once: true }}
        transition={{ duration: 1.5, ease: 'easeOut' }}
        className={`h-full bg-gradient-to-r ${color} rounded-full`}
      />
    </div>
  );
}

function MetricCard({
  icon: Icon,
  label,
  value,
  color,
  delay,
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  value: string;
  color: string;
  delay: number;
}) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 30, rotateX: -10 }}
      whileInView={{ opacity: 1, y: 0, rotateX: 0 }}
      viewport={{ once: true }}
      transition={{ delay, duration: 0.6 }}
      whileHover={{ scale: 1.05, y: -5, rotateX: 5 }}
      className='group relative bg-white/5 border border-white/10 rounded-2xl p-6 hover:border-white/20 transition-all duration-300'
    >
      <motion.div className='absolute inset-0 bg-blue-500/5 opacity-0 group-hover:opacity-100 transition-opacity rounded-2xl' />
      <div className='relative'>
        <motion.div
          animate={{ rotate: [0, 360] }}
          transition={{ duration: 20, repeat: Infinity, ease: 'linear' }}
          className='w-12 h-12 bg-white/10 rounded-xl flex items-center justify-center mb-4'
        >
          <Icon className={`w-6 h-6 text-${color}-400`} />
        </motion.div>
        <motion.div
          initial={{ opacity: 0, scale: 0.5 }}
          whileInView={{ opacity: 1, scale: 1 }}
          viewport={{ once: true }}
          transition={{ delay: delay + 0.3, duration: 0.5 }}
          className='text-4xl font-bold mb-2 bg-clip-text text-transparent bg-gradient-to-r from-white to-gray-300'
        >
          {value}
        </motion.div>
        <div className='text-sm text-gray-400 group-hover:text-gray-300 transition-colors'>
          {label}
        </div>
      </div>
    </motion.div>
  );
}

export default function Visualization() {
  const containerRef = useRef<HTMLDivElement>(null);
  const { scrollYProgress } = useScroll({
    target: containerRef,
    offset: ['start end', 'end start'],
  });

  const y = useTransform(scrollYProgress, [0, 1], [50, -50]);
  const scale = useTransform(scrollYProgress, [0, 0.5, 1], [0.8, 1, 0.8]);
  const opacity = useTransform(scrollYProgress, [0, 0.2, 0.8, 1], [0, 1, 1, 0]);

  const metrics = [
    { icon: Cpu, label: 'CPU Usage', value: '45%', color: 'blue-400' },
    { icon: HardDrive, label: 'Memory', value: '8.2 GB', color: 'purple-400' },
    { icon: Server, label: 'Disk Usage', value: '67%', color: 'pink-400' },
    { icon: Activity, label: 'Network', value: '1.2 GB/s', color: 'green-400' },
  ];

  return (
    <section ref={containerRef} className='py-32 relative overflow-hidden'>
      <motion.div
        style={{ y }}
        className='absolute inset-0 bg-gradient-to-b from-transparent via-blue-900/5 to-transparent'
      />
      <div className='absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-[800px] h-[800px] bg-purple-500/10 rounded-full blur-3xl' />

      <div className='container mx-auto px-6 relative z-10'>
        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.8 }}
          className='text-center mb-20'
        >
          <motion.div
            initial={{ opacity: 0, scale: 0.9 }}
            whileInView={{ opacity: 1, scale: 1 }}
            viewport={{ once: true }}
            transition={{ delay: 0.2 }}
            className='inline-flex items-center gap-2 px-4 py-2 rounded-full bg-blue-500/10 border border-blue-500/20 mb-6'
          >
            <TrendingUp className='w-4 h-4 text-blue-400' />
            <span className='text-sm text-blue-300 font-medium'>Live Dashboard</span>
          </motion.div>
          <h2 className='text-5xl md:text-6xl lg:text-7xl font-bold mb-6'>
            See Everything
            <motion.span
              className='bg-clip-text text-transparent bg-gradient-to-r from-blue-400 via-purple-400 to-pink-400'
              animate={{
                backgroundPosition: ['0% 50%', '100% 50%', '0% 50%'],
              }}
              transition={{
                duration: 4,
                repeat: Infinity,
                ease: 'easeInOut',
              }}
              style={{ backgroundSize: '200% auto' }}
            >
              {' '}
              In Real-Time
            </motion.span>
          </h2>
          <p className='text-xl text-gray-400 max-w-2xl mx-auto'>
            Beautiful dashboards with live metrics, charts, and alerts (coming soon)
          </p>
        </motion.div>

        <motion.div style={{ scale, opacity }} className='relative max-w-6xl mx-auto'>
          <motion.div
            animate={{
              scale: [1, 1.2, 1],
              opacity: [0.5, 0.8, 0.5],
            }}
            transition={{
              duration: 4,
              repeat: Infinity,
              ease: 'easeInOut',
            }}
            className='absolute inset-0 bg-gradient-to-r from-blue-600/30 to-purple-600/30 blur-3xl'
          />

          <motion.div
            whileHover={{ scale: 1.02 }}
            transition={{ duration: 0.3 }}
            className='relative bg-gray-900/90 border border-white/10 rounded-3xl p-8 backdrop-blur-xl shadow-2xl'
          >
            <div className='flex items-center justify-between mb-8'>
              <div>
                <motion.h3
                  initial={{ opacity: 0, x: -20 }}
                  whileInView={{ opacity: 1, x: 0 }}
                  viewport={{ once: true }}
                  transition={{ delay: 0.3 }}
                  className='text-3xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-white to-gray-300'
                >
                  Server Dashboard
                </motion.h3>
                <motion.p
                  initial={{ opacity: 0, x: -20 }}
                  whileInView={{ opacity: 1, x: 0 }}
                  viewport={{ once: true }}
                  transition={{ delay: 0.4 }}
                  className='text-gray-500'
                >
                  Real-time monitoring
                </motion.p>
              </div>
              <motion.div
                initial={{ opacity: 0, x: 20 }}
                whileInView={{ opacity: 1, x: 0 }}
                viewport={{ once: true }}
                transition={{ delay: 0.3 }}
                className='flex items-center gap-3 px-4 py-2 bg-green-500/10 border border-green-500/20 rounded-full'
              >
                <motion.div
                  animate={{ scale: [1, 1.2, 1] }}
                  transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
                  className='w-3 h-3 bg-green-500 rounded-full'
                />
                <span className='text-sm text-green-400 font-medium'>Live</span>
              </motion.div>
            </div>

            <div className='grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8'>
              {metrics.map((metric, i) => (
                <MetricCard key={i} {...metric} delay={i * 0.1} />
              ))}
            </div>

            <motion.div
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ delay: 0.5 }}
              className='bg-white/5 border border-white/10 rounded-2xl p-6'
            >
              <div className='flex items-center justify-between mb-6'>
                <div className='flex items-center gap-3'>
                  <Zap className='w-6 h-6 text-blue-400' />
                  <h4 className='text-xl font-bold'>Performance Metrics</h4>
                </div>
                <div className='flex items-center gap-2 text-sm text-gray-400'>
                  <Clock className='w-4 h-4' />
                  <span>Last 24h</span>
                </div>
              </div>

              <div className='space-y-4'>
                {[
                  { label: 'CPU Load', value: 45, color: 'from-blue-500 to-blue-400' },
                  { label: 'Memory Usage', value: 67, color: 'from-purple-500 to-purple-400' },
                  { label: 'Disk I/O', value: 32, color: 'from-pink-500 to-pink-400' },
                  { label: 'Network Traffic', value: 78, color: 'from-green-500 to-green-400' },
                ].map((item, i) => (
                  <motion.div
                    key={i}
                    initial={{ opacity: 0, x: -20 }}
                    whileInView={{ opacity: 1, x: 0 }}
                    viewport={{ once: true }}
                    transition={{ delay: 0.6 + i * 0.1 }}
                  >
                    <div className='flex justify-between mb-2'>
                      <span className='text-sm text-gray-300'>{item.label}</span>
                      <span className='text-sm font-medium text-white'>{item.value}%</span>
                    </div>
                    <AnimatedProgressBar value={item.value} color={item.color} />
                  </motion.div>
                ))}
              </div>
            </motion.div>

            <motion.div
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ delay: 0.7 }}
              className='mt-6 flex items-center gap-4 p-4 bg-yellow-500/10 border border-yellow-500/20 rounded-xl'
            >
              <motion.div
                animate={{ rotate: [0, 10, -10, 0] }}
                transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
              >
                <AlertCircle className='w-6 h-6 text-yellow-400' />
              </motion.div>
              <div>
                <div className='font-medium text-yellow-300'>System Alert (Coming Soon)</div>
                <div className='text-sm text-yellow-400/70'>
                  High CPU usage detected on server-01
                </div>
              </div>
            </motion.div>
          </motion.div>
        </motion.div>
      </div>
    </section>
  );
}
