'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/context/AuthContext';
import { EmailVerificationModal } from '@/components/auth/EmailVerificationModal';
import { motion } from 'framer-motion';
import { Mail, ArrowLeft } from 'lucide-react';
import { Button } from '@/components/ui/Button';

export default function VerifyEmailPage() {
  const { user, isEmailVerified, logout } = useAuth();
  const router = useRouter();
  const [showVerificationModal, setShowVerificationModal] = useState(false);
  const [pendingEmail, setPendingEmail] = useState<string | null>(null);

  useEffect(() => {
    // Проверяем есть ли email в sessionStorage (пользователь пытался войти с неверифицированным email)
    if (typeof window !== 'undefined') {
      const storedEmail = sessionStorage.getItem('pending_verification_email');
      if (storedEmail) {
        setPendingEmail(storedEmail);
        // Очищаем после получения
        sessionStorage.removeItem('pending_verification_email');
        return; // Показываем страницу верификации даже если пользователь не залогинен
      }
    }

    // Если пользователь не залогинен и нет pending email - редирект на логин
    if (!user && !pendingEmail) {
      router.push('/login');
      return;
    }

    // Если email уже верифицирован - редирект на дашборд
    if (isEmailVerified) {
      router.push('/dashboard');
      return;
    }

    // Если это OAuth пользователь без email - редирект на дашборд
    if (user && (!user.email || user.email.trim() === '')) {
      router.push('/dashboard');
      return;
    }
  }, [user, isEmailVerified, pendingEmail, router]);

  const handleVerificationSuccess = () => {
    router.push('/dashboard');
  };

  const handleLogout = async () => {
    await logout();
    router.push('/login');
  };

  // Определяем email для отображения
  const displayEmail = pendingEmail || user?.email;

  if ((!user && !pendingEmail) || isEmailVerified || !displayEmail) {
    return null;
  }

  return (
    <main className='min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-violet-900 flex items-center justify-center p-4'>
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className='w-full max-w-md'
      >
        <div className='bg-black/50 backdrop-blur-xl border border-white/10 rounded-2xl p-8'>
          {/* Header */}
          <div className='text-center mb-8'>
            <div className='w-16 h-16 bg-yellow-500/20 rounded-full flex items-center justify-center mx-auto mb-4'>
              <Mail className='w-8 h-8 text-yellow-400' />
            </div>
            <h1 className='text-3xl font-bold text-white mb-2'>Verify Your Email</h1>
            <p className='text-gray-300'>
              We sent a verification code to
            </p>
            <p className='text-blue-400 font-mono text-sm mt-1'>{displayEmail}</p>
          </div>

          {/* Message */}
          <div className='bg-yellow-500/10 border border-yellow-500/20 rounded-lg p-4 mb-6'>
            <p className='text-sm text-yellow-400'>
              <strong>Access Restricted:</strong> You need to verify your email address to access the dashboard and other features.
            </p>
          </div>

          {/* Actions */}
          <div className='space-y-3'>
            <Button
              fullWidth
              onClick={() => setShowVerificationModal(true)}
              className='bg-yellow-500 hover:bg-yellow-600 text-black'
            >
              <Mail className='w-4 h-4 mr-2' />
              Enter Verification Code
            </Button>

            <Button
              fullWidth
              variant='ghost'
              onClick={handleLogout}
              className='text-gray-400 hover:text-white'
            >
              <ArrowLeft className='w-4 h-4 mr-2' />
              Back to Login
            </Button>
          </div>

          {/* Help text */}
          <div className='mt-6 text-center'>
            <p className='text-xs text-gray-400'>
              Didn't receive the code? Check your spam folder or click "Resend Code" in the verification modal.
            </p>
          </div>
        </div>
      </motion.div>

      {/* Email Verification Modal */}
      <EmailVerificationModal
        isOpen={showVerificationModal}
        onClose={() => setShowVerificationModal(false)}
        email={displayEmail}
        onSuccess={handleVerificationSuccess}
      />
    </main>
  );
}
