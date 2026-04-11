import { NextRequest, NextResponse } from 'next/server';

const COOKIE_DOMAIN = process.env.NEXT_PUBLIC_COOKIE_DOMAIN;

export async function POST(request: NextRequest) {
  try {
    const accessToken = request.cookies.get('access_token')?.value;
    const refreshToken = request.cookies.get('refresh_token')?.value;

    console.log('[Refresh POST] Request received', {
      hasAccessToken: !!accessToken,
      hasRefreshToken: !!refreshToken,
      accessTokenLength: accessToken?.length,
      refreshTokenLength: refreshToken?.length,
      backendUrl: process.env.INTERNAL_API_URL || 'http://backend:8080/api',
      cookieDomain: COOKIE_DOMAIN,
    });

    if (!refreshToken) {
      console.log('[Refresh POST] No refresh token found');
      return NextResponse.json({ error: 'No refresh token found' }, { status: 401 });
    }

    if (!accessToken) {
      console.log('[Refresh POST] No access token found');
      return NextResponse.json({ error: 'No access token found' }, { status: 401 });
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
      body: JSON.stringify({ token: accessToken, refreshToken }),
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

    const cookieOptions = {
      httpOnly: true,
      secure: process.env.NODE_ENV === 'production',
      sameSite: 'lax' as const,
      path: '/',
      ...(COOKIE_DOMAIN && { domain: COOKIE_DOMAIN }),
    };

    console.log('[Refresh POST] Cookie options', cookieOptions);

    // Set new tokens in cookies
    const nextResponse = NextResponse.json({
      token: data.token,
      refreshToken: data.refreshToken,
    });

    // Delete old cookies first to prevent duplication
    nextResponse.cookies.delete('access_token');
    nextResponse.cookies.delete('refresh_token');

    if (data.token) {
      nextResponse.cookies.set('access_token', data.token, {
        ...cookieOptions,
        maxAge: 3600, // 1 hour
      });
    }

    if (data.refreshToken) {
      nextResponse.cookies.set('refresh_token', data.refreshToken, {
        ...cookieOptions,
        maxAge: 604800, // 7 days
      });
    }

    return nextResponse;
  } catch (error) {
    return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
  }
}
