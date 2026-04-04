import { NextRequest, NextResponse } from 'next/server';

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();

    const { telegramData, action, linkingInfo } = body;

    if (!telegramData) {
      return NextResponse.json(
        { success: false, message: 'Missing telegram data' },
        { status: 400 },
      );
    }

    // Check for linking information
    let isLinking = false;
    let userId: string | null = null;

    if (linkingInfo) {
      isLinking = linkingInfo.action === 'link';
      userId = linkingInfo.userId;
    }

    // Transform data to match backend expectations
    const backendRequest = {
      userData: {
        id: telegramData.id,
        firstName: telegramData.first_name || telegramData.firstName || '',
        username: telegramData.username || '',
        authDate: telegramData.auth_date || telegramData.authDate || 0,
        hash: telegramData.hash || '',
      },
      state: `${action || 'register'}_telegram_callback`,
      linkingAction: isLinking,
      userId: userId,
    };

    // Forward to backend for processing
    const backendUrl = `http://servereye-backend-dev/api/auth/oauth/telegram/callback?action=${action || 'auto'}`;

    const response = await fetch(backendUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(backendRequest),
    });

    if (!response.ok) {
      const errorText = await response.text();
      return NextResponse.json(
        { success: false, message: 'Backend authentication failed' },
        { status: response.status },
      );
    }

    const data = await response.json();

    return NextResponse.json(data);
  } catch (error) {
    return NextResponse.json({ success: false, message: 'Internal server error' }, { status: 500 });
  }
}
