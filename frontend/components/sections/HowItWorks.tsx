'use client';

import { motion } from 'framer-motion';
import { Download, Zap, Bell, BarChart, Terminal, Sparkles } from 'lucide-react';

export default function HowItWorks() {
  const steps = [
    {
      icon: Download,
      title: 'Install Agent',
      description: 'One command to install our lightweight agent on your servers',
      code: 'curl -sSL https://servereye.dev/install | bash',
    },
    {
      icon: Zap,
      title: 'Auto-Connect',
      description: 'Agent automatically connects and starts sending metrics',
      highlight: 'No configuration needed',
    },
    {
      icon: BarChart,
      title: 'Monitor Everything',
      description: 'View real-time metrics, logs, and performance data',
      highlight: 'Live dashboards',
    },
    {
      icon: Bell,
      title: 'Get Alerts',
      description: 'Receive instant notifications via Telegram, Email, or Webhooks',
      highlight: 'Never miss an issue',
    },
  ];

  return (
    <section className='py-32 relative overflow-hidden'>
      <div className='absolute inset-0 bg-gradient-to-b from-transparent via-blue-900/5 to-transparent' />

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
            <Sparkles className='w-4 h-4 text-blue-400' />
            <span className='text-sm text-blue-300 font-medium'>Quick Setup</span>
          </motion.div>
          <h2 className='text-5xl md:text-6xl lg:text-7xl font-bold mb-6'>
            Get Started In
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
              60 Seconds
            </motion.span>
          </h2>
          <p className='text-xl text-gray-400 max-w-2xl mx-auto'>
            No complex setup. No hours of configuration. Just install and go.
          </p>
        </motion.div>

        <div className='max-w-5xl mx-auto'>
          {steps.map((step, i) => (
            <motion.div
              key={i}
              initial={{ opacity: 0, x: -30 }}
              whileInView={{ opacity: 1, x: 0 }}
              viewport={{ once: true }}
              transition={{ delay: i * 0.15, duration: 0.6 }}
              className='relative mb-16 last:mb-0'
            >
              {i < steps.length - 1 && (
                <motion.div
                  initial={{ height: 0 }}
                  whileInView={{ height: '100%' }}
                  viewport={{ once: true }}
                  transition={{ delay: i * 0.15 + 0.3, duration: 0.8 }}
                  className='absolute left-7 top-20 w-0.5 bg-gradient-to-b from-blue-500 to-purple-500 opacity-30'
                />
              )}

              <div className='flex gap-8 items-start'>
                <motion.div
                  whileHover={{ scale: 1.1, rotate: 5 }}
                  transition={{ duration: 0.3 }}
                  className='relative flex-shrink-0'
                >
                  <motion.div
                    animate={{
                      boxShadow: [
                        '0 0 20px rgba(59, 130, 246, 0.3)',
                        '0 0 40px rgba(59, 130, 246, 0.5)',
                        '0 0 20px rgba(59, 130, 246, 0.3)',
                      ],
                    }}
                    transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
                    className='w-14 h-14 bg-gradient-to-br from-blue-600 to-purple-600 rounded-xl flex items-center justify-center'
                  >
                    <step.icon className='w-7 h-7 text-white' />
                  </motion.div>
                  <motion.div
                    animate={{ scale: [1, 1.1, 1] }}
                    transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
                    className='absolute -top-2 -right-2 w-6 h-6 bg-white text-black rounded-full flex items-center justify-center text-sm font-bold shadow-lg'
                  >
                    {i + 1}
                  </motion.div>
                </motion.div>

                <motion.div
                  whileHover={{ scale: 1.02, y: -5 }}
                  transition={{ duration: 0.3 }}
                  className='flex-1 bg-gray-900/50 border border-white/10 rounded-2xl p-8 hover:border-blue-500/50 transition-all duration-300'
                >
                  <h3 className='text-2xl font-bold mb-3'>{step.title}</h3>
                  <p className='text-gray-400 mb-4'>{step.description}</p>

                  {step.code && (
                    <motion.div
                      initial={{ opacity: 0, y: 10 }}
                      whileInView={{ opacity: 1, y: 0 }}
                      viewport={{ once: true }}
                      transition={{ delay: i * 0.15 + 0.4 }}
                      className='group relative bg-black/50 border border-white/10 rounded-lg p-4 font-mono text-sm text-green-400 hover:border-green-500/50 transition-all'
                    >
                      <div className='absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity'>
                        <Terminal className='w-4 h-4 text-green-400 cursor-pointer hover:scale-110 transition-transform' />
                      </div>
                      <span className='text-gray-500'>$ </span>
                      {step.code}
                    </motion.div>
                  )}

                  {step.highlight && (
                    <motion.div
                      initial={{ opacity: 0, scale: 0.9 }}
                      whileInView={{ opacity: 1, scale: 1 }}
                      viewport={{ once: true }}
                      transition={{ delay: i * 0.15 + 0.4 }}
                      whileHover={{ scale: 1.05 }}
                      className='inline-block px-4 py-2 bg-blue-500/10 border border-blue-500/20 rounded-full text-blue-400 text-sm font-semibold hover:bg-blue-500/20 transition-all'
                    >
                      {step.highlight}
                    </motion.div>
                  )}
                </motion.div>
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  );
}
