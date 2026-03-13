'use client';

import { useEffect, useState, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { useAuth } from '@/context/AuthContext';

function AuthCallbackContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { setTokensFromCallback } = useAuth();
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
  const [error, setError] = useState<string>('');
  const [isProcessing, setIsProcessing] = useState(false);

  useEffect(() => {
    const handleCallback = async () => {
      // Prevent multiple calls
      if (isProcessing) {
        console.log('[AuthCallback] Already processing, skipping...');
        return;
      }
      
      setIsProcessing(true);
      
      try {
        // Extract tokens from URL parameters
        const token = searchParams.get('token');
        const refreshToken = searchParams.get('refreshToken');
        const provider = searchParams.get('provider');

        if (!token || !refreshToken || !provider) {
          throw new Error('Missing required parameters');
        }

        console.log(`[AuthCallback] Processing ${provider} OAuth callback`);
        console.log('[AuthCallback] Token length:', token.length);
        console.log('[AuthCallback] Refresh token length:', refreshToken.length);

        // Check if this is a linking request
        const linkingInfo = typeof window !== 'undefined' ? sessionStorage.getItem('oauth_linking') : null;
        
        if (linkingInfo) {
          const { action, provider: linkProvider, code: oauthCode, state: oauthState } = JSON.parse(linkingInfo);
          
          console.log('[AuthCallback] Linking check:', { action, linkProvider, hasCode: !!oauthCode, hasState: !!oauthState });

          if (action === 'link' && linkProvider === provider && oauthCode && oauthState) {
            console.log('[AuthCallback] This is a linking request, calling backend /api/auth/oauth/link');
            
            // Get current JWT token
            const jwtToken = typeof window !== 'undefined' ? localStorage.getItem('jwt_token') : null;
            
            if (!jwtToken) {
              throw new Error('No JWT token found for linking');
            }
            
            // Call backend linking endpoint
            const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/auth/oauth/link`, {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwtToken}`,
              },
              body: JSON.stringify({
                provider: linkProvider,
                code: oauthCode,
                state: oauthState,
              }),
            });

            if (!response.ok) {
              const errorData = await response.json();
              throw new Error(errorData.message || 'Failed to link account');
            }

            const linkData = await response.json();
            console.log('[AuthCallback] Successfully linked account:', linkData);
            
            // Update tokens from linking response
            if (linkData.token && linkData.refreshToken) {
              localStorage.setItem('jwt_token', linkData.token);
              localStorage.setItem('refresh_token', linkData.refreshToken);
            }
            
            // Clear linking sessionStorage
            if (typeof window !== 'undefined') {
              sessionStorage.removeItem('oauth_linking');
            }

            setStatus('success');
            await new Promise(resolve => setTimeout(resolve, 300));
            router.push('/profile');
            return;
          }
        }

        // Use the new method to set tokens and refresh user data
        await setTokensFromCallback(token, refreshToken);

        console.log(`[AuthCallback] Successfully authenticated with ${provider}`);
        console.log('[AuthCallback] Redirecting to dashboard...');
        setStatus('success');

        // Wait a bit for state to update, then redirect
        await new Promise(resolve => setTimeout(resolve, 300));
        
        console.log('[AuthCallback] Redirecting to dashboard now...');
        router.push('/dashboard');

      } catch (err) {
        console.error('[AuthCallback] Error:', err);
        setError(err instanceof Error ? err.message : 'Authentication failed');
        setStatus('error');

        // Redirect to login after error
        setTimeout(() => {
          router.push('/login?error=auth_failed');
        }, 3000);
      }
    };

    handleCallback();
  }, [searchParams, setTokensFromCallback, router, isProcessing]);

  if (status === 'loading') {
    return (
      <div className='min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-violet-900 flex items-center justify-center'>
        <div className='text-center'>
          <div className='w-12 h-12 border-4 border-blue-400 border-t-transparent rounded-full animate-spin mx-auto mb-4' />
          <h2 className='text-xl font-semibold text-white mb-2'>Completing Authentication</h2>
          <p className='text-gray-300'>Please wait while we set up your account...</p>
        </div>
      </div>
    );
  }

  if (status === 'error') {
    return (
      <div className='min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-violet-900 flex items-center justify-center'>
        <div className='text-center max-w-md mx-auto p-6'>
          <div className='w-16 h-16 bg-red-500/20 rounded-full flex items-center justify-center mx-auto mb-4'>
            <svg className='w-8 h-8 text-red-400' fill='none' stroke='currentColor' viewBox='0 0 24 24'>
              <path strokeLinecap='round' strokeLinejoin='round' strokeWidth={2} d='M6 18L18 6M6 6l12 12' />
            </svg>
          </div>
          <h2 className='text-2xl font-bold text-white mb-2'>Authentication Failed</h2>
          <p className='text-gray-300 mb-4'>{error}</p>
          <p className='text-sm text-gray-400'>Redirecting to login page...</p>
        </div>
      </div>
    );
  }

  return (
    <div className='min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-violet-900 flex items-center justify-center'>
      <div className='text-center'>
        <div className='w-16 h-16 bg-green-500/20 rounded-full flex items-center justify-center mx-auto mb-4'>
          <svg className='w-8 h-8 text-green-400' fill='none' stroke='currentColor' viewBox='0 0 24 24'>
            <path strokeLinecap='round' strokeLinejoin='round' strokeWidth={2} d='M5 13l4 4L19 7' />
          </svg>
        </div>
        <h2 className='text-2xl font-bold text-white mb-2'>Authentication Successful!</h2>
        <p className='text-gray-300'>Redirecting to dashboard...</p>
      </div>
    </div>
  );
}

export default function AuthCallbackPage() {
  return (
    <Suspense fallback={
      <div className='min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-violet-900 flex items-center justify-center'>
        <div className='text-center'>
          <div className='w-12 h-12 border-4 border-blue-400 border-t-transparent rounded-full animate-spin mx-auto mb-4' />
          <h2 className='text-xl font-semibold text-white mb-2'>Loading...</h2>
          <p className='text-gray-300'>Please wait...</p>
        </div>
      </div>
    }>
      <AuthCallbackContent />
    </Suspense>
  );
}
