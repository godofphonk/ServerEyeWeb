'use client';

import { motion } from 'framer-motion';
import { Github, Star, GitFork, Users } from 'lucide-react';

export default function OpenSource() {
  return (
    <section className='py-32 relative overflow-hidden'>
      {/* Background gradient */}
      <div className='absolute inset-0 bg-gradient-to-br from-purple-600/10 via-transparent to-blue-600/10' />

      <div className='container mx-auto px-6 relative z-10'>
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          className='max-w-4xl mx-auto text-center'
        >
          <Github className='w-20 h-20 mx-auto mb-8 text-white' />

          <h2 className='text-5xl md:text-6xl font-bold mb-6'>
            <span className='bg-clip-text text-transparent bg-gradient-to-r from-purple-400 to-blue-400'>
              Open Source
            </span>{' '}
            & Community Driven
          </h2>

          <p className='text-xl text-gray-400 mb-12 leading-relaxed'>
            ServerEye is fully open source. Inspect the code, contribute features, and join our
            community of developers building the future of server monitoring.
          </p>

          {/* GitHub stats */}
          <div className='grid grid-cols-1 sm:grid-cols-3 gap-6 mb-12'>
            {[
              { icon: Star, value: '1.2K+', label: 'GitHub Stars' },
              { icon: GitFork, value: '150+', label: 'Forks' },
              { icon: Users, value: '50+', label: 'Contributors' },
            ].map((stat, i) => (
              <motion.div
                key={i}
                initial={{ opacity: 0, scale: 0.9 }}
                whileInView={{ opacity: 1, scale: 1 }}
                viewport={{ once: true }}
                transition={{ delay: i * 0.1 }}
                className='bg-gradient-to-br from-gray-900 to-black border border-white/10 rounded-2xl p-6'
              >
                <stat.icon className='w-10 h-10 text-purple-400 mx-auto mb-4' />
                <div className='text-3xl font-bold mb-2'>{stat.value}</div>
                <div className='text-gray-400'>{stat.label}</div>
              </motion.div>
            ))}
          </div>

          {/* CTA */}
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true }}
            className='flex flex-col sm:flex-row gap-4 justify-center'
          >
            <a
              href='https://github.com/godofphonk/ServerEye'
              target='_blank'
              rel='noopener noreferrer'
              className='inline-flex items-center gap-2 px-8 py-4 bg-white text-black rounded-full font-semibold hover:bg-gray-200 transition-all duration-300 hover:scale-105'
            >
              <Github className='w-5 h-5' />
              View on GitHub
            </a>
            <button className='px-8 py-4 bg-white/10 hover:bg-white/20 backdrop-blur-sm border border-white/20 rounded-full font-semibold transition-all duration-300'>
              Read Documentation
            </button>
          </motion.div>

          {/* License badge */}
          <div className='mt-12 inline-block px-6 py-3 bg-white/5 border border-white/10 rounded-full'>
            <span className='text-gray-400'>Licensed under </span>
            <span className='text-white font-semibold'>MIT License</span>
          </div>
        </motion.div>
      </div>
    </section>
  );
}
