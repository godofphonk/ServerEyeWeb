import { NextRequest, NextResponse } from 'next/server';

export async function POST(request: NextRequest) {
  try {
    const refreshToken = request.cookies.get('refresh_token')?.value;

    console.log('[Refresh POST] Request received', {
      hasRefreshToken: !!refreshToken,
      refreshTokenLength: refreshToken?.length,
      backendUrl: process.env.INTERNAL_API_URL || 'http://backend:8080/api',
    });

    if (!refreshToken) {
      console.log('[Refresh POST] No refresh token found');
      return NextResponse.json({ error: 'No refresh token found' }, { status: 401 });
    }

    // Forward refresh request to backend
    const backendUrl = process.env.INTERNAL_API_URL || 'http://backend:8080/api';

    console.log('[Refresh POST] Calling backend', {
      url: `${backendUrl}/auth/refresh`,
    });

    const response = await fetch(`${backendUrl}/auth/refresh`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      credentials: 'include',
      body: JSON.stringify({ refreshToken }),
    });

    console.log('[Refresh POST] Backend response', {
      status: response.status,
      ok: response.ok,
    });

    if (!response.ok) {
      const errorText = await response.text();
      console.log('[Refresh POST] Backend error', {
        status: response.status,
        errorText,
      });
      return NextResponse.json({ error: 'Refresh token failed' }, { status: response.status });
    }

    const data = await response.json();

    console.log('[Refresh POST] Backend success', {
      hasToken: !!data.token,
      hasRefreshToken: !!data.refreshToken,
    });

    // Set new tokens in cookies
    const nextResponse = NextResponse.json({
      token: data.token,
      refreshToken: data.refreshToken,
    });

    if (data.token) {
      nextResponse.cookies.set('access_token', data.token, {
        httpOnly: true,
        secure: process.env.NODE_ENV === 'production',
        sameSite: 'lax',
        maxAge: 3600, // 1 hour
      });
    }

    if (data.refreshToken) {
      nextResponse.cookies.set('refresh_token', data.refreshToken, {
        httpOnly: true,
        secure: process.env.NODE_ENV === 'production',
        sameSite: 'lax',
        maxAge: 604800, // 7 days
      });
    }

    return nextResponse;
  } catch (error) {
    return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
  }
}
