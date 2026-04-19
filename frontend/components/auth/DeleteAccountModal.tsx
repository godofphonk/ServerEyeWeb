'use client';

import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { AlertTriangle, X, Mail, Clock, Trash2, CheckCircle } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { authApi } from '@/lib/authApi';
import { useToast } from '@/hooks/useToast';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/context/AuthContext';
import { AxiosApiError } from '@/types';

type DeleteAccountStep =
  | 'initial'
  | 'password'
  | 'code'
  | 'processing'
  | 'success'
  | 'direct-confirm';

interface DeleteAccountModalProps {
  isOpen: boolean;
  onClose: () => void;
  email: string | null; // Может быть null для OAuth пользователей без email
  hasPassword: boolean; // Указывает есть ли у пользователя пароль (не OAuth-only)
}

export function DeleteAccountModal({
  isOpen,
  onClose,
  email,
  hasPassword,
}: DeleteAccountModalProps) {
  const _router = useRouter();
  const toast = useToast();
  const { clearAuthData } = useAuth();
  const [step, setStep] = useState<DeleteAccountStep>('initial');
  const [password, setPassword] = useState('');
  const [confirmationCode, setConfirmationCode] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [timeRemaining, setTimeRemaining] = useState(24 * 60 * 60); // 24 hours in seconds
  const [codeResendTimer, setCodeResendTimer] = useState(0);

  // Countdown timer for 24-hour expiry
  useEffect(() => {
    if (step === 'code' && timeRemaining > 0) {
      const timer = setInterval(() => {
        setTimeRemaining(prev => prev - 1);
      }, 1000);
      return () => clearInterval(timer);
    }
  }, [step, timeRemaining]);

  // Resend code timer (60 seconds)
  useEffect(() => {
    if (codeResendTimer > 0) {
      const timer = setInterval(() => {
        setCodeResendTimer(prev => prev - 1);
      }, 1000);
      return () => clearInterval(timer);
    }
  }, [codeResendTimer]);

  const formatTime = (seconds: number) => {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;
    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  const handleRequestDeletion = async (e: React.FormEvent) => {
    e.preventDefault();

    if (hasPassword && !password) {
      toast.error('Password Required', 'Please enter your password to continue');
      return;
    }

    setIsLoading(true);

    try {
      await authApi.requestAccountDeletion({ password: hasPassword ? password : null });
      setStep('code');
      setTimeRemaining(24 * 60 * 60); // Reset to 24 hours
      setCodeResendTimer(60); // 60 second cooldown for resend

      toast.warning(
        'Deletion Code Sent',
        `A confirmation code has been sent to ${email || 'your email'}. Check your inbox and spam folder.`,
      );
    } catch (error: unknown) {
      const errorMessage =
        (error as AxiosApiError)?.response?.data?.message ||
        (error as AxiosApiError)?.message ||
        'Failed to send deletion code';

      if (errorMessage.includes('password')) {
        toast.error('Invalid Password', 'The password you entered is incorrect');
      } else {
        toast.error('Request Failed', errorMessage);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleConfirmDeletion = async (e: React.FormEvent) => {
    e.preventDefault();

    if (confirmationCode.length !== 6) {
      toast.error('Invalid Code', 'Please enter a 6-digit confirmation code');
      return;
    }

    setIsLoading(true);
    setStep('processing');

    try {
      await authApi.confirmAccountDeletion({ confirmationCode });

      // Clear all authentication data using AuthContext
      clearAuthData();

      setStep('success');

      toast.error(
        'Account Deleted',
        'Your account has been permanently deleted. You will be redirected to the login page.',
        10000, // 10 seconds
      );

      // Force redirect to login after 2 seconds
      setTimeout(() => {
        window.location.href = '/login';
      }, 2000);
    } catch (error: unknown) {
      // Check if this is expected error after successful deletion
      // 401/403 = Account deleted, token is now invalid
      // 500 = Server error but account might be deleted
      const isAuthError =
        (error as AxiosApiError)?.response?.status === 401 ||
        (error as AxiosApiError)?.response?.status === 403;
      const isServerError = (error as AxiosApiError)?.response?.status === 500;
      const errorMessage =
        (error as AxiosApiError)?.response?.data?.message ||
        (error as AxiosApiError)?.message ||
        'Failed to delete account';

      // For 500 errors or auth errors, always verify if account was actually deleted
      if (isAuthError || isServerError) {
        // Verify account is actually deleted by checking session
        setTimeout(async () => {
          try {
            const response = await fetch('/api/auth/session', { credentials: 'include' });

            if (response.ok) {
              const data = await response.json();

              if (data.user) {
                setStep('code');
                setIsLoading(false);
                toast.error('Deletion Failed', 'Account deletion failed. Please try again.');
                return;
              }
            }

            // If we get here, account is deleted (no user in session or 401)
          } catch (_sessionError) {
            /* ignore error */
          }

          // Clear all authentication data using AuthContext
          clearAuthData();

          setStep('success');
          setIsLoading(false);

          toast.success(
            'Account Deleted',
            'Your account has been permanently deleted. You will be redirected to the login page.',
          );

          // Force redirect to login after 2 seconds
          setTimeout(() => {
            window.location.href = '/login';
          }, 2000);
        }, 1000); // Wait 1 second before checking

        return; // Exit early to avoid the else block
      } else {
        // Real error - show to user

        toast.error('Deletion Failed', errorMessage);
        setStep('code');
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleDirectDeletion = async () => {
    setIsLoading(true);
    setStep('processing');

    try {
      await authApi.deleteAccountDirect();

      // Clear all authentication data
      clearAuthData();

      setStep('success');

      toast.success(
        'Account Deleted',
        'Your account has been permanently deleted. You will be redirected to the login page.',
      );

      // Force redirect to login after 2 seconds
      setTimeout(() => {
        window.location.href = '/login';
      }, 2000);
    } catch (error: unknown) {
      const errorMessage =
        (error as AxiosApiError)?.response?.data?.message ||
        (error as AxiosApiError)?.message ||
        'Failed to delete account';
      toast.error('Deletion Failed', errorMessage);
      setStep('direct-confirm');
    } finally {
      setIsLoading(false);
    }
  };

  const handleResendCode = async () => {
    if (codeResendTimer > 0) return;

    setIsLoading(true);

    try {
      await authApi.requestAccountDeletion({ password: hasPassword ? password : null });
      setTimeRemaining(24 * 60 * 60); // Reset timer
      setCodeResendTimer(60); // Reset resend timer

      toast.warning('Code Resent', 'A new confirmation code has been sent to your email');
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

  const handleCodeChange = (value: string) => {
    // Only allow numbers and max 6 digits
    const numericValue = value.replace(/\D/g, '').slice(0, 6);
    setConfirmationCode(numericValue);
  };

  const handleClose = () => {
    if (!isLoading && step !== 'processing' && step !== 'success') {
      setStep('initial');
      setPassword('');
      setConfirmationCode('');
      setTimeRemaining(24 * 60 * 60);
      onClose();
    }
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
          <div className='bg-black/90 backdrop-blur-xl border border-red-500/20 rounded-2xl p-6 relative overflow-hidden shadow-2xl shadow-red-500/20'>
            <motion.div
              className='absolute inset-0 bg-gradient-to-br from-red-500/5 to-orange-500/5'
              animate={{ opacity: [0, 1, 0] }}
              transition={{ duration: 3, repeat: Infinity, ease: 'easeInOut' }}
            />
            {/* Header */}
            <div className='flex items-center justify-between mb-6 relative z-10'>
              <motion.div whileHover={{ scale: 1.02 }} className='flex items-center gap-3'>
                <motion.div
                  animate={
                    step === 'success' ? { scale: [1, 1.1, 1] } : { rotate: [0, 10, -10, 0] }
                  }
                  transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
                  className={`w-12 h-12 rounded-full flex items-center justify-center ${
                    step === 'success' ? 'bg-green-500/20' : 'bg-red-500/20'
                  } border ${step === 'success' ? 'border-green-500/30' : 'border-red-500/30'}`}
                >
                  {step === 'success' ? (
                    <CheckCircle className='w-6 h-6 text-green-400' />
                  ) : (
                    <AlertTriangle className='w-6 h-6 text-red-400' />
                  )}
                </motion.div>
                <div>
                  <h3 className='text-xl font-bold text-white'>
                    {step === 'initial' && 'Delete Account'}
                    {step === 'password' && 'Confirm Password'}
                    {step === 'code' && 'Enter Confirmation Code'}
                    {step === 'processing' && 'Deleting Account...'}
                    {step === 'success' && 'Account Deleted'}
                  </h3>
                </div>
              </motion.div>
              {step !== 'processing' && step !== 'success' && (
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
            {step === 'initial' && (
              <motion.div
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                className='space-y-6 relative z-10'
              >
                <motion.div
                  whileHover={{ scale: 1.02 }}
                  className='bg-red-500/10 border border-red-500/20 rounded-xl p-4'
                >
                  <div className='flex items-start gap-3'>
                    <motion.div
                      animate={{ rotate: [0, 10, -10, 0] }}
                      transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
                    >
                      <AlertTriangle className='w-5 h-5 text-red-400 flex-shrink-0 mt-0.5' />
                    </motion.div>
                    <div className='space-y-2'>
                      <p className='text-sm font-semibold text-red-400'>
                        ⚠️ WARNING: This action cannot be undone
                      </p>
                      <p className='text-sm text-gray-300'>
                        Deleting your account will permanently remove:
                      </p>
                      <ul className='text-xs text-gray-400 space-y-1 ml-4'>
                        <li>• All server configurations</li>
                        <li>• Historical metrics data</li>
                        <li>• Alert settings and notifications</li>
                        <li>• All personal information</li>
                      </ul>
                    </div>
                  </div>
                </motion.div>

                <div className='flex gap-3'>
                  <Button variant='secondary' onClick={handleClose} className='flex-1'>
                    Cancel
                  </Button>
                  <Button
                    onClick={() => (email ? setStep('password') : setStep('direct-confirm'))}
                    className='flex-1 bg-gradient-to-r from-red-600 to-red-700 hover:from-red-700 hover:to-red-800 shadow-lg shadow-red-500/30'
                  >
                    <Trash2 className='w-4 h-4 mr-2' />
                    {email ? 'Delete Account' : 'Delete Account (Immediate)'}
                  </Button>
                </div>
              </motion.div>
            )}

            {step === 'direct-confirm' && (
              <motion.div
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                className='space-y-6 relative z-10'
              >
                <motion.div
                  whileHover={{ scale: 1.02 }}
                  className='bg-yellow-500/10 border border-yellow-500/20 rounded-xl p-4'
                >
                  <div className='flex items-start gap-3'>
                    <motion.div
                      animate={{ rotate: [0, 10, -10, 0] }}
                      transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
                    >
                      <AlertTriangle className='w-5 h-5 text-yellow-400 flex-shrink-0 mt-0.5' />
                    </motion.div>
                    <div className='space-y-2'>
                      <p className='text-sm font-semibold text-yellow-400'>
                        ⚠️ No Email Confirmation Available
                      </p>
                      <p className='text-sm text-gray-300'>
                        Since you don't have an email associated with this account, deletion will be
                        immediate and cannot be reversed.
                      </p>
                      <p className='text-sm text-gray-400'>
                        Are you absolutely sure you want to delete your account?
                      </p>
                    </div>
                  </div>
                </motion.div>

                <div className='flex gap-3'>
                  <Button
                    variant='secondary'
                    onClick={() => setStep('initial')}
                    disabled={isLoading}
                    className='flex-1'
                  >
                    Cancel
                  </Button>
                  <Button
                    onClick={handleDirectDeletion}
                    isLoading={isLoading}
                    className='flex-1 bg-gradient-to-r from-red-600 to-red-700 hover:from-red-700 hover:to-red-800 shadow-lg shadow-red-500/30'
                  >
                    Yes, Delete Now
                  </Button>
                </div>
              </motion.div>
            )}

            {step === 'password' && (
              <motion.form
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                onSubmit={handleRequestDeletion}
                className='space-y-6 relative z-10'
              >
                <div>
                  <Input
                    label='Enter your password'
                    type='password'
                    value={password}
                    onChange={e => setPassword(e.target.value)}
                    placeholder='Enter your password to confirm'
                    required
                    disabled={isLoading}
                  />
                  <p className='text-xs text-gray-400 mt-2'>
                    This is required to prevent unauthorized account deletion
                  </p>
                </div>

                <div className='flex gap-3'>
                  <Button
                    type='button'
                    variant='secondary'
                    onClick={() => setStep('initial')}
                    disabled={isLoading}
                    className='flex-1'
                  >
                    Back
                  </Button>
                  <Button
                    type='submit'
                    isLoading={isLoading}
                    className='flex-1 bg-gradient-to-r from-red-600 to-red-700 hover:from-red-700 hover:to-red-800 shadow-lg shadow-red-500/30'
                  >
                    Send Deletion Code
                  </Button>
                </div>
              </motion.form>
            )}

            {step === 'code' && (
              <motion.form
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                onSubmit={handleConfirmDeletion}
                className='space-y-6 relative z-10'
              >
                <div className='text-center space-y-3'>
                  <motion.div
                    animate={{ scale: [1, 1.1, 1] }}
                    transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
                  >
                    <Mail className='w-12 h-12 text-blue-400 mx-auto' />
                  </motion.div>
                  <p className='text-sm text-gray-300'>
                    A 6-digit confirmation code has been sent to:
                  </p>
                  <p className='font-mono text-sm bg-white/10 rounded-lg p-2 border border-blue-500/30'>
                    {email || 'your email'}
                  </p>
                  <div className='flex items-center justify-center gap-2 text-xs text-yellow-400'>
                    <motion.div
                      animate={{ rotate: 360 }}
                      transition={{ duration: 1, repeat: Infinity, ease: 'linear' }}
                    >
                      <Clock className='w-3 h-3' />
                    </motion.div>
                    <span>Expires in {formatTime(timeRemaining)}</span>
                  </div>
                </div>

                <div>
                  <Input
                    label='Confirmation Code'
                    type='text'
                    value={confirmationCode}
                    onChange={e => handleCodeChange(e.target.value)}
                    placeholder='Enter 6-digit code'
                    maxLength={6}
                    className='text-center text-2xl tracking-widest'
                    required
                    disabled={isLoading}
                  />
                  <p className='text-xs text-gray-400 mt-2 text-center'>
                    Check your email (including spam folder)
                  </p>
                </div>

                <div className='text-center'>
                  <motion.button
                    whileHover={{ scale: 1.05 }}
                    whileTap={{ scale: 0.95 }}
                    type='button'
                    onClick={handleResendCode}
                    disabled={isLoading || codeResendTimer > 0}
                    className='text-sm text-blue-400 hover:text-blue-300 transition-colors disabled:opacity-50'
                  >
                    {codeResendTimer > 0
                      ? `Resend code in ${codeResendTimer}s`
                      : "Didn't receive the code? Resend"}
                  </motion.button>
                </div>

                <div className='flex gap-3'>
                  <Button
                    type='button'
                    variant='secondary'
                    onClick={() => setStep('password')}
                    disabled={isLoading}
                    className='flex-1'
                  >
                    Back
                  </Button>
                  <Button
                    type='submit'
                    isLoading={isLoading}
                    disabled={confirmationCode.length !== 6}
                    className='flex-1 bg-gradient-to-r from-red-600 to-red-700 hover:from-red-700 hover:to-red-800 shadow-lg shadow-red-500/30'
                  >
                    Delete Account
                  </Button>
                </div>
              </motion.form>
            )}

            {step === 'processing' && (
              <motion.div
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                className='text-center py-8 space-y-4 relative z-10'
              >
                <motion.div
                  animate={{ rotate: 360 }}
                  transition={{ duration: 1, repeat: Infinity, ease: 'linear' }}
                  className='w-16 h-16 border-4 border-red-500 border-t-transparent rounded-full mx-auto shadow-lg shadow-red-500/30'
                />
                <p className='text-lg font-semibold'>Deleting your account...</p>
                <p className='text-sm text-gray-400'>
                  Please wait while we permanently delete your data
                </p>
              </motion.div>
            )}

            {step === 'success' && (
              <motion.div
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                className='text-center py-8 space-y-4 relative z-10'
              >
                <motion.div
                  animate={{ scale: [1, 1.2, 1] }}
                  transition={{ duration: 1, repeat: Infinity, ease: 'easeInOut' }}
                  className='w-16 h-16 bg-green-500/20 rounded-full flex items-center justify-center mx-auto border border-green-500/30 shadow-lg shadow-green-500/30'
                >
                  <CheckCircle className='w-10 h-10 text-green-400' />
                </motion.div>
                <p className='text-lg font-semibold'>Account Successfully Deleted</p>
                <p className='text-sm text-gray-400'>All your data has been permanently removed.</p>
                <p className='text-xs text-gray-500'>Redirecting to login page...</p>
              </motion.div>
            )}
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
}
