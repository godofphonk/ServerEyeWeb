'use client';

import { useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';

export default function TelegramCallbackPage() {
  const router = useRouter();
  const searchParams = useSearchParams();

  useEffect(() => {
    // Telegram sends data as hash: #tgAuthResult=...
    const urlHash = typeof window !== 'undefined' ? window.location.hash : '';
    const state = searchParams.get('state');

    console.log('[Telegram Callback] Intercepted Telegram callback:', { hash: !!urlHash, state, urlHash });

    let telegramData = null;
    
    // Extract tgAuthResult from hash
    if (urlHash && urlHash.includes('tgAuthResult=')) {
      const hashParams = new URLSearchParams(urlHash.substring(1)); // Remove # and parse
      const tgAuthResult = hashParams.get('tgAuthResult');
      
      if (tgAuthResult) {
        try {
          // tgAuthResult is Base64 encoded, decode it first
          const decodedResult = atob(tgAuthResult);
          telegramData = JSON.parse(decodedResult);
          console.log('[Telegram Callback] Parsed Telegram data:', telegramData);
        } catch (error) {
          console.error('[Telegram Callback] Failed to parse tgAuthResult:', error);
          console.error('[Telegram Callback] tgAuthResult value:', tgAuthResult);
        }
      }
    }

    if (!telegramData) {
      console.error('[Telegram Callback] Missing or invalid Telegram data');
      router.push('/login?error=missing_telegram_data');
      return;
    }

    // Get action from sessionStorage (stored during challenge creation)
    const action = typeof window !== 'undefined' ? sessionStorage.getItem('telegram_oauth_action') : null;
    
    console.log('[Telegram Callback] Retrieved action from sessionStorage:', action);
    console.log('[Telegram Callback] sessionStorage contents:', {
      telegram_oauth_action: sessionStorage.getItem('telegram_oauth_action'),
      oauth_action: sessionStorage.getItem('oauth_action')
    });

    // Clear the stored action
    if (typeof window !== 'undefined') {
      sessionStorage.removeItem('telegram_oauth_action');
    }

    // Send POST request to backend Telegram callback
    const callbackUrl = `${process.env.NEXT_PUBLIC_API_BASE_URL?.replace('/api', '') || 'http://localhost:5246'}/api/auth/oauth/telegram/callback${action ? `?action=${encodeURIComponent(action)}` : ''}`;
    
    console.log('[Telegram Callback] Sending POST to backend callback:', callbackUrl);
    
    // Create request body matching backend expectations
    const requestBody = {
      UserData: {
        id: telegramData.id,
        first_name: telegramData.first_name,
        username: telegramData.username,
        auth_date: telegramData.auth_date,
        hash: telegramData.hash
      }
    };
    
    // Send POST request using fetch
    console.log('[Telegram Callback] Sending request body:', requestBody);
    
    // Save debug info to localStorage
    localStorage.setItem('telegram_debug_request', JSON.stringify(requestBody));
    localStorage.setItem('telegram_debug_url', callbackUrl);
    
    fetch(callbackUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(requestBody)
    })
    .then(response => {
      console.log('[Telegram Callback] Backend response status:', response.status);
      console.log('[Telegram Callback] Backend response headers:', response.headers);
      
      // Save response status to localStorage
      localStorage.setItem('telegram_debug_status', response.status.toString());
      localStorage.setItem('telegram_debug_statustext', response.statusText);
      
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }
      
      return response.json();
    })
    .then(data => {
      console.log('[Telegram Callback] Backend response data:', data);
      console.log('[Telegram Callback] Response success:', data.success);
      console.log('[Telegram Callback] Response message:', data.message);
      
      // Save response data to localStorage
      localStorage.setItem('telegram_debug_response', JSON.stringify(data));
      localStorage.setItem('telegram_debug_success', data.success ? 'true' : 'false');
      localStorage.setItem('telegram_debug_has_token', (data.token || data.refreshToken) ? 'true' : 'false');
      
      if (data.success && (data.token || data.refreshToken)) {
        // Store both tokens and redirect to dashboard
        if (data.token) {
          localStorage.setItem('token', data.token);
          localStorage.setItem('jwt_token', data.token);
          localStorage.setItem('access_token', data.token);
          
          // Also set cookie for middleware
          document.cookie = `access_token=${data.token}; path=/; max-age=3600; SameSite=Lax`;
          
          console.log('[Telegram Callback] Saved access token:', data.token.substring(0, 20) + '...');
        }
        if (data.refreshToken) {
          localStorage.setItem('refreshToken', data.refreshToken);
          
          // Also set refresh token cookie
          document.cookie = `refresh_token=${data.refreshToken}; path=/; max-age=604800; SameSite=Lax`;
          
          console.log('[Telegram Callback] Saved refresh token:', data.refreshToken.substring(0, 20) + '...');
        }
        
        // Verify tokens are saved
        console.log('[Telegram Callback] Verifying saved tokens:');
        console.log('- token:', !!localStorage.getItem('token'));
        console.log('- jwt_token:', !!localStorage.getItem('jwt_token'));
        console.log('- access_token:', !!localStorage.getItem('access_token'));
        console.log('- refreshToken:', !!localStorage.getItem('refreshToken'));
        
        localStorage.setItem('telegram_debug_result', 'SUCCESS - redirecting to dashboard');
        window.location.href = '/dashboard';
      } else {
        // Redirect to login with error
        console.log('[Telegram Callback] Authentication failed:', data);
        
        localStorage.setItem('telegram_debug_result', 'FAILED - redirecting to login');
        localStorage.setItem('telegram_debug_error', data.message || 'telegram_auth_failed');
        window.location.href = `/login?error=${encodeURIComponent(data.message || 'telegram_auth_failed')}`;
      }
    })
    .catch(error => {
      console.error('[Telegram Callback] Backend error:', error);
      
      // Save error to localStorage
      localStorage.setItem('telegram_debug_result', 'ERROR - redirecting to login');
      localStorage.setItem('telegram_debug_error', error.message);
      window.location.href = '/login?error=telegram_auth_failed';
    });
  }, [searchParams, router]);

  return (
    <div className='min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-violet-900 flex items-center justify-center'>
      <div className='text-center'>
        <div className='w-12 h-12 border-4 border-blue-400 border-t-transparent rounded-full animate-spin mx-auto mb-4' />
        <h2 className='text-xl font-semibold text-white mb-2'>Processing Telegram OAuth...</h2>
        <p className='text-gray-300'>Please wait...</p>
      </div>
    </div>
  );
}
