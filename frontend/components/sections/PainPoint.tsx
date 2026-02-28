'use client';

import { motion } from 'framer-motion';
import { AlertTriangle, Clock, DollarSign, Terminal } from 'lucide-react';

export default function PainPoint() {
  const problems = [
    {
      icon: AlertTriangle,
      title: 'Late Problem Detection',
      description: 'You find out about server issues only after users complain',
    },
    {
      icon: Clock,
      title: 'Time-Consuming Setup',
      description: 'Existing solutions require hours of configuration and learning',
    },
    {
      icon: DollarSign,
      title: 'Expensive Monitoring',
      description: 'Enterprise tools cost thousands per month for basic features',
    },
    {
      icon: Terminal,
      title: 'Complex Dashboards',
      description: 'Overloaded interfaces make it hard to spot critical issues',
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
          <div className='inline-block px-4 py-2 bg-red-500/10 border border-red-500/20 rounded-full mb-6'>
            <span className='text-red-400 font-semibold'>The Problem</span>
          </div>
          <h2 className='text-5xl md:text-6xl font-bold mb-6'>
            Server Monitoring
            <br />
            <span className='bg-clip-text text-transparent bg-gradient-to-r from-red-400 to-orange-400'>
              Shouldn't Be This Hard
            </span>
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
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true }}
              transition={{ delay: i * 0.1 }}
              className='group relative bg-gradient-to-br from-gray-900 to-black border border-white/10 rounded-2xl p-8 hover:border-red-500/50 transition-all duration-300'
            >
              <div className='absolute inset-0 bg-gradient-to-br from-red-600/5 to-orange-600/5 opacity-0 group-hover:opacity-100 transition-opacity rounded-2xl' />
              <div className='relative'>
                <div className='w-14 h-14 bg-red-500/10 border border-red-500/20 rounded-xl flex items-center justify-center mb-6'>
                  <problem.icon className='w-7 h-7 text-red-400' />
                </div>
                <h3 className='text-2xl font-bold mb-3'>{problem.title}</h3>
                <p className='text-gray-400 leading-relaxed'>{problem.description}</p>
              </div>
            </motion.div>
          ))}
        </div>

        {/* Solution teaser */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          className='text-center mt-20'
        >
          <div className='inline-block px-6 py-3 bg-gradient-to-r from-blue-600 to-purple-600 rounded-full'>
            <span className='font-semibold'>✨ ServerEye solves all of this</span>
          </div>
        </motion.div>
      </div>
    </section>
  );
}
