'use client';

import { useEffect, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';

function OAuthInterceptContent() {
  const router = useRouter();
  const searchParams = useSearchParams();

  useEffect(() => {
    const code = searchParams.get('code');
    const state = searchParams.get('state');
    const error = searchParams.get('error');

    console.log('[OAuth Intercept] Intercepted OAuth callback:', { code: !!code, state: !!state, error });

    if (error) {
      console.error('[OAuth Intercept] OAuth error:', error);
      router.push(`/login?error=${encodeURIComponent(error)}`);
      return;
    }

    if (!code || !state) {
      console.error('[OAuth Intercept] Missing code or state');
      router.push('/login?error=missing_parameters');
      return;
    }

    // Check if this is a linking request
    const linkingInfo = typeof window !== 'undefined' ? sessionStorage.getItem('oauth_linking') : null;

    if (linkingInfo) {
      try {
        const { action, provider, userId, state: expectedState } = JSON.parse(linkingInfo);
        
        console.log('[OAuth Intercept] Found linking info:', { action, provider, userId, expectedState });

        // Verify state matches (state might have github_ prefix)
        if (state.includes(expectedState) || state === `github_${expectedState}`) {
          console.log('[OAuth Intercept] State matches, redirecting to backend callback with linking parameters');
          
          // Clear linking info
          sessionStorage.removeItem('oauth_linking');
          
          // Redirect to backend callback with linkingAction=true and userId
          const callbackUrl = `${process.env.NEXT_PUBLIC_API_BASE_URL?.replace('/api', '') || 'http://127.0.0.1:5246'}/api/auth/oauth/callback?code=${encodeURIComponent(code)}&state=${encodeURIComponent(state)}&provider=${encodeURIComponent(provider)}&linkingAction=true&userId=${encodeURIComponent(userId)}`;
          
          console.log('[OAuth Intercept] Redirecting to backend callback with linking:', callbackUrl);
          window.location.href = callbackUrl;
          return;
        } else {
          console.warn('[OAuth Intercept] State mismatch, clearing linking info');
          sessionStorage.removeItem('oauth_linking');
        }
      } catch (err) {
        console.error('[OAuth Intercept] Error parsing linking info:', err);
        sessionStorage.removeItem('oauth_linking');
      }
    }

    // Not a linking request, proceed with normal OAuth flow
    console.log('[OAuth Intercept] Not a linking request, forwarding to backend callback');
    
    // Forward to backend callback
    const callbackUrl = `${process.env.NEXT_PUBLIC_API_BASE_URL?.replace('/api', '') || 'http://127.0.0.1:5246'}/api/auth/oauth/callback?code=${encodeURIComponent(code)}&state=${encodeURIComponent(state)}`;
    window.location.href = callbackUrl;
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

export default function OAuthInterceptPage() {
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
      <OAuthInterceptContent />
    </Suspense>
  );
}
