'use client';

import { useEffect } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';

export default function TelegramCallbackPage() {
  const router = useRouter();
  const searchParams = useSearchParams();

  useEffect(() => {
    // Check if this is a Telegram OAuth callback from backend
    const auth = searchParams.get('auth');
    const token = searchParams.get('token');
    
    if (auth === 'success' && token) {
      // This is a successful OAuth callback from backend
      console.log('[Telegram Callback] Successful OAuth callback detected');
      
      // Set flag for Telegram OAuth completion to trigger server discovery
      sessionStorage.setItem('telegram_oauth_completed', 'true');
      console.log('[Telegram Callback] Set telegram_oauth_completed flag');
      
      // Redirect to dashboard
      window.location.href = '/dashboard';
      return;
    }
    
    // Handle direct Telegram callback with tgAuthResult in hash
    const urlHash = typeof window !== 'undefined' ? window.location.hash : '';
    
    if (urlHash && urlHash.includes('tgAuthResult=')) {
      console.log('[Telegram Callback] Processing direct Telegram callback with hash');
      
      const hashParams = new URLSearchParams(urlHash.substring(1)); // Remove # and parse
      const tgAuthResult = hashParams.get('tgAuthResult');
      
      if (tgAuthResult) {
        try {
          // Decode and parse Telegram data
          const decodedResult = atob(tgAuthResult);
          const telegramData = JSON.parse(decodedResult);
          console.log('[Telegram Callback] Parsed Telegram data:', telegramData);
          
          // Send Telegram data to backend for processing
          fetch('/api/auth/telegram/callback', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify({
              telegramData,
              action: 'register'
            }),
          })
          .then(response => response.json())
          .then(data => {
            console.log('[Telegram Callback] Backend response:', data);
            
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
              
              // Set flag for Telegram OAuth completion
              sessionStorage.setItem('telegram_oauth_completed', 'true');
              console.log('[Telegram Callback] Set telegram_oauth_completed flag');
              
              // Redirect to dashboard
              window.location.href = '/dashboard';
            } else {
              console.error('[Telegram Callback] Authentication failed:', data);
              window.location.href = `/login?error=${encodeURIComponent(data.message || 'telegram_auth_failed')}`;
            }
          })
          .catch(error => {
            console.error('[Telegram Callback] Backend error:', error);
            window.location.href = '/login?error=telegram_auth_failed';
          });
        } catch (error) {
          console.error('[Telegram Callback] Failed to parse tgAuthResult:', error);
          window.location.href = '/login?error=invalid_telegram_data';
        }
      } else {
        console.error('[Telegram Callback] Missing tgAuthResult');
        window.location.href = '/login?error=missing_telegram_data';
      }
    } else {
      console.error('[Telegram Callback] No valid Telegram data found');
      window.location.href = '/login?error=invalid_callback';
    }
  }, [searchParams, router]);

  return (
    <div className='min-h-screen bg-gradient-to-br from-gray-900 via-purple-900 to-violet-900 flex items-center justify-center'>
      <div className='text-center'>
        <div className='w-12 h-12 border-4 border-blue-400 border-t-transparent rounded-full animate-spin mx-auto mb-4' />
        <h2 className='text-xl font-semibold text-white mb-2'>Redirecting...</h2>
        <p className='text-gray-300'>Please wait...</p>
      </div>
    </div>
  );
}
