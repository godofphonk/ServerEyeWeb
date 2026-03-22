'use client';

import { useEffect, useState } from 'react';
import { useRouter, useSearchParams, usePathname } from 'next/navigation';
import { useAuth } from '@/context/AuthContext';
import { motion } from 'framer-motion';
import { AlertCircle, CheckCircle } from 'lucide-react';

export default function OAuthCallbackPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const pathname = usePathname();
  const { loginWithOAuth } = useAuth();
  const [status, setStatus] = useState<'loading' | 'success' | 'error' | 'processing'>('loading');
  const [message, setMessage] = useState('');

  useEffect(() => {
    const handleOAuthCallback = async () => {
      const code = searchParams.get('code');
      const state = searchParams.get('state');
      const provider = pathname.split('/')[2]; // Extract provider from URL

      if (!code || !state) {
        // Don't show error - just wait for redirect or login
        setStatus('processing');
        setMessage('Processing OAuth...');
        setTimeout(() => router.push('/login'), 5000);
        return;
      }

      try {
        await loginWithOAuth(provider, code, state);
        setStatus('success');
        setMessage('Successfully authenticated!');
        setTimeout(() => router.push('/dashboard'), 2000);
      } catch (error: any) {
        setStatus('error');
        setMessage(error.message || 'Authentication failed');
        setTimeout(() => router.push('/login'), 3000);
      }
    };

    handleOAuthCallback();
  }, [searchParams, router, loginWithOAuth]);

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
          {/* Status Icon */}
          <motion.div
            initial={{ scale: 0 }}
            animate={{ scale: 1 }}
            transition={{ delay: 0.2 }}
            className='w-20 h-20 mx-auto mb-6 rounded-full flex items-center justify-center'
          >
            {status === 'loading' && (
              <div className='w-12 h-12 border-4 border-blue-400 border-t-transparent rounded-full animate-spin' />
            )}
            {status === 'success' && (
              <div className='w-20 h-20 bg-green-500/20 rounded-full flex items-center justify-center'>
                <CheckCircle className='w-12 h-12 text-green-400' />
              </div>
            )}
            {status === 'error' && (
              <div className='w-20 h-20 bg-red-500/20 rounded-full flex items-center justify-center'>
                <AlertCircle className='w-12 h-12 text-red-400' />
              </div>
            )}
          </motion.div>

          {/* Status Message */}
          <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={{ delay: 0.3 }}>
            <h1 className='text-2xl font-bold mb-3'>
              {status === 'loading' && 'Authenticating...'}
              {status === 'success' && 'Processing authentication...'}
              {status === 'error' && 'Processing authentication...'}
            </h1>

            <p className='text-gray-400 mb-6'>
              {status === 'loading' && 'Please wait while we complete your authentication...'}
              {status === 'success' && message}
              {status === 'error' && message}
            </p>

            {/* Redirect Notice */}
            <div className='text-sm text-gray-500'>
              {status !== 'loading' && <p>Redirecting you in a few seconds...</p>}
            </div>
          </motion.div>
        </div>
      </motion.div>
    </main>
  );
}
