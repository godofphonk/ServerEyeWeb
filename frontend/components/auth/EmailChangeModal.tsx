'use client';

import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Mail, X, RefreshCw, CheckCircle } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { authApi } from '@/lib/authApi';
import { useToast } from '@/hooks/useToast';
import { AxiosApiError } from '@/types';

interface EmailChangeModalProps {
  isOpen: boolean;
  onClose: () => void;
  currentEmail: string;
  onSuccess: (newEmail: string) => void;
}

export function EmailChangeModal({
  isOpen,
  onClose,
  currentEmail,
  onSuccess,
}: EmailChangeModalProps) {
  const toast = useToast();
  const [newEmail, setNewEmail] = useState('');
  const [verificationCode, setVerificationCode] = useState('');
  const [step, setStep] = useState<'input' | 'verify'>('input');
  const [isLoading, setIsLoading] = useState(false);
  const [isVerifying, setIsVerifying] = useState(false);
  const [isVerified, setIsVerified] = useState(false);

  // Check if this is an OAuth user without email
  const isOAuthUser = !currentEmail || currentEmail.trim() === '';

  const handleSubmitEmail = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!newEmail.trim()) {
      toast.error('Email Required', 'Please enter your email address');
      return;
    }

    // For OAuth users, any email is valid (they don't have current email)
    if (!isOAuthUser && newEmail === currentEmail) {
      toast.error('Same Email', 'New email must be different from current email');
      return;
    }

    if (!newEmail.includes('@')) {
      toast.error('Invalid Email', 'Please enter a valid email address');
      return;
    }

    setIsLoading(true);

    try {
      await authApi.changeEmail({ newEmail: newEmail.trim() });
      setStep('verify');

      toast.success('Verification Code Sent', `A verification code has been sent to ${newEmail}`);
    } catch (error: unknown) {
      const errorMessage =
        (error as AxiosApiError)?.response?.data?.message ||
        (error as AxiosApiError)?.message ||
        'Failed to send verification code';

      toast.error('Request Failed', errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  const handleVerifyCode = async (e: React.FormEvent) => {
    e.preventDefault();

    if (verificationCode.length !== 6) {
      toast.error('Invalid Code', 'Please enter a 6-digit verification code');
      return;
    }

    setIsVerifying(true);

    try {
      await authApi.confirmEmailChange({ code: verificationCode });
      setIsVerified(true);

      const successMessage = isOAuthUser
        ? 'Email Added Successfully'
        : 'Email Changed Successfully';

      toast.success(
        successMessage,
        `Your email has been successfully ${isOAuthUser ? 'added' : 'updated'}`,
      );

      setTimeout(() => {
        onSuccess(newEmail);
        onClose();
        resetForm();
      }, 1500);
    } catch (error: unknown) {
      const errorMessage =
        (error as AxiosApiError)?.response?.data?.message ||
        (error as AxiosApiError)?.message ||
        'Verification failed';

      toast.error('Verification Failed', errorMessage);
    } finally {
      setIsVerifying(false);
    }
  };

  const handleResendCode = async () => {
    setIsLoading(true);

    try {
      await authApi.changeEmail({ newEmail: newEmail.trim() });

      toast.success('Code Resent', 'A new verification code has been sent to your email');
    } catch (error: unknown) {
      const errorMessage =
        (error as AxiosApiError)?.response?.data?.message ||
        (error as AxiosApiError)?.message ||
        'Failed to resend code';

      toast.error('Resend Failed', errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  const resetForm = () => {
    setNewEmail('');
    setVerificationCode('');
    setStep('input');
    setIsLoading(false);
    setIsVerifying(false);
    setIsVerified(false);
  };

  const handleClose = () => {
    if (!isLoading && !isVerifying) {
      resetForm();
      onClose();
    }
  };

  const handleCodeChange = (value: string) => {
    // Only allow numbers and max 6 digits
    const numericValue = value.replace(/\D/g, '').slice(0, 6);
    setVerificationCode(numericValue);
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
                    {isVerified
                      ? isOAuthUser
                        ? 'Email Added!'
                        : 'Email Updated!'
                      : step === 'input'
                        ? isOAuthUser
                          ? 'Add Email Address'
                          : 'Change Email'
                        : 'Verify Email Address'}
                  </h3>
                  <p className='text-sm text-gray-400'>
                    {isVerified
                      ? `Your email has been successfully ${isOAuthUser ? 'added' : 'updated'}`
                      : step === 'input'
                        ? isOAuthUser
                          ? 'Add an email address to your account'
                          : `Current: ${currentEmail}`
                        : `Code sent to ${newEmail}`}
                  </p>
                </div>
              </motion.div>
              {!isVerified && (
                <motion.button
                  whileHover={{ scale: 1.1, rotate: 90 }}
                  whileTap={{ scale: 0.9 }}
                  onClick={handleClose}
                  className='text-gray-400 hover:text-white transition-colors'
                >
                  <X className='w-5 h-5' />
                </motion.button>
              )}
            </div>

            {/* Content */}
            {!isVerified && (
              <div className='relative z-10'>
                {step === 'input' ? (
                  <motion.form
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    onSubmit={handleSubmitEmail}
                    className='space-y-6'
                  >
                    <Input
                      label={isOAuthUser ? 'Email Address' : 'New Email Address'}
                      type='email'
                      value={newEmail}
                      onChange={e => setNewEmail(e.target.value)}
                      placeholder={
                        isOAuthUser ? 'Enter your email address' : 'Enter new email address'
                      }
                      required
                      disabled={isLoading}
                    />

                    <Button
                      type='submit'
                      fullWidth
                      isLoading={isLoading}
                      disabled={!newEmail.trim()}
                      className='shadow-lg shadow-blue-500/30'
                    >
                      {isOAuthUser ? 'Add Email Address' : 'Send Verification Code'}
                    </Button>
                  </motion.form>
                ) : (
                  <motion.form
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    onSubmit={handleVerifyCode}
                    className='space-y-6'
                  >
                    {/* 6-digit code input */}
                    <div>
                      <Input
                        label='Verification Code'
                        type='text'
                        value={verificationCode}
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
                        onClick={handleResendCode}
                        disabled={isLoading}
                        className='text-sm text-blue-400 hover:text-blue-300 transition-colors disabled:opacity-50'
                      >
                        {isLoading ? (
                          <span className='flex items-center gap-2'>
                            <motion.div
                              animate={{ rotate: 360 }}
                              transition={{ duration: 1, repeat: Infinity, ease: 'linear' }}
                            >
                              <RefreshCw className='w-4 h-4' />
                            </motion.div>
                            Sending...
                          </span>
                        ) : (
                          "Didn't receive the code? Resend"
                        )}
                      </motion.button>
                    </div>

                    {/* Buttons */}
                    <div className='space-y-3'>
                      <Button
                        type='submit'
                        fullWidth
                        isLoading={isVerifying}
                        disabled={verificationCode.length !== 6}
                        className='shadow-lg shadow-blue-500/30'
                      >
                        {isOAuthUser ? 'Verify & Add Email' : 'Verify & Update Email'}
                      </Button>

                      <Button
                        type='button'
                        variant='secondary'
                        fullWidth
                        onClick={() => setStep('input')}
                        disabled={isVerifying}
                      >
                        Back
                      </Button>
                    </div>
                  </motion.form>
                )}
              </div>
            )}

            {/* Success state */}
            {isVerified && (
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
                <p className='text-gray-300'>
                  Your email has been successfully {isOAuthUser ? 'added:' : 'changed to:'}
                </p>
                <p className='font-mono text-sm bg-white/10 rounded-lg p-3 mt-2 border border-green-500/30'>
                  {newEmail}
                </p>
              </motion.div>
            )}
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
}
