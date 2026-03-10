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
          telegramData = JSON.parse(decodeURIComponent(tgAuthResult));
          console.log('[Telegram Callback] Parsed Telegram data:', telegramData);
        } catch (error) {
          console.error('[Telegram Callback] Failed to parse tgAuthResult:', error);
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

    // Clear the stored action
    if (typeof window !== 'undefined') {
      sessionStorage.removeItem('telegram_oauth_action');
    }

    // Redirect to backend Telegram callback with action and user data
    const callbackUrl = `${process.env.NEXT_PUBLIC_API_BASE_URL?.replace('/api', '') || 'http://localhost:5246'}/api/auth/oauth/telegram/callback?hash=${encodeURIComponent(telegramData.hash || '')}&state=${encodeURIComponent(state || '')}${action ? `&action=${encodeURIComponent(action)}` : ''}`;
    
    console.log('[Telegram Callback] Redirecting to backend callback:', callbackUrl);
    window.location.href = callbackUrl;
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
