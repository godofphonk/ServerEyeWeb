'use client';

import { useEffect, Suspense } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';

function TelegramCallbackContent() {
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
          
          // Check for linking information in sessionStorage
          let linkingInfo = null;
          try {
            const storedLinkingInfo = sessionStorage.getItem('oauth_linking');
            if (storedLinkingInfo) {
              linkingInfo = JSON.parse(storedLinkingInfo);
              console.log('[Telegram Callback] Found linking info:', linkingInfo);
            }
          } catch (err) {
            console.error('[Telegram Callback] Error parsing linking info:', err);
          }
          
          // Get the action from sessionStorage (set by AuthContext when creating challenge)
          const storedAction = sessionStorage.getItem('telegram_oauth_action');
          const action = linkingInfo ? 'link' : (storedAction || 'auto');
          console.log('[Telegram Callback] Using action:', action, '(stored:', storedAction, ', linking:', !!linkingInfo, ')');
          
          // Send Telegram data to backend for processing
          fetch('/api/auth/telegram/callback', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify({
              telegramData,
              action: action,
              linkingInfo: linkingInfo
            }),
          })
          .then(response => response.json())
          .then(data => {
            console.log('[Telegram Callback] Backend response:', data);
            
            // Clear linking info and action from sessionStorage
            sessionStorage.removeItem('oauth_linking');
            sessionStorage.removeItem('telegram_oauth_action');
            
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
              
              // If this was a linking attempt, redirect to profile
              if (linkingInfo) {
                window.location.href = '/profile?linking=success';
              } else {
                // Otherwise redirect to dashboard
                window.location.href = '/dashboard';
              }
            } else {
              console.error('[Telegram Callback] Authentication failed:', data);
              
              // Check if this is a linking error
              if (linkingInfo) {
                if (data.message && data.message.toLowerCase().includes('already linked to another user')) {
                  window.location.href = '/profile?error=already_linked';
                } else if (data.message && data.message.toLowerCase().includes('already linked')) {
                  window.location.href = '/profile?error=already_linked';
                } else {
                  // Pass the actual error message to the profile page
                  const errorMessage = data.message || 'Failed to link Telegram account';
                  window.location.href = `/profile?error=linking_failed&message=${encodeURIComponent(errorMessage)}`;
                }
              } else {
                window.location.href = `/login?error=${encodeURIComponent(data.message || 'telegram_auth_failed')}`;
              }
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

export default function TelegramCallbackPage() {
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
      <TelegramCallbackContent />
    </Suspense>
  );
}
