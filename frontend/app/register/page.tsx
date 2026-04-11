'use client';

import { useEffect, useState } from 'react';
import { motion } from 'framer-motion';
import { ArrowRight, AlertCircle } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/context/AuthContext';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { EmailVerificationModal } from '@/components/auth/EmailVerificationModal';
import { useToast } from '@/hooks/useToast';

export default function RegisterPage() {
  const router = useRouter();
  const { register, refreshUserData, getOAuthURL } = useAuth();
  const toast = useToast();
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: '',
  });
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isOAuthLoading, setIsOAuthLoading] = useState<string | null>(null);
  const [showVerificationModal, setShowVerificationModal] = useState(false);

  // Handle OAuth callback errors
  useEffect(() => {
    const urlParams = new URLSearchParams(window.location.search);
    const oauthError = urlParams.get('error');

    if (oauthError) {
      switch (oauthError) {
        case 'user_already_exists':
          setError('An account with this provider already exists. Please login instead.');
          break;
        case 'backend_error':
          setError('OAuth registration failed. Please try again.');
          break;
        case 'callback_exception':
          setError('An error occurred during OAuth registration.');
          break;
        case 'access_denied':
          setError('Access denied. You cancelled the authentication.');
          break;
        default:
          setError(`OAuth registration failed: ${oauthError}`);
      }

      // Clean URL
      window.history.replaceState({}, document.title, '/register');
    }
  }, []);

  const passwordStrength = (password: string) => {
    if (password.length === 0) return { strength: 0, label: '' };
    if (password.length < 6) return { strength: 1, label: 'Weak', color: 'bg-red-500' };
    if (password.length < 10) return { strength: 2, label: 'Medium', color: 'bg-yellow-500' };
    if (!/(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/.test(password))
      return { strength: 2, label: 'Medium', color: 'bg-yellow-500' };
    return { strength: 3, label: 'Strong', color: 'bg-green-500' };
  };

  const strength = passwordStrength(formData.password);

  const handleVerificationSuccess = async () => {
    // User has verified email, refresh auth context and redirect to dashboard
    try {
      // Wait a bit for backend to update the database
      await new Promise(resolve => setTimeout(resolve, 500));

      // Refresh user data to get updated isEmailVerified status
      if (refreshUserData) {
        await refreshUserData();
      }
    } catch (error) {
      /* ignore error */
    }

    router.push('/dashboard');
  };

  const handleOAuthRegister = async (provider: string) => {
    setError('');
    setIsOAuthLoading(provider);

    try {
      const authURL = await getOAuthURL(provider, 'register');
      window.location.href = authURL;
    } catch (err: any) {
      // eslint-disable-line @typescript-eslint/no-explicit-any
      setError(err.message || `Failed to authenticate with ${provider}`);
      setIsOAuthLoading(null);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (formData.password !== formData.confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    if (formData.password.length < 8) {
      setError('Password must be at least 8 characters');
      return;
    }

    setIsLoading(true);

    try {
      // Generate username from email (part before @)
      const username = formData.email.split('@')[0];
      await register(formData.email, username, formData.password);

      // Show verification modal instead of redirecting
      setShowVerificationModal(true);

      toast.info('Registration Successful', 'Please verify your email to continue');
    } catch (err: any) {
      // eslint-disable-line @typescript-eslint/no-explicit-any
      setError(err.message || 'Registration failed');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main className='min-h-screen bg-black text-white flex items-center justify-center p-6'>
      <div className='absolute inset-0 bg-gradient-to-br from-blue-600/20 via-purple-600/20 to-pink-600/20 opacity-50' />
      <div className='absolute inset-0 bg-[radial-gradient(ellipse_at_center,_var(--tw-gradient-stops))] from-blue-900/20 via-transparent to-transparent' />

      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className='relative z-10 w-full max-w-md'
      >
        <div className='bg-white/10 backdrop-blur-xl border border-white/20 rounded-3xl p-8'>
          <div className='text-center mb-8'>
            <Link href='/' className='inline-block mb-6'>
              <div className='text-3xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent'>
                ServerEye
              </div>
            </Link>
            <h1 className='text-3xl font-bold mb-2'>Create Account</h1>
            <p className='text-gray-400'>Start monitoring your servers today</p>
          </div>

          {error && (
            <motion.div
              initial={{ opacity: 0, y: -10 }}
              animate={{ opacity: 1, y: 0 }}
              className='mb-6 p-4 bg-red-500/10 border border-red-500/20 rounded-xl flex items-center gap-3'
            >
              <AlertCircle className='w-5 h-5 text-red-400 flex-shrink-0' />
              <p className='text-sm text-red-400'>{error}</p>
            </motion.div>
          )}

          <form onSubmit={handleSubmit} className='space-y-6'>
            <Input
              type='email'
              label='Email'
              placeholder='you@example.com'
              value={formData.email}
              onChange={e => setFormData({ ...formData, email: e.target.value })}
              required
              disabled={isLoading}
            />

            <div>
              <Input
                type='password'
                label='Password'
                placeholder='••••••••'
                value={formData.password}
                onChange={e => setFormData({ ...formData, password: e.target.value })}
                required
                disabled={isLoading}
              />
              {formData.password && (
                <div className='mt-3'>
                  <div className='flex gap-2 mb-2'>
                    {[1, 2, 3].map(level => (
                      <div
                        key={level}
                        className={`h-1 flex-1 rounded-full transition-colors ${
                          level <= strength.strength ? strength.color : 'bg-gray-700'
                        }`}
                      />
                    ))}
                  </div>
                  <p className='text-xs text-gray-400'>
                    Password strength:{' '}
                    <span className={strength.strength >= 2 ? 'text-green-400' : 'text-yellow-400'}>
                      {strength.label}
                    </span>
                  </p>
                </div>
              )}
            </div>

            <Input
              type='password'
              label='Confirm Password'
              placeholder='••••••••'
              value={formData.confirmPassword}
              onChange={e => setFormData({ ...formData, confirmPassword: e.target.value })}
              required
              disabled={isLoading}
            />

            <div className='space-y-3 text-sm'>
              <label className='flex items-start gap-3 cursor-pointer'>
                <input type='checkbox' required className='mt-1 rounded' />
                <span className='text-gray-400'>
                  I agree to the{' '}
                  <Link href='/terms' className='text-blue-400 hover:text-blue-300'>
                    Terms of Service
                  </Link>{' '}
                  and{' '}
                  <Link href='/privacy' className='text-blue-400 hover:text-blue-300'>
                    Privacy Policy
                  </Link>
                </span>
              </label>
            </div>

            <Button type='submit' fullWidth isLoading={isLoading}>
              <span className='flex items-center gap-2'>
                Create Account
                <ArrowRight className='w-5 h-5' />
              </span>
            </Button>
          </form>

          {/* Divider */}
          <div className='my-8 flex items-center gap-4'>
            <div className='flex-1 h-px bg-white/10' />
            <span className='text-sm text-gray-400'>Or sign up with</span>
            <div className='flex-1 h-px bg-white/10' />
          </div>

          {/* OAuth Buttons */}
          <div className='grid grid-cols-3 gap-3'>
            <Button
              variant='secondary'
              disabled={isOAuthLoading === 'github' || isLoading}
              onClick={() => handleOAuthRegister('github')}
            >
              {isOAuthLoading === 'github' ? (
                <div className='w-7 h-7 border-2 border-gray-400 border-t-transparent rounded-full animate-spin' />
              ) : (
                <svg className='w-7 h-7 text-white' viewBox='0 0 24 24' fill='currentColor'>
                  <path d='M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z' />
                </svg>
              )}
            </Button>

            <Button
              variant='secondary'
              disabled={isOAuthLoading === 'google' || isLoading}
              onClick={() => handleOAuthRegister('google')}
            >
              {isOAuthLoading === 'google' ? (
                <div className='w-7 h-7 border-2 border-gray-400 border-t-transparent rounded-full animate-spin' />
              ) : (
                <svg className='w-7 h-7 text-white' viewBox='0 0 24 24'>
                  <path
                    fill='currentColor'
                    d='M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z'
                  />
                  <path
                    fill='currentColor'
                    d='M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z'
                  />
                  <path
                    fill='currentColor'
                    d='M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z'
                  />
                  <path
                    fill='currentColor'
                    d='M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z'
                  />
                </svg>
              )}
            </Button>

            <Button
              variant='secondary'
              disabled={isOAuthLoading === 'telegram' || isLoading}
              onClick={() => handleOAuthRegister('telegram')}
            >
              {isOAuthLoading === 'telegram' ? (
                <div className='w-7 h-7 border-2 border-gray-400 border-t-transparent rounded-full animate-spin' />
              ) : (
                <svg className='w-7 h-7 text-white' viewBox='0 0 24 24' fill='currentColor'>
                  <path d='M12 0C5.373 0 0 5.373 0 12s5.373 12 12 12 12-5.373 12-12S18.627 0 12 0zm5.894 8.221l-1.97 9.28c-.145.658-.537.818-1.084.508l-3-2.21-1.446 1.394c-.14.18-.357.295-.6.295-.002 0-.003 0-.005 0l.213-3.054 5.56-5.022c.24-.213-.054-.334-.373-.121l-6.869 4.326-2.96-.924c-.64-.203-.658-.64.135-.954l11.566-4.458c.538-.196 1.006.128.832.941z' />
                </svg>
              )}
            </Button>
          </div>

          <div className='mt-8 text-center text-sm'>
            <span className='text-gray-400'>Already have an account? </span>
            <Link href='/login' className='text-blue-400 hover:text-blue-300 font-semibold'>
              Sign in
            </Link>
          </div>
        </div>
      </motion.div>

      {/* Email Verification Modal */}
      <EmailVerificationModal
        isOpen={showVerificationModal}
        onClose={() => setShowVerificationModal(false)}
        email={formData.email}
        onSuccess={handleVerificationSuccess}
      />
    </main>
  );
}
