'use client';

import { motion } from 'framer-motion';
import { Mail } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/Card';

export default function ContactPage() {
  return (
    <main className='min-h-screen bg-black text-white'>
      <div className='absolute inset-0 bg-gradient-to-br from-blue-600/10 via-purple-600/10 to-pink-600/10' />

      <div className='relative z-10'>
        <div className='border-b border-white/10 bg-black/50 backdrop-blur-xl'>
          <div className='container mx-auto px-6 py-12'>
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ type: 'spring', stiffness: 300, damping: 30 }}
              className='text-center max-w-3xl mx-auto'
            >
              <h1 className='text-5xl md:text-6xl font-bold mb-6'>Contact Us</h1>
              <p className='text-xl text-gray-400'>Get in touch with our team</p>
            </motion.div>
          </div>
        </div>

        <div className='container mx-auto px-6 py-16'>
          <div className='max-w-2xl mx-auto'>
            <Card className='border border-white/5 shadow-lg'>
              <CardContent className='p-12 text-center'>
                <div className='flex items-center justify-center gap-3 mb-6'>
                  <Mail className='w-8 h-8 text-blue-400' />
                  <h2 className='text-2xl font-bold'>Email Support</h2>
                </div>
                <a
                  href='mailto:support@servereye.dev'
                  className='text-3xl font-bold text-blue-400 hover:text-blue-300 transition-colors mb-4 block'
                >
                  support@servereye.dev
                </a>
                <p className='text-gray-400'>
                  For all questions and inquiries, please contact us via email.
                </p>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </main>
  );
}
