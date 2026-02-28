'use client';

import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { AlertTriangle, X, Lock, Mail, Clock, RefreshCw, Trash2, CheckCircle } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { authApi } from '@/lib/authApi';
import { useToast } from '@/hooks/useToast';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/context/AuthContext';

type DeleteAccountStep = 'initial' | 'password' | 'code' | 'processing' | 'success';

interface DeleteAccountModalProps {
  isOpen: boolean;
  onClose: () => void;
  email: string;
}

export function DeleteAccountModal({ isOpen, onClose, email }: DeleteAccountModalProps) {
  const router = useRouter();
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

    if (!password) {
      toast.error('Password Required', 'Please enter your password to continue');
      return;
    }

    setIsLoading(true);

    try {
      await authApi.requestAccountDeletion({ password });
      setStep('code');
      setTimeRemaining(24 * 60 * 60); // Reset to 24 hours
      setCodeResendTimer(60); // 60 second cooldown for resend

      toast.warning(
        'Deletion Code Sent',
        `A confirmation code has been sent to ${email}. Check your inbox and spam folder.`,
      );
    } catch (error: any) {
      const errorMessage =
        error?.response?.data?.message || error?.message || 'Failed to send deletion code';

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
    } catch (error: any) {
      console.log('[DeleteAccountModal] Deletion error:', error);
      console.log('[DeleteAccountModal] Error status:', error?.response?.status);
      console.log('[DeleteAccountModal] Error data:', error?.response?.data);

      // Check if this is expected error after successful deletion
      // 401/403 = Account deleted, token is now invalid
      // 500 = Server error but account might be deleted
      const isAuthError = error?.response?.status === 401 || error?.response?.status === 403;
      const isServerError = error?.response?.status === 500;
      const errorMessage =
        error?.response?.data?.message || error?.message || 'Failed to delete account';

      // For 500 errors or auth errors, always verify if account was actually deleted
      if (isAuthError || isServerError) {
        console.log('[DeleteAccountModal] Checking if account was actually deleted...');

        // Verify account is actually deleted by checking session
        setTimeout(async () => {
          try {
            const response = await fetch('/api/auth/session', { credentials: 'include' });
            console.log('[DeleteAccountModal] Session check response:', response.status);

            if (response.ok) {
              const data = await response.json();
              console.log('[DeleteAccountModal] Session data:', data);

              if (data.user) {
                console.log('[DeleteAccountModal] Account still exists - deletion failed');
                setStep('code');
                setIsLoading(false);
                toast.error('Deletion Failed', 'Account deletion failed. Please try again.');
                return;
              }
            }

            // If we get here, account is deleted (no user in session or 401)
            console.log('[DeleteAccountModal] Account successfully deleted');
          } catch (sessionError) {
            console.log(
              '[DeleteAccountModal] Session check failed (expected after deletion):',
              sessionError,
            );
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
        console.log('[DeleteAccountModal] Real deletion error:', errorMessage);

        toast.error('Deletion Failed', errorMessage);
        setStep('code');
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleResendCode = async () => {
    if (codeResendTimer > 0) return;

    setIsLoading(true);

    try {
      await authApi.requestAccountDeletion({ password });
      setTimeRemaining(24 * 60 * 60); // Reset timer
      setCodeResendTimer(60); // Reset resend timer

      toast.warning('Code Resent', 'A new confirmation code has been sent to your email');
    } catch (error: any) {
      const errorMessage =
        error?.response?.data?.message || error?.message || 'Failed to resend code';
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
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          exit={{ opacity: 0, scale: 0.95 }}
          className='w-full max-w-md mx-4'
        >
          <div className='bg-black/90 backdrop-blur-xl border border-red-500/20 rounded-2xl p-6'>
            {/* Header */}
            <div className='flex items-center justify-between mb-6'>
              <div className='flex items-center gap-3'>
                <div
                  className={`w-12 h-12 rounded-full flex items-center justify-center ${
                    step === 'success' ? 'bg-green-500/20' : 'bg-red-500/20'
                  }`}
                >
                  {step === 'success' ? (
                    <CheckCircle className='w-6 h-6 text-green-400' />
                  ) : (
                    <AlertTriangle className='w-6 h-6 text-red-400' />
                  )}
                </div>
                <div>
                  <h3 className='text-xl font-bold text-white'>
                    {step === 'initial' && 'Delete Account'}
                    {step === 'password' && 'Confirm Password'}
                    {step === 'code' && 'Enter Confirmation Code'}
                    {step === 'processing' && 'Deleting Account...'}
                    {step === 'success' && 'Account Deleted'}
                  </h3>
                </div>
              </div>
              {step !== 'processing' && step !== 'success' && (
                <button
                  onClick={handleClose}
                  className='text-gray-400 hover:text-white transition-colors'
                >
                  <X className='w-5 h-5' />
                </button>
              )}
            </div>

            {/* Content */}
            {step === 'initial' && (
              <div className='space-y-6'>
                <div className='bg-red-500/10 border border-red-500/20 rounded-lg p-4'>
                  <div className='flex items-start gap-3'>
                    <AlertTriangle className='w-5 h-5 text-red-400 flex-shrink-0 mt-0.5' />
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
                </div>

                <div className='flex gap-3'>
                  <Button variant='secondary' onClick={handleClose} className='flex-1'>
                    Cancel
                  </Button>
                  <Button
                    onClick={() => setStep('password')}
                    className='flex-1 bg-red-600 hover:bg-red-700'
                  >
                    <Trash2 className='w-4 h-4 mr-2' />
                    Delete Account
                  </Button>
                </div>
              </div>
            )}

            {step === 'password' && (
              <form onSubmit={handleRequestDeletion} className='space-y-6'>
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
                    className='flex-1 bg-red-600 hover:bg-red-700'
                  >
                    Send Deletion Code
                  </Button>
                </div>
              </form>
            )}

            {step === 'code' && (
              <form onSubmit={handleConfirmDeletion} className='space-y-6'>
                <div className='text-center space-y-3'>
                  <Mail className='w-12 h-12 text-blue-400 mx-auto' />
                  <p className='text-sm text-gray-300'>
                    A 6-digit confirmation code has been sent to:
                  </p>
                  <p className='font-mono text-sm bg-white/10 rounded-lg p-2'>{email}</p>
                  <div className='flex items-center justify-center gap-2 text-xs text-yellow-400'>
                    <Clock className='w-3 h-3' />
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
                  <button
                    type='button'
                    onClick={handleResendCode}
                    disabled={isLoading || codeResendTimer > 0}
                    className='text-sm text-blue-400 hover:text-blue-300 transition-colors disabled:opacity-50'
                  >
                    {codeResendTimer > 0
                      ? `Resend code in ${codeResendTimer}s`
                      : "Didn't receive the code? Resend"}
                  </button>
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
                    className='flex-1 bg-red-600 hover:bg-red-700'
                  >
                    Delete Account
                  </Button>
                </div>
              </form>
            )}

            {step === 'processing' && (
              <div className='text-center py-8 space-y-4'>
                <div className='w-16 h-16 border-4 border-red-500 border-t-transparent rounded-full animate-spin mx-auto' />
                <p className='text-lg font-semibold'>Deleting your account...</p>
                <p className='text-sm text-gray-400'>
                  Please wait while we permanently delete your data
                </p>
              </div>
            )}

            {step === 'success' && (
              <div className='text-center py-8 space-y-4'>
                <CheckCircle className='w-16 h-16 text-green-400 mx-auto' />
                <p className='text-lg font-semibold'>Account Successfully Deleted</p>
                <p className='text-sm text-gray-400'>All your data has been permanently removed.</p>
                <p className='text-xs text-gray-500'>Redirecting to login page...</p>
              </div>
            )}
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
}
