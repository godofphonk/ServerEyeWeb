'use client';

import { useState } from 'react';
import { motion } from 'framer-motion';
import { Mail, Lock, ArrowRight, Github, AlertCircle } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/context/AuthContext';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';

export default function LoginPage() {
  const router = useRouter();
  const { login, getOAuthURL } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isOAuthLoading, setIsOAuthLoading] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    try {
      await login(email, password);
      router.push('/dashboard');
    } catch (err: any) {
      setError(err.message || 'Invalid email or password');
    } finally {
      setIsLoading(false);
    }
  };

  const handleOAuthLogin = async (provider: string) => {
    setError('');
    setIsOAuthLoading(provider);

    try {
      const authURL = await getOAuthURL(provider);
      window.location.href = authURL;
    } catch (err: any) {
      setError(err.message || `Failed to authenticate with ${provider}`);
      setIsOAuthLoading(null);
    }
  };

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
        <div className='bg-white/10 backdrop-blur-xl border border-white/20 rounded-3xl p-8'>
          {/* Header */}
          <div className='text-center mb-8'>
            <Link href='/' className='inline-block mb-6'>
              <div className='text-3xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent'>
                ServerEye
              </div>
            </Link>
            <h1 className='text-3xl font-bold mb-2'>Welcome Back</h1>
            <p className='text-gray-400'>Sign in to your account</p>
          </div>

          {/* Error Message */}
          {error && (
            <motion.div
              initial={{ opacity: 0, y: -10 }}
              animate={{ opacity: 1, y: 0 }}
              className='mb-6 p-4 bg-red-500/10 border border-red-500/20 rounded-xl flex items-center gap-3'
            >
              <AlertCircle className='w-5 h-5 text-red-400 flex-shrink-0' />
              <p className='text-sm text-red-400'>{error}</p>
            </motion.div>
          )}

          {/* Form */}
          <form onSubmit={handleSubmit} className='space-y-6'>
            <Input
              type='email'
              label='Email'
              placeholder='you@example.com'
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
              disabled={isLoading}
            />

            <Input
              type='password'
              label='Password'
              placeholder='••••••••'
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
              disabled={isLoading}
            />

            <div className='flex items-center justify-between text-sm'>
              <label className='flex items-center gap-2 cursor-pointer'>
                <input type='checkbox' className='rounded' />
                <span className='text-gray-400'>Remember me</span>
              </label>
              <Link href='/forgot-password' className='text-blue-400 hover:text-blue-300'>
                Forgot password?
              </Link>
            </div>

            <Button type='submit' fullWidth isLoading={isLoading}>
              <span className='flex items-center gap-2'>
                Sign In
                <ArrowRight className='w-5 h-5' />
              </span>
            </Button>
          </form>

          {/* Divider */}
          <div className='my-8 flex items-center gap-4'>
            <div className='flex-1 h-px bg-white/10' />
            <span className='text-sm text-gray-400'>Or continue with</span>
            <div className='flex-1 h-px bg-white/10' />
          </div>

          {/* OAuth Buttons */}
          <div className='grid grid-cols-2 gap-4'>
            <Button
              variant='secondary'
              disabled={isOAuthLoading === 'github' || isLoading}
              onClick={() => handleOAuthLogin('github')}
            >
              {isOAuthLoading === 'github' ? (
                <div className='w-5 h-5 border-2 border-gray-400 border-t-transparent rounded-full animate-spin mr-2' />
              ) : (
                <Github className='w-5 h-5 mr-2' />
              )}
              GitHub
            </Button>
            <Button
              variant='secondary'
              disabled={isOAuthLoading === 'google' || isLoading}
              onClick={() => handleOAuthLogin('google')}
            >
              {isOAuthLoading === 'google' ? (
                <div className='w-5 h-5 border-2 border-gray-400 border-t-transparent rounded-full animate-spin mr-2' />
              ) : (
                <svg className='w-5 h-5 mr-2' viewBox='0 0 24 24'>
                  <path
                    fill='currentColor'
                    d='M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z'
                  />
                  <path
                    fill='currentColor'
                    d='M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z'
                  />
                  <path
                    fill='currentColor'
                    d='M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z'
                  />
                  <path
                    fill='currentColor'
                    d='M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z'
                  />
                </svg>
              )}
              Google
            </Button>
          </div>

          {/* Sign Up Link */}
          <div className='mt-8 text-center text-sm'>
            <span className='text-gray-400'>Don't have an account? </span>
            <Link href='/register' className='text-blue-400 hover:text-blue-300 font-semibold'>
              Sign up
            </Link>
          </div>
        </div>
      </motion.div>
    </main>
  );
}
