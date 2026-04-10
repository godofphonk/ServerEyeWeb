'use client';

import { useEffect, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';

function OAuthCallbackContent() {
  const router = useRouter();
  const searchParams = useSearchParams();

  useEffect(() => {
    const code = searchParams.get('code');
    const state = searchParams.get('state');
    const error = searchParams.get('error');

    if (error) {
      router.push(`/login?error=${encodeURIComponent(error)}`);
      return;
    }

    // Validate OAuth parameters
    if (!code || !state) {
      router.push('/login?error=missing_parameters');
      return;
    }

    // Validate code length and format (OAuth codes are typically 20-256 chars)
    if (code.length < 20 || code.length > 256 || !/^[a-zA-Z0-9._-]+$/.test(code)) {
      router.push('/login?error=invalid_code');
      return;
    }

    // Validate state length and format (state should be at least 10 chars)
    if (state.length < 10 || state.length > 256 || !/^[a-zA-Z0-9._-]+$/.test(state)) {
      router.push('/login?error=invalid_state');
      return;
    }

    // Check if this is a linking request
    const linkingInfo =
      typeof window !== 'undefined' ? sessionStorage.getItem('oauth_linking') : null;

    if (linkingInfo) {
      try {
        const { action, provider, userId, state: expectedState } = JSON.parse(linkingInfo);

        // Verify state matches
        if (state.includes(expectedState)) {
          // Clear linking info
          sessionStorage.removeItem('oauth_linking');

          // Send to backend callback API with linking parameters
          const callbackUrl = `${process.env.NEXT_PUBLIC_API_URL!}/auth/oauth/callback`;

          fetch(callbackUrl, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify({
              code: code,
              state: state,
              provider: provider,
              linkingAction: true,
              userId: userId,
              codeVerifier: sessionStorage.getItem('oauth_code_verifier'),
            }),
          })
            .then(response => response.json())
            .then(async data => {
              if (data.success && (data.token || data.refreshToken)) {
                // Set HttpOnly cookies via session API
                await fetch('/api/auth/session', {
                  method: 'POST',
                  headers: { 'Content-Type': 'application/json' },
                  body: JSON.stringify({ token: data.token, refreshToken: data.refreshToken }),
                });

                // Redirect to profile for linking success
                window.location.href = '/profile?linking=success';
              } else {
                // Check for linking errors
                if (data.message && data.message.toLowerCase().includes('already linked')) {
                  window.location.href = '/profile?error=already_linked';
                } else {
                  window.location.href = `/profile?error=linking_failed&message=${encodeURIComponent(data.message || 'Failed to link account')}`;
                }
              }
            })
            .catch(error => {
              window.location.href = '/profile?error=linking_failed';
            });
          return;
        } else {
          sessionStorage.removeItem('oauth_linking');
        }
      } catch (err) {
        sessionStorage.removeItem('oauth_linking');
      }
    }

    // Not a linking request, proceed with normal OAuth flow

    // Extract provider from state
    const provider = state.split('_')[0]; // state format: provider_action_randomString

    // Send to backend callback API and handle response
    const callbackUrl = `${process.env.NEXT_PUBLIC_API_URL!}/auth/oauth/callback`;

    console.log('[OAuth Callback] Requesting:', callbackUrl);

    fetch(callbackUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        provider: provider,
        code: code,
        state: state,
        codeVerifier: sessionStorage.getItem('oauth_code_verifier'),
      }),
    })
      .then(response => {
        console.log('[OAuth Callback] Response status:', response.status);
        return response.json();
      })
      .then(async data => {
        console.log('[OAuth Callback] Response data:', data);

        if (data.success && (data.token || data.refreshToken)) {
          console.log('[OAuth Callback] Setting cookies via session API');

          // Set HttpOnly cookies via session API
          const sessionResponse = await fetch('/api/auth/session', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ token: data.token, refreshToken: data.refreshToken }),
          });

          console.log('[OAuth Callback] Session API response status:', sessionResponse.status);

          // Redirect to dashboard
          window.location.href = '/dashboard';
        } else {
          console.error('[OAuth Callback] OAuth failed:', data.message);
          window.location.href = `/login?error=${encodeURIComponent(data.message || 'oauth_auth_failed')}`;
        }
      })
      .catch(error => {
        console.error('[OAuth Callback] Request failed:', error);
        window.location.href = '/login?error=oauth_auth_failed';
      });
  }, [searchParams, router]);

  return (
    <div className='min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-violet-900 flex items-center justify-center'>
      <div className='text-center'>
        <div className='w-12 h-12 border-4 border-blue-400 border-t-transparent rounded-full animate-spin mx-auto mb-4' />
        <h2 className='text-xl font-semibold text-white mb-2'>Processing OAuth...</h2>
        <p className='text-gray-300'>Please wait...</p>
      </div>
    </div>
  );
}

export default function OAuthCallbackPage() {
  return (
    <Suspense
      fallback={
        <div className='min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-violet-900 flex items-center justify-center'>
          <div className='text-center'>
            <div className='w-12 h-12 border-4 border-blue-400 border-t-transparent rounded-full animate-spin mx-auto mb-4' />
            <h2 className='text-xl font-semibold text-white mb-2'>Loading...</h2>
            <p className='text-gray-300'>Please wait...</p>
          </div>
        </div>
      }
    >
      <OAuthCallbackContent />
    </Suspense>
  );
}
