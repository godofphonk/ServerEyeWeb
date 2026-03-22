'use client';

import { useEffect, useState, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { motion } from 'framer-motion';
import { AlertCircle, ArrowLeft } from 'lucide-react';

function AuthContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const [message, setMessage] = useState('');

  useEffect(() => {
    const error = searchParams.get('error');
    const errorDescription = searchParams.get('error_description');
    
    // Temporarily disable auth error page - always redirect to login
    router.push('/login');
    
    // if (error === 'oauth_failed') {
    //   setMessage('OAuth authentication failed. Please try again.');
    // } else if (error) {
    //   setMessage(errorDescription || `Authentication error: ${error}`);
    // } else {
    //   // No error, redirect to login
    //   router.push('/login');
    // }
  }, [searchParams, router]);

  return (
    <main className='min-h-screen bg-black text-white flex items-center justify-center p-6'>
      {/* Background */}
      <div className='absolute inset-0 bg-gradient-to-br from-blue-600/20 via-purple-600/20 to-pink-600/20 opacity-50' />
      <div className='absolute inset-0 bg-[radial-gradient(ellipse_at_center,_var(--tw-gradient-stops))] from-blue-900/20 via-transparent to-transparent' />

      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className='relative z-10 w-full max-w-md'
      >
        <div className='bg-white/10 backdrop-blur-xl border border-white/20 rounded-3xl p-8 text-center'>
          {/* Error Icon */}
          <motion.div
            initial={{ scale: 0 }}
            animate={{ scale: 1 }}
            transition={{ delay: 0.2 }}
            className='w-20 h-20 mx-auto mb-6 bg-red-500/20 rounded-full flex items-center justify-center'
          >
            <AlertCircle className='w-12 h-12 text-red-400' />
          </motion.div>

          {/* Error Message */}
          <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.3 }}>
            <h1 className='text-2xl font-bold mb-3'>Authentication Failed</h1>
            <p className='text-gray-400 mb-6'>{message}</p>

            {/* Back to Login Button */}
            <motion.button
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              transition={{ delay: 0.4 }}
              onClick={() => router.push('/login')}
              className='inline-flex items-center gap-2 px-6 py-3 bg-blue-600 hover:bg-blue-700 rounded-xl transition-colors'
            >
              <ArrowLeft className='w-4 h-4' />
              Back to Login
            </motion.button>
          </motion.div>
        </div>
      </motion.div>
    </main>
  );
}

export default function AuthPage() {
  return (
    <Suspense fallback={
      <main className='min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-violet-900 flex items-center justify-center'>
        <div className='text-center'>
          <div className='w-12 h-12 border-4 border-blue-400 border-t-transparent rounded-full animate-spin mx-auto mb-4' />
          <h2 className='text-xl font-semibold text-white mb-2'>Loading...</h2>
          <p className='text-gray-300'>Please wait...</p>
        </div>
      </main>
    }>
      <AuthContent />
    </Suspense>
  );
}
