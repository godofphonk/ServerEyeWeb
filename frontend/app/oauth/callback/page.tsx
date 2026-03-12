'use client';

import { useEffect, useState } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { useAuth } from '@/context/AuthContext';

export default function OAuthCallbackPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { checkAuth, isAuthenticated, loading } = useAuth();
  const [status, setStatus] = useState<'processing' | 'success' | 'error'>('processing');
  const [message, setMessage] = useState('Processing authentication...');

  useEffect(() => {
    const processCallback = async () => {
      try {
        const code = searchParams.get('code');
        const state = searchParams.get('state');
        const auth = searchParams.get('auth');
        const token = searchParams.get('token');
        const refreshToken = searchParams.get('refresh_token');
        
        console.log('[OAuth Callback] Processing:', { code: !!code, state: !!state, auth, hasToken: !!token, hasRefreshToken: !!refreshToken });
        
        // If this is a success redirect from backend with tokens
        if (auth === 'success' && token) {
          setStatus('processing');
          setMessage('Setting up authentication...');
          
          // Store tokens in localStorage
          if (typeof window !== 'undefined') {
            localStorage.setItem('jwt_token', token);
            if (refreshToken) {
              localStorage.setItem('refresh_token', refreshToken);
            }
            console.log('[OAuth Callback] Tokens stored in localStorage');
          }
          
          // Set cookies via API call
          try {
            const response = await fetch('/api/auth/session', {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
              },
              body: JSON.stringify({ token, refreshToken }),
            });
            
            if (response.ok) {
              console.log('[OAuth Callback] Tokens set via API');
            } else {
              console.log('[OAuth Callback] Failed to set tokens via API, but localStorage is set');
            }
          } catch (apiError) {
            console.log('[OAuth Callback] API error, but localStorage is set:', apiError);
          }
          
          // Force auth check
          await checkAuth();
          
          // Give time for auth to complete
          await new Promise(resolve => setTimeout(resolve, 1000));
          
          console.log('[OAuth Callback] Auth check completed:', { isAuthenticated, loading });
          
          if (isAuthenticated) {
            setStatus('success');
            setMessage('Authentication successful! Redirecting...');
            
            // Clean URL and redirect to dashboard
            window.history.replaceState({}, '', '/oauth/callback');
            setTimeout(() => {
              router.push('/dashboard');
            }, 1000);
          } else {
            setStatus('error');
            setMessage('Authentication failed. Please try again.');
            
            setTimeout(() => {
              router.push('/login');
            }, 3000);
          }
        } else if (code && state) {
          // Handle OAuth callback with code and state
          setStatus('processing');
          setMessage('Completing OAuth flow...');
          
          setTimeout(() => {
            router.push('/login');
          }, 2000);
        } else {
          setStatus('error');
          setMessage('Invalid OAuth callback');
          setTimeout(() => {
            router.push('/login');
          }, 3000);
        }
      } catch (error) {
        console.error('[OAuth Callback] Error:', error);
        setStatus('error');
        setMessage('An error occurred during authentication');
        setTimeout(() => {
          router.push('/login');
        }, 3000);
      }
    };

    processCallback();
  }, [searchParams, checkAuth, isAuthenticated, loading, router]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-900">
      <div className="text-center">
        <div className="mb-8">
          {status === 'processing' && (
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto"></div>
          )}
          {status === 'success' && (
            <div className="text-green-500 text-4xl mb-4">✓</div>
          )}
          {status === 'error' && (
            <div className="text-red-500 text-4xl mb-4">✗</div>
          )}
        </div>
        
        <h1 className="text-2xl font-bold text-white mb-4">
          {status === 'processing' && 'Processing Authentication'}
          {status === 'success' && 'Authentication Successful'}
          {status === 'error' && 'Authentication Failed'}
        </h1>
        
        <p className="text-gray-400">
          {message}
        </p>
        
        {status === 'error' && (
          <button
            onClick={() => router.push('/login')}
            className="mt-4 px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600 transition-colors"
          >
            Back to Login
          </button>
        )}
      </div>
    </div>
  );
}
