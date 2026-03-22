import { NextRequest, NextResponse } from 'next/server';

export async function POST(request: NextRequest) {
  console.log('[Telegram Callback API] === TELEGRAM CALLBACK STARTED ===');
  
  try {
    const body = await request.json();
    console.log('[Telegram Callback API] Request body:', body);
    
    const { telegramData, action, linkingInfo } = body;
    
    if (!telegramData) {
      console.error('[Telegram Callback API] Missing telegramData');
      return NextResponse.json(
        { success: false, message: 'Missing telegram data' },
        { status: 400 }
      );
    }
    
    // Check for linking information
    let isLinking = false;
    let userId: string | null = null;
    
    if (linkingInfo) {
      isLinking = linkingInfo.action === 'link';
      userId = linkingInfo.userId;
      console.log('[Telegram Callback API] Linking info detected:', { isLinking, userId });
    }
    
    // Transform data to match backend expectations
    const backendRequest = {
      userData: {
        id: telegramData.id,
        firstName: telegramData.first_name || telegramData.firstName || '',
        username: telegramData.username || '',
        authDate: telegramData.auth_date || telegramData.authDate || 0,
        hash: telegramData.hash || ''
      },
      state: `${action || 'register'}_telegram_callback`,
      linkingAction: isLinking,
      userId: userId
    };
    
    console.log('[Telegram Callback API] Transformed request:', backendRequest);
    
    // Forward to backend for processing
    const backendUrl = `http://servereye-backend-dev/api/auth/oauth/telegram/callback?action=${action || 'auto'}`;
    
    console.log('[Telegram Callback API] Forwarding to backend:', backendUrl);
    
    const response = await fetch(backendUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(backendRequest),
    });
    
    if (!response.ok) {
      const errorText = await response.text();
      console.error('[Telegram Callback API] Backend error:', response.status, errorText);
      return NextResponse.json(
        { success: false, message: 'Backend authentication failed' },
        { status: response.status }
      );
    }
    
    const data = await response.json();
    console.log('[Telegram Callback API] Backend response:', data);
    
    return NextResponse.json(data);
    
  } catch (error) {
    console.error('[Telegram Callback API] Error:', error);
    return NextResponse.json(
      { success: false, message: 'Internal server error' },
      { status: 500 }
    );
  }
}
