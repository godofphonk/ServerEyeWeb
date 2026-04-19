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
  onSuccess: (code: string) => void;
  resendCooldown?: number;
  onResend?: () => void;
}

export function EmailVerificationModal({
  isOpen,
  onClose,
  email,
  onSuccess,
  resendCooldown = 0,
  onResend,
}: EmailVerificationModalProps) {
  const toast = useToast();
  const [code, setCode] = useState('');
  const [isVerifying, setIsVerifying] = useState(false);
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
        onSuccess(code);
        onClose();
      }, 1500);
    } catch (error: unknown) {
      const errorMessage =
        (error as { response?: { data?: { message?: string } }; message?: string })?.response?.data
          ?.message ||
        (error as { message?: string })?.message ||
        'Invalid or expired verification code';

      toast.error('Verification Failed', errorMessage);
      // НЕ вызываем onSuccess и НЕ закрываем модал при ошибке
    } finally {
      setIsVerifying(false);
    }
  };

  const handleResend = () => {
    if (onResend) {
      onResend();
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
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 20 }}
          transition={{ type: 'spring', stiffness: 300, damping: 30 }}
          className='w-full max-w-md mx-4'
        >
          <div className='bg-black/90 backdrop-blur-xl border border-blue-500/20 rounded-2xl p-6 relative overflow-hidden shadow-2xl shadow-blue-500/20'>
            <motion.div
              className='absolute inset-0 bg-gradient-to-br from-blue-500/5 to-purple-500/5'
              animate={{ opacity: [0, 1, 0] }}
              transition={{ duration: 3, repeat: Infinity, ease: 'easeInOut' }}
            />
            {/* Header */}
            <div className='flex items-center justify-between mb-6 relative z-10'>
              <motion.div whileHover={{ scale: 1.02 }} className='flex items-center gap-3'>
                <motion.div
                  animate={isVerified ? { scale: [1, 1.1, 1] } : { rotate: [0, 360] }}
                  transition={{
                    duration: isVerified ? 2 : 20,
                    repeat: Infinity,
                    ease: 'easeInOut',
                  }}
                  className={`w-12 h-12 rounded-full flex items-center justify-center ${
                    isVerified ? 'bg-green-500/20' : 'bg-blue-500/20'
                  } border ${isVerified ? 'border-green-500/30' : 'border-blue-500/30'}`}
                >
                  {isVerified ? (
                    <CheckCircle className='w-6 h-6 text-green-400' />
                  ) : (
                    <Mail className='w-6 h-6 text-blue-400' />
                  )}
                </motion.div>
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
              </motion.div>
              {!isVerified && (
                <motion.button
                  whileHover={{ scale: 1.1, rotate: 90 }}
                  whileTap={{ scale: 0.9 }}
                  onClick={onClose}
                  className='text-gray-400 hover:text-white transition-colors'
                >
                  <X className='w-5 h-5' />
                </motion.button>
              )}
            </div>

            {/* Content */}
            {!isVerified ? (
              <motion.form
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                onSubmit={handleSubmit}
                className='space-y-6 relative z-10'
              >
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
                  <motion.button
                    whileHover={{ scale: 1.05 }}
                    whileTap={{ scale: 0.95 }}
                    type='button'
                    onClick={handleResend}
                    disabled={resendCooldown > 0}
                    className='text-sm text-blue-400 hover:text-blue-300 transition-colors disabled:opacity-50'
                  >
                    {resendCooldown > 0 ? (
                      <span className='flex items-center gap-2'>
                        <motion.div
                          animate={{ rotate: 360 }}
                          transition={{ duration: 1, repeat: Infinity, ease: 'linear' }}
                        >
                          <RefreshCw className='w-4 h-4' />
                        </motion.div>
                        Wait {resendCooldown}s
                      </span>
                    ) : (
                      "Didn't receive the code? Resend"
                    )}
                  </motion.button>
                </div>

                {/* Submit button */}
                <Button
                  type='submit'
                  fullWidth
                  isLoading={isVerifying}
                  disabled={code.length !== 6}
                  className='shadow-lg shadow-blue-500/30'
                >
                  Verify Email
                </Button>
              </motion.form>
            ) : (
              <motion.div
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                className='text-center py-4 relative z-10'
              >
                <motion.div
                  animate={{ scale: [1, 1.2, 1] }}
                  transition={{ duration: 1, repeat: Infinity, ease: 'easeInOut' }}
                  className='w-16 h-16 bg-green-500/20 rounded-full flex items-center justify-center mx-auto mb-4 border border-green-500/30 shadow-lg shadow-green-500/30'
                >
                  <CheckCircle className='w-10 h-10 text-green-400' />
                </motion.div>
                <p className='text-gray-300'>You can now access all features of your account.</p>
              </motion.div>
            )}
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
}
