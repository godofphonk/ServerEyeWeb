'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';

export default function OAuthLinkHandlerPage() {
  const router = useRouter();
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
  const [error, setError] = useState<string>('');

  useEffect(() => {
    const handleLinking = async () => {
      try {
        // Try to get linking info from URL params first (passed from dashboard for security),
        // then fall back to sessionStorage for backward compatibility
        let action: string | null = null;
        let provider: string | null = null;
        let code: string | null = null;
        let state: string | null = null;

        const urlParams = new URLSearchParams(window.location.search);
        if (urlParams.get('code') && urlParams.get('provider')) {
          action = urlParams.get('action');
          provider = urlParams.get('provider');
          code = urlParams.get('code');
          state = urlParams.get('state');
        } else {
          const linkingInfo =
            typeof window !== 'undefined' ? sessionStorage.getItem('oauth_linking') : null;

          if (!linkingInfo) {
            throw new Error('No linking information found');
          }

          ({ action, provider, code, state } = JSON.parse(linkingInfo));
        }

        if (action !== 'link' || !provider || !code || !state) {
          throw new Error('Invalid linking information');
        }

        // Get JWT token
        const jwtToken = typeof window !== 'undefined' ? localStorage.getItem('jwt_token') : null;

        if (!jwtToken) {
          throw new Error('No authentication token found. Please log in first.');
        }

        // Call backend linking endpoint
        const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/auth/oauth/link`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${jwtToken}`,
          },
          body: JSON.stringify({
            provider: provider,
            code: code,
            state: state,
          }),
        });

        if (!response.ok) {
          const errorData = await response.json();
          throw new Error(errorData.message || 'Failed to link account');
        }

        const linkData = await response.json();

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

        // Redirect to profile after short delay
        setTimeout(() => {
          router.push('/profile');
        }, 1500);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to link account');
        setStatus('error');

        // Clear linking info on error
        if (typeof window !== 'undefined') {
          sessionStorage.removeItem('oauth_linking');
        }

        // Redirect to profile after error
        setTimeout(() => {
          router.push('/profile');
        }, 3000);
      }
    };

    handleLinking();
  }, [router]);

  if (status === 'loading') {
    return (
      <div className='min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-violet-900 flex items-center justify-center'>
        <div className='text-center'>
          <div className='w-12 h-12 border-4 border-blue-400 border-t-transparent rounded-full animate-spin mx-auto mb-4' />
          <h2 className='text-xl font-semibold text-white mb-2'>Linking Account...</h2>
          <p className='text-gray-300'>Please wait while we connect your account...</p>
        </div>
      </div>
    );
  }

  if (status === 'error') {
    return (
      <div className='min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-violet-900 flex items-center justify-center'>
        <div className='text-center max-w-md mx-auto p-6'>
          <div className='w-16 h-16 bg-red-500/20 rounded-full flex items-center justify-center mx-auto mb-4'>
            <svg
              className='w-8 h-8 text-red-400'
              fill='none'
              stroke='currentColor'
              viewBox='0 0 24 24'
            >
              <path
                strokeLinecap='round'
                strokeLinejoin='round'
                strokeWidth={2}
                d='M6 18L18 6M6 6l12 12'
              />
            </svg>
          </div>
          <h2 className='text-2xl font-bold text-white mb-2'>Linking Failed</h2>
          <p className='text-gray-300 mb-4'>{error}</p>
          <p className='text-sm text-gray-400'>Redirecting back to profile...</p>
        </div>
      </div>
    );
  }

  return (
    <div className='min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-violet-900 flex items-center justify-center'>
      <div className='text-center'>
        <div className='w-16 h-16 bg-green-500/20 rounded-full flex items-center justify-center mx-auto mb-4'>
          <svg
            className='w-8 h-8 text-green-400'
            fill='none'
            stroke='currentColor'
            viewBox='0 0 24 24'
          >
            <path strokeLinecap='round' strokeLinejoin='round' strokeWidth={2} d='M5 13l4 4L19 7' />
          </svg>
        </div>
        <h2 className='text-2xl font-bold text-white mb-2'>Account Linked Successfully!</h2>
        <p className='text-gray-300'>Redirecting to profile...</p>
      </div>
    </div>
  );
}
