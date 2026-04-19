'use client';

import { motion } from 'framer-motion';
import { AlertTriangle, Clock, DollarSign, Terminal, X } from 'lucide-react';

export default function PainPoint() {
  const problems = [
    {
      icon: AlertTriangle,
      title: 'Late Problem Detection',
      description: 'You find out about server issues only after users complain',
      color: 'red',
    },
    {
      icon: Clock,
      title: 'Time-Consuming Setup',
      description: 'Existing solutions require hours of configuration and learning',
      color: 'orange',
    },
    {
      icon: DollarSign,
      title: 'Expensive Monitoring',
      description: 'Enterprise tools cost thousands per month for basic features',
      color: 'yellow',
    },
    {
      icon: Terminal,
      title: 'Complex Dashboards',
      description: 'Overloaded interfaces make it hard to spot critical issues',
      color: 'purple',
    },
  ];

  return (
    <section className='py-32 relative overflow-hidden'>
      <div className='absolute inset-0 bg-gradient-to-b from-transparent via-red-900/5 to-transparent' />

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
            className='inline-flex items-center gap-2 px-4 py-2 rounded-full bg-red-500/10 border border-red-500/20 mb-6'
          >
            <X className='w-4 h-4 text-red-400' />
            <span className='text-sm text-red-300 font-medium'>The Problem</span>
          </motion.div>
          <h2 className='text-5xl md:text-6xl lg:text-7xl font-bold mb-6'>
            Server Monitoring
            <br />
            <motion.span
              className='bg-clip-text text-transparent bg-gradient-to-r from-red-400 via-orange-400 to-yellow-400'
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
              Shouldn't Be This Hard
            </motion.span>
          </h2>
          <p className='text-xl text-gray-400 max-w-2xl mx-auto'>
            Traditional monitoring tools are complex, expensive, and slow.
            <br />
            Your business deserves better.
          </p>
        </motion.div>

        <div className='grid grid-cols-1 md:grid-cols-2 gap-8 max-w-5xl mx-auto'>
          {problems.map((problem, i) => (
            <motion.div
              key={i}
              initial={{ opacity: 0, y: 30, rotateX: -10 }}
              whileInView={{ opacity: 1, y: 0, rotateX: 0 }}
              viewport={{ once: true }}
              transition={{ delay: i * 0.15, duration: 0.6 }}
              whileHover={{
                scale: 1.05,
                y: -10,
                rotateX: 5,
                boxShadow: '0 20px 40px rgba(239, 68, 68, 0.2)',
              }}
              className='group relative bg-gray-900/50 border border-white/10 rounded-2xl p-8 hover:border-red-500/50 transition-all duration-300'
              style={{ transformStyle: 'preserve-3d' }}
            >
              <motion.div className='absolute inset-0 bg-gradient-to-br from-red-600/10 to-orange-600/10 opacity-0 group-hover:opacity-100 transition-opacity rounded-2xl' />
              <div className='relative' style={{ transform: 'translateZ(20px)' }}>
                <motion.div
                  animate={{ rotate: [0, 5, -5, 0] }}
                  transition={{ duration: 4, repeat: Infinity, ease: 'easeInOut' }}
                  className='w-16 h-16 bg-red-500/10 border border-red-500/20 rounded-2xl flex items-center justify-center mb-6 group-hover:scale-110 transition-transform'
                >
                  <problem.icon className={`w-8 h-8 text-${problem.color}-400`} />
                </motion.div>
                <h3 className='text-2xl font-bold mb-3 group-hover:text-red-300 transition-colors'>
                  {problem.title}
                </h3>
                <p className='text-gray-400 leading-relaxed group-hover:text-gray-300 transition-colors'>
                  {problem.description}
                </p>
              </div>
            </motion.div>
          ))}
        </div>

        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ delay: 0.8 }}
          className='text-center mt-20'
        >
          <motion.button
            whileHover={{ scale: 1.05 }}
            whileTap={{ scale: 0.95 }}
            className='px-8 py-4 bg-gradient-to-r from-blue-600 to-purple-600 rounded-full font-semibold text-lg shadow-lg shadow-blue-500/50 hover:shadow-xl hover:shadow-blue-500/60 transition-all'
          >
            ✨ ServerEye solves all of this
          </motion.button>
        </motion.div>
      </div>
    </section>
  );
}
