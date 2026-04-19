'use client';

import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Mail, AlertTriangle, X, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { authApi } from '@/lib/authApi';
import { useToast } from '@/hooks/useToast';
import { EmailVerificationModal } from './EmailVerificationModal';
import { EmailChangeModal } from './EmailChangeModal';
import { AxiosApiError } from '@/types';

interface EmailVerificationBannerProps {
  email: string;
  onVerified: () => void;
}

export function EmailVerificationBanner({ email, onVerified }: EmailVerificationBannerProps) {
  const toast = useToast();
  const [isDismissed, setIsDismissed] = useState(false);
  const [showVerificationModal, setShowVerificationModal] = useState(false);
  const [showChangeModal, setShowChangeModal] = useState(false);
  const [isResending, setIsResending] = useState(false);

  // Check if this is an OAuth user without email
  const isOAuthUser = !email || email.trim() === '';

  const handleResend = async () => {
    if (isOAuthUser) {
      // OAuth users can't resend - they need to add email first
      setShowChangeModal(true);
      return;
    }

    setIsResending(true);

    try {
      await authApi.resendVerification({ email });
      toast.success('Code Resent', 'A new verification code has been sent to your email');
    } catch (error: unknown) {
      const errorMessage =
        (error as AxiosApiError)?.response?.data?.message ||
        (error as AxiosApiError)?.message ||
        'Failed to resend code';

      toast.error('Resend Failed', errorMessage);
    } finally {
      setIsResending(false);
    }
  };

  const handleVerificationSuccess = () => {
    onVerified();
    setIsDismissed(true);
  };

  const handleEmailChangeSuccess = (_newEmail: string) => {
    onVerified();
    setIsDismissed(true);
  };

  if (isDismissed) return null;

  return (
    <>
      <AnimatePresence>
        <motion.div
          initial={{ opacity: 0, y: -20, scale: 0.95 }}
          animate={{ opacity: 1, y: 0, scale: 1 }}
          exit={{ opacity: 0, y: -20, scale: 0.95 }}
          transition={{ type: 'spring', stiffness: 300, damping: 30 }}
          whileHover={{ scale: 1.01 }}
          className='bg-yellow-500/10 border border-yellow-500/20 rounded-xl p-4 mb-6 relative overflow-hidden'
        >
          <motion.div
            className='absolute inset-0 bg-gradient-to-r from-yellow-500/5 to-orange-500/5'
            animate={{ opacity: [0, 1, 0] }}
            transition={{ duration: 3, repeat: Infinity, ease: 'easeInOut' }}
          />
          <div className='flex items-start gap-3 relative z-10'>
            <motion.div
              animate={{ rotate: [0, 10, -10, 0] }}
              transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
              className='flex-shrink-0'
            >
              <AlertTriangle className='w-5 h-5 text-yellow-400 mt-0.5' />
            </motion.div>

            <div className='flex-1 min-w-0'>
              <h3 className='text-sm font-semibold text-yellow-400 mb-1'>
                {isOAuthUser ? 'Add Email Address' : 'Verify Your Email Address'}
              </h3>
              <p className='text-sm text-gray-300 mb-3'>
                {isOAuthUser
                  ? 'Add an email address to access all features and receive notifications.'
                  : `Please verify <span class="font-mono">${email}</span> to access all features. Check your inbox for the verification code.`}
              </p>

              <div className='flex flex-wrap gap-2'>
                {isOAuthUser ? (
                  <Button
                    size='sm'
                    onClick={() => setShowChangeModal(true)}
                    className='bg-gradient-to-r from-yellow-500 to-orange-500 hover:from-yellow-600 hover:to-orange-600 text-black shadow-lg shadow-yellow-500/30'
                  >
                    <Mail className='w-4 h-4 mr-2' />
                    Add Email
                  </Button>
                ) : (
                  <>
                    <Button
                      size='sm'
                      onClick={() => setShowVerificationModal(true)}
                      className='bg-gradient-to-r from-yellow-500 to-orange-500 hover:from-yellow-600 hover:to-orange-600 text-black shadow-lg shadow-yellow-500/30'
                    >
                      <Mail className='w-4 h-4 mr-2' />
                      Verify Email
                    </Button>

                    <Button
                      variant='ghost'
                      size='sm'
                      onClick={handleResend}
                      disabled={isResending}
                      className='text-yellow-400 hover:text-yellow-300 hover:bg-yellow-500/10'
                    >
                      {isResending ? (
                        <>
                          <motion.div
                            animate={{ rotate: 360 }}
                            transition={{ duration: 1, repeat: Infinity, ease: 'linear' }}
                          >
                            <RefreshCw className='w-4 h-4 mr-2' />
                          </motion.div>
                          Sending...
                        </>
                      ) : (
                        <>
                          <RefreshCw className='w-4 h-4 mr-2' />
                          Resend Code
                        </>
                      )}
                    </Button>
                  </>
                )}
              </div>
            </div>

            <motion.button
              whileHover={{ scale: 1.1, rotate: 90 }}
              whileTap={{ scale: 0.9 }}
              onClick={() => setIsDismissed(true)}
              className='flex-shrink-0 text-gray-400 hover:text-white transition-colors'
            >
              <X className='w-4 h-4' />
            </motion.button>
          </div>
        </motion.div>
      </AnimatePresence>

      {/* Email Verification Modal */}
      <EmailVerificationModal
        isOpen={showVerificationModal}
        onClose={() => setShowVerificationModal(false)}
        email={email}
        onSuccess={handleVerificationSuccess}
      />

      {/* Email Change Modal for OAuth users */}
      <EmailChangeModal
        isOpen={showChangeModal}
        onClose={() => setShowChangeModal(false)}
        currentEmail={email}
        onSuccess={handleEmailChangeSuccess}
      />
    </>
  );
}
