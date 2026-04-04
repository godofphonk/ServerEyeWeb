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

    if (!code || !state) {
      router.push('/login?error=missing_parameters');
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
          const callbackUrl = `${process.env.NEXT_PUBLIC_API_BASE_URL?.replace('/api', '') || 'http://127.0.0.1:5246'}/api/auth/oauth/callback`;

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
            .then(data => {
              if (data.success && (data.token || data.refreshToken)) {
                // Store tokens
                if (data.token) {
                  localStorage.setItem('jwt_token', data.token);
                  localStorage.setItem('access_token', data.token);
                  document.cookie = `access_token=${data.token}; path=/; max-age=3600; SameSite=Lax`;
                }
                if (data.refreshToken) {
                  localStorage.setItem('refreshToken', data.refreshToken);
                  document.cookie = `refresh_token=${data.refreshToken}; path=/; max-age=604800; SameSite=Lax`;
                }

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
    const callbackUrl = `${process.env.NEXT_PUBLIC_API_BASE_URL?.replace('/api', '') || 'http://127.0.0.1:5246'}/api/auth/oauth/callback`;

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
      .then(response => response.json())
      .then(data => {
        if (data.success && (data.token || data.refreshToken)) {
          // Store tokens
          if (data.token) {
            localStorage.setItem('jwt_token', data.token);
            localStorage.setItem('access_token', data.token);
            document.cookie = `access_token=${data.token}; path=/; max-age=3600; SameSite=Lax`;
          }
          if (data.refreshToken) {
            localStorage.setItem('refreshToken', data.refreshToken);
            document.cookie = `refresh_token=${data.refreshToken}; path=/; max-age=604800; SameSite=Lax`;
          }

          // Redirect to dashboard
          window.location.href = '/dashboard';
        } else {
          window.location.href = `/login?error=${encodeURIComponent(data.message || 'oauth_auth_failed')}`;
        }
      })
      .catch(error => {
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
