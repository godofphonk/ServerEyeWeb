'use client';

import { motion } from 'framer-motion';
import { Download, Zap, Bell, BarChart } from 'lucide-react';

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
    <section className='py-32 relative'>
      <div className='container mx-auto px-6'>
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          className='text-center mb-20'
        >
          <h2 className='text-5xl md:text-6xl font-bold mb-6'>
            Get Started In
            <span className='bg-clip-text text-transparent bg-gradient-to-r from-blue-400 to-purple-400'>
              {' '}
              60 Seconds
            </span>
          </h2>
          <p className='text-xl text-gray-400 max-w-2xl mx-auto'>
            No complex setup. No hours of configuration. Just install and go.
          </p>
        </motion.div>

        <div className='max-w-5xl mx-auto'>
          {steps.map((step, i) => (
            <motion.div
              key={i}
              initial={{ opacity: 0, x: -20 }}
              whileInView={{ opacity: 1, x: 0 }}
              viewport={{ once: true }}
              transition={{ delay: i * 0.2 }}
              className='relative mb-16 last:mb-0'
            >
              {/* Connection line */}
              {i < steps.length - 1 && (
                <div className='absolute left-7 top-20 w-0.5 h-full bg-gradient-to-b from-blue-500 to-purple-500 opacity-30' />
              )}

              <div className='flex gap-8 items-start'>
                {/* Step number & icon */}
                <div className='relative flex-shrink-0'>
                  <div className='w-14 h-14 bg-gradient-to-br from-blue-600 to-purple-600 rounded-xl flex items-center justify-center shadow-lg shadow-blue-500/50'>
                    <step.icon className='w-7 h-7 text-white' />
                  </div>
                  <div className='absolute -top-2 -right-2 w-6 h-6 bg-white text-black rounded-full flex items-center justify-center text-sm font-bold'>
                    {i + 1}
                  </div>
                </div>

                {/* Content */}
                <div className='flex-1 bg-gradient-to-br from-gray-900 to-black border border-white/10 rounded-2xl p-8'>
                  <h3 className='text-2xl font-bold mb-3'>{step.title}</h3>
                  <p className='text-gray-400 mb-4'>{step.description}</p>

                  {step.code && (
                    <div className='bg-black/50 border border-white/10 rounded-lg p-4 font-mono text-sm text-green-400'>
                      $ {step.code}
                    </div>
                  )}

                  {step.highlight && (
                    <div className='inline-block px-4 py-2 bg-blue-500/10 border border-blue-500/20 rounded-full text-blue-400 text-sm font-semibold'>
                      {step.highlight}
                    </div>
                  )}
                </div>
              </div>
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  );
}
