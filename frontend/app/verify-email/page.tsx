'use client';

import { useEffect, useState, useRef } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/context/AuthContext';
import { EmailVerificationModal } from '@/components/auth/EmailVerificationModal';
import { motion } from 'framer-motion';
import { Mail, ArrowLeft, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { isOAuthUser } from '@/lib/authUtils';
import { authApi } from '@/lib/authApi';
import { useToast } from '@/hooks/useToast';

export default function VerifyEmailPage() {
  const { user, isEmailVerified, logout, loading, login } = useAuth();
  const router = useRouter();
  const toast = useToast();
  const [showVerificationModal, setShowVerificationModal] = useState(false);
  const [pendingEmail, setPendingEmail] = useState<string | null>(null);
  const [isChecking, setIsChecking] = useState(true);
  const [isSendingCode, setIsSendingCode] = useState(false);
  const [resendCooldown, setResendCooldown] = useState(0);
  const redirectAttempted = useRef(false);
  const resendTimerRef = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    // Ждем загрузки auth данных
    if (loading) {
      return;
    }

    setIsChecking(false);

    // OAuth пользователи не должны попадать на страницу верификации
    if (user && isOAuthUser(user)) {
      router.push('/dashboard');
      return;
    }

    // Если email уже верифицирован - редирект на дашборд
    if (isEmailVerified) {
      router.push('/dashboard');
      return;
    }

    // Проверяем есть ли email в sessionStorage (пользователь пытался войти с неверифицированным email)
    if (typeof window !== 'undefined' && !pendingEmail) {
      const storedEmail = sessionStorage.getItem('pending_verification_email');

      if (storedEmail) {
        setPendingEmail(storedEmail);
        // НЕ очищаем sessionStorage сразу - пусть будет для следующих рендеров
        return; // Показываем страницу верификации даже если пользователь не залогинен
      }
    }

    // НЕ очищаем sessionStorage - пусть остается для сохранения email при обновлении страницы
    // НЕ редиректим на login автоматически - пользователь может остаться на странице для ввода кода
  }, [user, isEmailVerified, pendingEmail, router, loading]);

  // Таймер cooldown для ресенда
  useEffect(() => {
    if (resendCooldown > 0) {
      resendTimerRef.current = setTimeout(() => {
        setResendCooldown(prev => prev - 1);
      }, 1000);
    }

    return () => {
      if (resendTimerRef.current) {
        clearTimeout(resendTimerRef.current);
      }
    };
  }, [resendCooldown]);

  const handleSendCode = async () => {
    if (!displayEmail) return;

    setIsSendingCode(true);

    try {
      await authApi.resendVerificationWithoutAuth({ email: displayEmail });
      toast.success('Code Sent', 'A verification code has been sent to your email');
      setResendCooldown(60); // 1 минута cooldown
      setShowVerificationModal(true);
    } catch (error: unknown) {
      const errorMessage =
        (error as any)?.response?.data?.message || (error as any)?.message || 'Failed to send code';
      toast.error('Send Failed', errorMessage);
    } finally {
      setIsSendingCode(false);
    }
  };

  const handleVerificationSuccess = async (code: string) => {
    // После успешной верификации пытаемся автоматически залогиниться с сохраненными креденциалами
    const savedEmail =
      displayEmail ||
      (typeof window !== 'undefined' ? sessionStorage.getItem('pending_verification_email') : null);
    const savedPassword =
      typeof window !== 'undefined'
        ? sessionStorage.getItem('pending_verification_password')
        : null;

    if (savedEmail && savedPassword) {
      try {
        await login(savedEmail, savedPassword);
        // Очищаем сохраненные креденциалы
        if (typeof window !== 'undefined') {
          sessionStorage.removeItem('pending_verification_password');
        }
        router.push('/dashboard');
      } catch (error) {
        // Если автоматический логин не удался, перенаправляем на login
        router.push('/login');
      }
    } else {
      // Если нет сохраненных креденциалов, перенаправляем на login
      router.push('/login');
    }
  };

  const handleLogout = async () => {
    await logout();
    router.push('/login');
  };

  // Определяем email для отображения
  const displayEmail = pendingEmail || user?.email;

  // Показываем loader во время проверки
  if (isChecking || loading) {
    return (
      <div className='min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-violet-900 flex items-center justify-center'>
        <div className='text-white'>
          <div className='inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-white'></div>
        </div>
      </div>
    );
  }

  if (isEmailVerified || !displayEmail) {
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
            <p className='text-gray-300'>We sent a verification code to</p>
            <p className='text-blue-400 font-mono text-sm mt-1'>{displayEmail}</p>
          </div>

          {/* Message */}
          <div className='bg-yellow-500/10 border border-yellow-500/20 rounded-lg p-4 mb-6'>
            <p className='text-sm text-yellow-400'>
              <strong>Access Restricted:</strong> You need to verify your email address to access
              the dashboard and other features.
            </p>
          </div>

          {/* Actions */}
          <div className='space-y-3'>
            <Button
              fullWidth
              onClick={handleSendCode}
              isLoading={isSendingCode}
              disabled={isSendingCode || resendCooldown > 0}
              className='bg-yellow-500 hover:bg-yellow-600 text-black'
            >
              <Mail className='w-4 h-4 mr-2' />
              {isSendingCode
                ? 'Sending Code...'
                : resendCooldown > 0
                  ? `Wait ${resendCooldown}s`
                  : 'Send Verification Code'}
            </Button>

            <Button
              fullWidth
              variant='ghost'
              onClick={() => router.push('/login')}
              className='text-gray-400 hover:text-white'
            >
              <ArrowLeft className='w-4 h-4 mr-2' />
              Back to Login
            </Button>
          </div>

          {/* Help text */}
          <div className='mt-6 text-center'>
            <p className='text-xs text-gray-400'>
              Check your email for the verification code. You can resend the code after{' '}
              {resendCooldown > 0 ? `${resendCooldown} seconds` : 'immediately'}.
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
        resendCooldown={resendCooldown}
        onResend={handleSendCode}
      />
    </main>
  );
}
