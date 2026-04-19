'use client';

import { motion } from 'framer-motion';
import { Shield, Lock, Key, Eye, CheckCircle } from 'lucide-react';

export default function Security() {
  const features = [
    {
      icon: Shield,
      title: 'End-to-End Encryption',
      description:
        'All data transmitted between your servers and our platform is encrypted with TLS 1.3',
    },
    {
      icon: Lock,
      title: 'Zero Trust Architecture',
      description: 'Your servers never expose ports. Agent connects outbound via HTTPS only',
    },
    {
      icon: Key,
      title: 'JWT Authentication',
      description: 'Secure authentication with industry-standard JSON Web Tokens',
    },
    {
      icon: Eye,
      title: 'No Data Retention',
      description: "We don't store your sensitive data. Metrics are kept for 30 days only",
    },
  ];

  return (
    <section className='py-32 relative overflow-hidden'>
      <div className='absolute inset-0 bg-gradient-to-b from-transparent via-green-900/5 to-transparent' />

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
            className='inline-flex items-center gap-2 px-4 py-2 rounded-full bg-green-500/10 border border-green-500/20 mb-6'
          >
            <CheckCircle className='w-4 h-4 text-green-400' />
            <span className='text-sm text-green-300 font-medium'>Enterprise-Grade Security</span>
          </motion.div>
          <h2 className='text-5xl md:text-6xl lg:text-7xl font-bold mb-6'>
            Your Data Is
            <motion.span
              className='bg-clip-text text-transparent bg-gradient-to-r from-green-400 via-emerald-400 to-teal-400'
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
              Safe With Us
            </motion.span>
          </h2>
          <p className='text-xl text-gray-400 max-w-2xl mx-auto'>
            Security is our top priority. We follow industry best practices to protect your
            infrastructure.
          </p>
        </motion.div>

        <div className='grid grid-cols-1 md:grid-cols-2 gap-8 max-w-5xl mx-auto'>
          {features.map((feature, i) => (
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
                boxShadow: '0 20px 40px rgba(34, 197, 94, 0.2)',
              }}
              className='group relative bg-gray-900/50 border border-white/10 rounded-2xl p-8 hover:border-green-500/50 transition-all duration-300'
              style={{ transformStyle: 'preserve-3d' }}
            >
              <motion.div className='absolute inset-0 bg-gradient-to-br from-green-600/10 to-emerald-600/10 opacity-0 group-hover:opacity-100 transition-opacity rounded-2xl' />
              <div className='relative' style={{ transform: 'translateZ(20px)' }}>
                <motion.div
                  animate={{ rotate: [0, 5, -5, 0] }}
                  transition={{ duration: 4, repeat: Infinity, ease: 'easeInOut' }}
                  className='w-16 h-16 bg-green-500/10 border border-green-500/20 rounded-2xl flex items-center justify-center mb-6 group-hover:scale-110 transition-transform'
                >
                  <feature.icon className='w-8 h-8 text-green-400' />
                </motion.div>
                <h3 className='text-2xl font-bold mb-3 group-hover:text-green-300 transition-colors'>
                  {feature.title}
                </h3>
                <p className='text-gray-400 leading-relaxed group-hover:text-gray-300 transition-colors'>
                  {feature.description}
                </p>
              </div>
            </motion.div>
          ))}
        </div>

        <motion.div
          initial={{ opacity: 0 }}
          whileInView={{ opacity: 1 }}
          viewport={{ once: true }}
          transition={{ delay: 0.8 }}
          className='flex flex-wrap justify-center gap-6 mt-16'
        >
          {['SOC 2 Type II', 'GDPR Compliant', 'ISO 27001', 'HTTPS Only'].map((badge, i) => (
            <motion.div
              key={i}
              initial={{ opacity: 0, scale: 0.9 }}
              whileInView={{ opacity: 1, scale: 1 }}
              viewport={{ once: true }}
              transition={{ delay: 0.9 + i * 0.1 }}
              whileHover={{ scale: 1.05, y: -5 }}
              className='px-6 py-3 bg-white/5 border border-white/10 rounded-full text-gray-300 font-semibold hover:border-green-500/50 hover:text-green-300 transition-all cursor-default'
            >
              {badge}
            </motion.div>
          ))}
        </motion.div>
      </div>
    </section>
  );
}
