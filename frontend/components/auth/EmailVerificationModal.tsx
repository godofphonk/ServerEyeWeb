'use client';

import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Mail, X, RefreshCw, CheckCircle } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { authApi } from '@/lib/authApi';
import { useToast } from '@/hooks/useToast';

interface EmailVerificationModalProps {
  isOpen: boolean;
  onClose: () => void;
  email: string;
  onSuccess: () => void;
}

export function EmailVerificationModal({
  isOpen,
  onClose,
  email,
  onSuccess,
}: EmailVerificationModalProps) {
  const toast = useToast();
  const [code, setCode] = useState('');
  const [isVerifying, setIsVerifying] = useState(false);
  const [isResending, setIsResending] = useState(false);
  const [isVerified, setIsVerified] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (code.length !== 6) {
      toast.error('Invalid Code', 'Please enter a 6-digit verification code');
      return;
    }

    setIsVerifying(true);

    try {
      // Use verifyEmailWithoutAuth for registration flow (no auth required)
      await authApi.verifyEmailWithoutAuth({ email, code });
      setIsVerified(true);
      toast.success('Email Verified', 'Your email has been successfully verified!');

      setTimeout(() => {
        onSuccess();
        onClose();
      }, 1500);
    } catch (error: any) {
      // eslint-disable-line @typescript-eslint/no-explicit-any
      const errorMessage =
        error?.response?.data?.message || error?.message || 'Verification failed';

      toast.error('Verification Failed', errorMessage);
    } finally {
      setIsVerifying(false);
    }
  };

  const handleResend = async () => {
    setIsResending(true);

    try {
      await authApi.resendVerification({ email });
      toast.success('Code Resent', 'A new verification code has been sent to your email');
    } catch (error: any) {
      // eslint-disable-line @typescript-eslint/no-explicit-any
      const errorMessage =
        error?.response?.data?.message || error?.message || 'Failed to resend code';

      toast.error('Resend Failed', errorMessage);
    } finally {
      setIsResending(false);
    }
  };

  const handleCodeChange = (value: string) => {
    // Only allow numbers and max 6 digits
    const numericValue = value.replace(/\D/g, '').slice(0, 6);
    setCode(numericValue);
  };

  if (!isOpen) return null;

  return (
    <AnimatePresence>
      <div className='fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm'>
        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          exit={{ opacity: 0, scale: 0.95 }}
          className='w-full max-w-md mx-4'
        >
          <div className='bg-black/90 backdrop-blur-xl border border-white/10 rounded-2xl p-6'>
            {/* Header */}
            <div className='flex items-center justify-between mb-6'>
              <div className='flex items-center gap-3'>
                <div className='w-12 h-12 bg-blue-500/20 rounded-full flex items-center justify-center'>
                  {isVerified ? (
                    <CheckCircle className='w-6 h-6 text-green-400' />
                  ) : (
                    <Mail className='w-6 h-6 text-blue-400' />
                  )}
                </div>
                <div>
                  <h3 className='text-xl font-bold text-white'>
                    {isVerified ? 'Email Verified!' : 'Verify Your Email'}
                  </h3>
                  <p className='text-sm text-gray-400'>
                    {isVerified
                      ? 'Your email has been successfully verified'
                      : `We sent a code to ${email}`}
                  </p>
                </div>
              </div>
              {!isVerified && (
                <button
                  onClick={onClose}
                  className='text-gray-400 hover:text-white transition-colors'
                >
                  <X className='w-5 h-5' />
                </button>
              )}
            </div>

            {/* Content */}
            {!isVerified ? (
              <form onSubmit={handleSubmit} className='space-y-6'>
                {/* 6-digit code input */}
                <div>
                  <Input
                    label='Verification Code'
                    type='text'
                    value={code}
                    onChange={e => handleCodeChange(e.target.value)}
                    placeholder='Enter 6-digit code'
                    maxLength={6}
                    className='text-center text-2xl tracking-widest'
                    required
                    disabled={isVerifying}
                  />
                  <p className='text-xs text-gray-400 mt-2 text-center'>
                    Check your email for the verification code
                  </p>
                </div>

                {/* Resend link */}
                <div className='text-center'>
                  <button
                    type='button'
                    onClick={handleResend}
                    disabled={isResending}
                    className='text-sm text-blue-400 hover:text-blue-300 transition-colors disabled:opacity-50'
                  >
                    {isResending ? (
                      <span className='flex items-center gap-2'>
                        <RefreshCw className='w-4 h-4 animate-spin' />
                        Sending...
                      </span>
                    ) : (
                      "Didn't receive the code? Resend"
                    )}
                  </button>
                </div>

                {/* Submit button */}
                <Button
                  type='submit'
                  fullWidth
                  isLoading={isVerifying}
                  disabled={code.length !== 6}
                >
                  Verify Email
                </Button>
              </form>
            ) : (
              <div className='text-center py-4'>
                <CheckCircle className='w-16 h-16 text-green-400 mx-auto mb-4' />
                <p className='text-gray-300'>You can now access all features of your account.</p>
              </div>
            )}
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
}
