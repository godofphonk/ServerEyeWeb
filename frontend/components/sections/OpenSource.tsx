'use client';

import { motion } from 'framer-motion';
import { GitBranch, Star, GitFork, Users, Heart, BookOpen } from 'lucide-react';

export default function OpenSource() {
  return (
    <section className='py-32 relative overflow-hidden'>
      <div className='absolute inset-0 bg-gradient-to-br from-purple-600/10 via-transparent to-blue-600/10' />
      <div className='absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-[600px] h-[600px] bg-purple-500/10 rounded-full blur-3xl' />

      <div className='container mx-auto px-6 relative z-10'>
        <motion.div
          initial={{ opacity: 0, y: 30 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.8 }}
          className='max-w-4xl mx-auto text-center'
        >
          <motion.div
            animate={{ rotate: [0, 10, -10, 0] }}
            transition={{ duration: 4, repeat: Infinity, ease: 'easeInOut' }}
            className='w-24 h-24 mx-auto mb-8 bg-gradient-to-br from-purple-500 to-blue-500 rounded-2xl flex items-center justify-center shadow-2xl shadow-purple-500/50'
          >
            <GitBranch className='w-12 h-12 text-white' />
          </motion.div>

          <h2 className='text-5xl md:text-6xl lg:text-7xl font-bold mb-6'>
            <motion.span
              className='bg-clip-text text-transparent bg-gradient-to-r from-purple-400 via-blue-400 to-purple-400'
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
              Open Source
            </motion.span>{' '}
            & Community Driven
          </h2>

          <p className='text-xl text-gray-400 mb-12 leading-relaxed'>
            ServerEye is fully open source. Inspect the code, contribute features, and join our
            community of developers building the future of server monitoring.
          </p>

          <div className='grid grid-cols-1 sm:grid-cols-3 gap-6 mb-12'>
            {[
              { icon: Star, value: '1.2K+', label: 'GitHub Stars' },
              { icon: GitFork, value: '150+', label: 'Forks' },
              { icon: Users, value: '50+', label: 'Contributors' },
            ].map((stat, i) => (
              <motion.div
                key={i}
                initial={{ opacity: 0, y: 30, scale: 0.9 }}
                whileInView={{ opacity: 1, y: 0, scale: 1 }}
                viewport={{ once: true }}
                transition={{ delay: i * 0.15, duration: 0.6 }}
                whileHover={{
                  scale: 1.05,
                  y: -5,
                  boxShadow: '0 20px 40px rgba(168, 85, 247, 0.2)',
                }}
                className='bg-gray-900/50 border border-white/10 rounded-2xl p-6 hover:border-purple-500/50 transition-all duration-300'
              >
                <motion.div
                  animate={{ rotate: [0, 360] }}
                  transition={{ duration: 20, repeat: Infinity, ease: 'linear' }}
                  className='w-12 h-12 bg-purple-500/10 rounded-xl flex items-center justify-center mb-4 mx-auto'
                >
                  <stat.icon className='w-6 h-6 text-purple-400' />
                </motion.div>
                <motion.div
                  initial={{ opacity: 0, scale: 0.5 }}
                  whileInView={{ opacity: 1, scale: 1 }}
                  viewport={{ once: true }}
                  transition={{ delay: i * 0.15 + 0.3 }}
                  className='text-3xl font-bold mb-2 bg-clip-text text-transparent bg-gradient-to-r from-purple-400 to-blue-400'
                >
                  {stat.value}
                </motion.div>
                <div className='text-gray-400'>{stat.label}</div>
              </motion.div>
            ))}
          </div>

          <motion.div
            initial={{ opacity: 0, y: 30 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            transition={{ delay: 0.6 }}
            className='flex flex-col sm:flex-row gap-4 justify-center'
          >
            <motion.a
              href='https://github.com/godofphonk/ServerEye'
              target='_blank'
              rel='noopener noreferrer'
              whileHover={{ scale: 1.05 }}
              whileTap={{ scale: 0.95 }}
              className='inline-flex items-center gap-2 px-8 py-4 bg-white text-black rounded-full font-semibold hover:bg-gray-200 transition-all duration-300 shadow-lg shadow-white/20'
            >
              <GitBranch className='w-5 h-5' />
              View on GitHub
            </motion.a>
            <motion.button
              whileHover={{ scale: 1.05 }}
              whileTap={{ scale: 0.95 }}
              className='px-8 py-4 bg-white/10 hover:bg-white/20 backdrop-blur-sm border border-white/20 rounded-full font-semibold transition-all duration-300 flex items-center gap-2 justify-center'
            >
              <BookOpen className='w-5 h-5' />
              Read Documentation
            </motion.button>
          </motion.div>

          <motion.div
            initial={{ opacity: 0 }}
            whileInView={{ opacity: 1 }}
            viewport={{ once: true }}
            transition={{ delay: 0.8 }}
            className='mt-12 inline-flex items-center gap-3 px-6 py-3 bg-white/5 border border-white/10 rounded-full'
          >
            <Heart className='w-4 h-4 text-purple-400' />
            <span className='text-gray-400'>Licensed under </span>
            <span className='text-white font-semibold'>MIT License</span>
          </motion.div>
        </motion.div>
      </div>
    </section>
  );
}
