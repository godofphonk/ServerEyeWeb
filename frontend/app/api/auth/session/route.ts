// SESSION ROUTE - SHOULD LOAD!
console.log('================================');
console.log('SESSION ROUTE FILE IS BEING LOADED!');
console.log('================================');

import { NextRequest, NextResponse } from 'next/server';

console.log('Session route file loaded!');

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://127.0.0.1:5246/api';

export async function GET(request: NextRequest) {
  console.log('Session API route called via GET!');
  console.log('API_BASE_URL:', API_BASE_URL);
  console.log('Environment NEXT_PUBLIC_API_BASE_URL:', process.env.NEXT_PUBLIC_API_BASE_URL);

  try {
    const accessToken = request.cookies.get('accessToken')?.value;
    const refreshToken = request.cookies.get('refreshToken')?.value;

    console.log('Session check - cookies:', {
      hasAccessToken: !!accessToken,
      accessTokenLength: accessToken?.length,
      hasRefreshToken: !!refreshToken,
      refreshTokenLength: refreshToken?.length,
      allCookies: request.cookies
        .getAll()
        .map(c => ({ name: c.name, value: c.value?.substring(0, 20) + '...' })),
    });

    if (!accessToken) {
      console.log('Session check - no access token found');
      return NextResponse.json({ user: null }, { status: 401 });
    }

    const backendResponse = await fetch(`${API_BASE_URL}/users/me`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${accessToken}`,
      },
    });

    console.log('Session check - backend response:', {
      status: backendResponse.status,
      statusText: backendResponse.statusText,
      ok: backendResponse.ok,
    });

    if (backendResponse.ok) {
      const userData = await backendResponse.json();
      console.log('Session check - user data received:', !!userData);
      console.log('Session check - full user data:', userData);
      console.log('Session check - isEmailVerified field:', userData?.isEmailVerified);
      return NextResponse.json({ user: userData });
    }

    if (backendResponse.status === 401 && refreshToken) {
      console.log('Session check - attempting token refresh');
      const refreshBody = {
        token: accessToken,
        refreshToken: refreshToken,
      };
      console.log('Session check - refresh request body:', {
        tokenLength: refreshBody.token?.length,
        refreshTokenLength: refreshBody.refreshToken?.length,
        tokenPrefix: refreshBody.token?.substring(0, 20) + '...',
        refreshTokenPrefix: refreshBody.refreshToken?.substring(0, 20) + '...',
      });
      const refreshResponse = await fetch(`${API_BASE_URL}/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(refreshBody),
      });

      const refreshData = await refreshResponse.json();
      console.log('Session check - refresh response:', {
        status: refreshResponse.status,
        statusText: refreshResponse.statusText,
        ok: refreshResponse.ok,
        data: refreshData,
      });

      if (refreshResponse.ok) {
        const userResponse = await fetch(`${API_BASE_URL}/users/me`, {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${refreshData.token}`,
          },
        });

        console.log('Session check - user response after refresh:', {
          status: userResponse.status,
          statusText: userResponse.statusText,
          ok: userResponse.ok,
        });

        if (userResponse.ok) {
          const userData = await userResponse.json();
          console.log('Session check - user data after refresh:', userData);
          console.log('Session check - isEmailVerified after refresh:', userData?.isEmailVerified);

          const response = NextResponse.json({ user: userData });

          response.cookies.set('accessToken', refreshData.token, {
            httpOnly: true,
            secure: false, // Явно false для dev
            sameSite: 'lax',
            path: '/',
            maxAge: refreshData.expiresIn || 1800,
          });

          response.cookies.set('refreshToken', refreshData.refreshToken, {
            httpOnly: true,
            secure: false, // Явно false для dev
            sameSite: 'lax',
            path: '/',
            maxAge: 7 * 24 * 60 * 60,
          });

          return response;
        }
      }
    }

    const response = NextResponse.json({ user: null }, { status: 401 });
    response.cookies.set('accessToken', '', { path: '/', maxAge: 0 });
    response.cookies.set('refreshToken', '', { path: '/', maxAge: 0 });
    return response;
  } catch (error) {
    console.error('Session API route error:', error);
    return NextResponse.json({ user: null }, { status: 500 });
  }
}
