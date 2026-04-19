'use client';

import { useState, useEffect, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { motion } from 'framer-motion';
import { Lock, ArrowLeft, CheckCircle, AlertTriangle } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { authApi } from '@/lib/authApi';
import { useToast } from '@/hooks/useToast';
import { AxiosApiError } from '@/types';

function ResetPasswordForm() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const toast = useToast();

  const [token, setToken] = useState<string | null>(null);
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const tokenFromUrl = searchParams.get('token');
    if (!tokenFromUrl) {
      setError('Invalid or missing reset token');
      toast.error('Invalid Token', 'The reset token is invalid or has expired');
    } else {
      setToken(tokenFromUrl);
    }
  }, [searchParams, toast]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!token) return;

    if (newPassword.length < 8) {
      toast.error('Weak Password', 'Password must be at least 8 characters long');
      return;
    }

    if (newPassword !== confirmPassword) {
      toast.error('Password Mismatch', 'Passwords do not match');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      await authApi.resetPassword({
        token,
        newPassword,
      });

      setIsSuccess(true);
      toast.success('Password Reset', 'Your password has been successfully reset');

      // Redirect to login after 2 seconds
      setTimeout(() => {
        router.push('/login');
      }, 2000);
    } catch (error: unknown) {
      const errorMessage =
        (error as AxiosApiError)?.response?.data?.message ||
        (error as AxiosApiError)?.message ||
        'Failed to reset password';
      setError(errorMessage);

      toast.error('Reset Failed', errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  if (error && !isSuccess) {
    return (
      <main className='min-h-screen bg-black text-white flex items-center justify-center'>
        <div className='absolute inset-0 bg-gradient-to-br from-red-600/10 via-orange-600/10 to-pink-600/10' />

        <div className='relative z-10 w-full max-w-md mx-4'>
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ type: 'spring', stiffness: 300, damping: 30 }}
          >
            <motion.div whileHover={{ scale: 1.02 }}>
              <Card className='border border-white/5 hover:border-white/10 shadow-lg hover:shadow-xl hover:shadow-red-500/20 transition-all duration-300'>
                <CardHeader className='text-center'>
                  <motion.div
                    whileHover={{ rotate: 360, scale: 1.1 }}
                    transition={{ duration: 0.5 }}
                    className='w-16 h-16 bg-red-500/20 border border-red-500/30 rounded-full flex items-center justify-center mx-auto mb-4 shadow-lg shadow-red-500/30'
                  >
                    <AlertTriangle className='w-8 h-8 text-red-400' />
                  </motion.div>
                  <CardTitle className='text-2xl text-red-400'>Invalid Reset Link</CardTitle>
                  <p className='text-gray-400 mt-2'>
                    This password reset link is invalid or has expired.
                  </p>
                </CardHeader>

                <CardContent className='text-center'>
                  <p className='text-sm text-gray-400 mb-6'>
                    Please request a new password reset link.
                  </p>

                  <div className='space-y-3'>
                    <Button onClick={() => router.push('/forgot-password')} fullWidth>
                      Request New Link
                    </Button>

                    <Button variant='ghost' onClick={() => router.push('/login')} fullWidth>
                      Back to Login
                    </Button>
                  </div>
                </CardContent>
              </Card>
            </motion.div>
          </motion.div>
        </div>
      </main>
    );
  }

  if (isSuccess) {
    return (
      <main className='min-h-screen bg-black text-white flex items-center justify-center'>
        <div className='absolute inset-0 bg-gradient-to-br from-green-600/10 via-emerald-600/10 to-teal-600/10' />

        <div className='relative z-10 w-full max-w-md mx-4'>
          <motion.div
            initial={{ opacity: 0, scale: 0.95 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ type: 'spring', stiffness: 300, damping: 30 }}
            className='text-center'
          >
            <motion.div
              animate={{ scale: [1, 1.1, 1] }}
              transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
            >
              <CheckCircle className='w-20 h-20 text-green-400 mx-auto mb-6 shadow-lg shadow-green-500/30' />
            </motion.div>
            <h2 className='text-3xl font-bold mb-4'>Password Reset Successful</h2>
            <p className='text-gray-400 mb-8'>
              Your password has been successfully reset. You will be redirected to the login page.
            </p>

            <motion.button whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}>
              <Button onClick={() => router.push('/login')} size='lg'>
                Go to Login
              </Button>
            </motion.button>
          </motion.div>
        </div>
      </main>
    );
  }

  return (
    <main className='min-h-screen bg-black text-white flex items-center justify-center'>
      <div className='absolute inset-0 bg-gradient-to-br from-blue-600/10 via-purple-600/10 to-pink-600/10' />

      <div className='relative z-10 w-full max-w-md mx-4'>
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ type: 'spring', stiffness: 300, damping: 30 }}
        >
          {/* Back button */}
          <motion.div whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}>
            <Button variant='secondary' onClick={() => router.back()} className='mb-6'>
              <ArrowLeft className='w-4 h-4 mr-2' />
              Back
            </Button>
          </motion.div>

          <motion.div whileHover={{ scale: 1.02 }}>
            <Card className='border border-white/5 hover:border-white/10 shadow-lg hover:shadow-xl hover:shadow-blue-500/20 transition-all duration-300'>
              <CardHeader className='text-center'>
                <motion.div
                  whileHover={{ rotate: 360, scale: 1.1 }}
                  transition={{ duration: 0.5 }}
                  className='w-16 h-16 bg-blue-500/20 border border-blue-500/30 rounded-full flex items-center justify-center mx-auto mb-4 shadow-lg shadow-blue-500/30'
                >
                  <Lock className='w-8 h-8 text-blue-400' />
                </motion.div>
                <CardTitle className='text-2xl'>Reset Password</CardTitle>
                <p className='text-gray-400 mt-2'>Enter your new password below</p>
              </CardHeader>

              <CardContent>
                <form onSubmit={handleSubmit} className='space-y-6'>
                  <Input
                    label='New Password'
                    type='password'
                    value={newPassword}
                    onChange={e => setNewPassword(e.target.value)}
                    placeholder='Enter new password'
                    required
                    disabled={isLoading}
                    minLength={8}
                  />

                  <Input
                    label='Confirm Password'
                    type='password'
                    value={confirmPassword}
                    onChange={e => setConfirmPassword(e.target.value)}
                    placeholder='Confirm new password'
                    required
                    disabled={isLoading}
                    minLength={8}
                  />

                  <div className='bg-white/5 rounded-lg p-4'>
                    <p className='text-sm text-gray-400 mb-2'>Password requirements:</p>
                    <ul className='text-xs text-gray-500 space-y-1'>
                      <li>• At least 8 characters long</li>
                      <li>• Contains letters and numbers</li>
                      <li>• Not easily guessable</li>
                    </ul>
                  </div>

                  <Button
                    type='submit'
                    fullWidth
                    isLoading={isLoading}
                    disabled={!newPassword || !confirmPassword || newPassword !== confirmPassword}
                  >
                    Reset Password
                  </Button>
                </form>

                <div className='mt-6 text-center'>
                  <Button variant='ghost' onClick={() => router.push('/login')} className='text-sm'>
                    Back to Login
                  </Button>
                </div>
              </CardContent>
            </Card>
          </motion.div>
        </motion.div>
      </div>
    </main>
  );
}

export default function ResetPasswordPage() {
  return (
    <Suspense
      fallback={<div className='min-h-screen flex items-center justify-center'>Loading...</div>}
    >
      <ResetPasswordForm />
    </Suspense>
  );
}
