'use client';

import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Mail, AlertTriangle, X, CheckCircle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { authApi } from '@/lib/authApi';
import { useToast } from '@/hooks/useToast';
import { EmailVerificationModal } from './EmailVerificationModal';

interface EmailVerificationBannerProps {
  email: string;
  onVerified: () => void;
}

export function EmailVerificationBanner({ email, onVerified }: EmailVerificationBannerProps) {
  const toast = useToast();
  const [isDismissed, setIsDismissed] = useState(false);
  const [showVerificationModal, setShowVerificationModal] = useState(false);
  const [isResending, setIsResending] = useState(false);

  const handleResend = async () => {
    setIsResending(true);
    
    try {
      await authApi.resendVerification({ email });
      toast.success(
        'Code Resent',
        'A new verification code has been sent to your email'
      );
    } catch (error: any) {
      const errorMessage = error?.response?.data?.message || error?.message || 'Failed to resend code';
      
      toast.error(
        'Resend Failed',
        errorMessage
      );
    } finally {
      setIsResending(false);
    }
  };

  const handleVerificationSuccess = () => {
    onVerified();
    setIsDismissed(true);
  };

  if (isDismissed) return null;

  return (
    <>
      <AnimatePresence>
        <motion.div
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -20 }}
          className="bg-yellow-500/10 border border-yellow-500/20 rounded-lg p-4 mb-6"
        >
          <div className="flex items-start gap-3">
            <div className="flex-shrink-0">
              <AlertTriangle className="w-5 h-5 text-yellow-400 mt-0.5" />
            </div>
            
            <div className="flex-1 min-w-0">
              <h3 className="text-sm font-semibold text-yellow-400 mb-1">
                Verify Your Email Address
              </h3>
              <p className="text-sm text-gray-300 mb-3">
                Please verify <span className="font-mono">{email}</span> to access all features. 
                Check your inbox for the verification code.
              </p>
              
              <div className="flex flex-wrap gap-2">
                <Button
                  size="sm"
                  onClick={() => setShowVerificationModal(true)}
                  className="bg-yellow-500 hover:bg-yellow-600 text-black"
                >
                  <Mail className="w-4 h-4 mr-2" />
                  Verify Email
                </Button>
                
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={handleResend}
                  disabled={isResending}
                  className="text-yellow-400 hover:text-yellow-300"
                >
                  {isResending ? (
                    <>
                      <RefreshCw className="w-4 h-4 mr-2 animate-spin" />
                      Sending...
                    </>
                  ) : (
                    <>
                      <RefreshCw className="w-4 h-4 mr-2" />
                      Resend Code
                    </>
                  )}
                </Button>
              </div>
            </div>
            
            <button
              onClick={() => setIsDismissed(true)}
              className="flex-shrink-0 text-gray-400 hover:text-white transition-colors"
            >
              <X className="w-4 h-4" />
            </button>
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
    </>
  );
}
