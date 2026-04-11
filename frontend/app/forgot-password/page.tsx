'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { motion } from 'framer-motion';
import { Mail, ArrowLeft, CheckCircle } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { authApi } from '@/lib/authApi';
import { useToast } from '@/hooks/useToast';

export default function ForgotPasswordPage() {
  const router = useRouter();
  const toast = useToast();
  const [email, setEmail] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!email.trim()) {
      toast.error('Email Required', 'Please enter your email address');
      return;
    }

    if (!email.includes('@')) {
      toast.error('Invalid Email', 'Please enter a valid email address');
      return;
    }

    setIsLoading(true);

    try {
      await authApi.forgotPassword({ email: email.trim() });
      setIsSubmitted(true);

      toast.success(
        'Reset Link Sent',
        'If an account with this email exists, you will receive a password reset link',
      );
    } catch (error: any) {
      // eslint-disable-line @typescript-eslint/no-explicit-any
      const errorMessage =
        error?.response?.data?.message || error?.message || 'Failed to send reset link';

      toast.error('Request Failed', errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main className='min-h-screen bg-black text-white flex items-center justify-center'>
      <div className='absolute inset-0 bg-gradient-to-br from-blue-600/10 via-purple-600/10 to-pink-600/10' />

      <div className='relative z-10 w-full max-w-md mx-4'>
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
        >
          {/* Back button */}
          <Button variant='secondary' onClick={() => router.back()} className='mb-6'>
            <ArrowLeft className='w-4 h-4 mr-2' />
            Back
          </Button>

          <Card>
            <CardHeader className='text-center'>
              <div className='w-16 h-16 bg-blue-500/20 rounded-full flex items-center justify-center mx-auto mb-4'>
                <Mail className='w-8 h-8 text-blue-400' />
              </div>
              <CardTitle className='text-2xl'>Forgot Password</CardTitle>
              <p className='text-gray-400 mt-2'>
                Enter your email address and we'll send you a link to reset your password
              </p>
            </CardHeader>

            <CardContent>
              {!isSubmitted ? (
                <form onSubmit={handleSubmit} className='space-y-6'>
                  <Input
                    label='Email Address'
                    type='email'
                    value={email}
                    onChange={e => setEmail(e.target.value)}
                    placeholder='Enter your email'
                    required
                    disabled={isLoading}
                  />

                  <Button type='submit' fullWidth isLoading={isLoading} disabled={!email.trim()}>
                    Send Reset Link
                  </Button>
                </form>
              ) : (
                <motion.div
                  initial={{ opacity: 0, scale: 0.95 }}
                  animate={{ opacity: 1, scale: 1 }}
                  className='text-center py-6'
                >
                  <CheckCircle className='w-16 h-16 text-green-400 mx-auto mb-4' />
                  <h3 className='text-xl font-semibold mb-2'>Check Your Email</h3>
                  <p className='text-gray-400 mb-6'>We've sent a password reset link to:</p>
                  <div className='bg-white/10 rounded-lg p-3 mb-6'>
                    <p className='font-mono text-sm'>{email}</p>
                  </div>
                  <p className='text-sm text-gray-400'>
                    Didn't receive the email? Check your spam folder or try again.
                  </p>

                  <div className='mt-6 space-y-3'>
                    <Button
                      variant='secondary'
                      fullWidth
                      onClick={() => {
                        setIsSubmitted(false);
                        setEmail('');
                      }}
                    >
                      Try Different Email
                    </Button>

                    <Button variant='ghost' fullWidth onClick={() => router.push('/login')}>
                      Back to Login
                    </Button>
                  </div>
                </motion.div>
              )}
            </CardContent>
          </Card>
        </motion.div>
      </div>
    </main>
  );
}
