// SESSION ROUTE
import { NextRequest, NextResponse } from 'next/server';

const API_BASE_URL = process.env.INTERNAL_API_URL || 'http://backend:8080/api';

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { token, refreshToken } = body;

    console.log('[Session API] POST request received');
    console.log('[Session API] Token provided:', !!token);
    console.log('[Session API] Refresh token provided:', !!refreshToken);
    console.log('[Session API] NODE_ENV:', process.env.NODE_ENV);

    if (!token) {
      console.error('[Session API] No token provided');
      return NextResponse.json({ error: 'No token provided' }, { status: 400 });
    }

    const response = NextResponse.json({ success: true });

    // Set cookies
    response.cookies.set('access_token', token, {
      httpOnly: true,
      secure: process.env.NODE_ENV === 'production',
      sameSite: 'lax',
      path: '/',
      maxAge: 3600, // 1 hour
    });

    console.log('[Session API] access_token cookie set with secure:', process.env.NODE_ENV === 'production');

    if (refreshToken) {
      response.cookies.set('refresh_token', refreshToken, {
        httpOnly: true,
        secure: process.env.NODE_ENV === 'production',
        sameSite: 'lax',
        path: '/',
        maxAge: 7 * 24 * 60 * 60, // 7 days
      });

      console.log('[Session API] refresh_token cookie set with secure:', process.env.NODE_ENV === 'production');
    }

    console.log('[Session API] Response sent successfully');
    return response;
  } catch (error) {
    console.error('[Session API] Error:', error);
    return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
  }
}

export async function GET(request: NextRequest) {
  try {
    const accessToken = request.cookies.get('access_token')?.value;
    const refreshToken = request.cookies.get('refresh_token')?.value;

    console.log('[Session API] GET request received');
    console.log('[Session API] access_token cookie present:', !!accessToken);
    console.log('[Session API] refresh_token cookie present:', !!refreshToken);
    console.log('[Session API] API_BASE_URL:', API_BASE_URL);

    if (!accessToken) {
      console.log('[Session API] No access_token cookie found');
      return NextResponse.json({ user: null }, { status: 401 });
    }

      const backendResponse = await fetch(`${API_BASE_URL}/users/me`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${accessToken}`,
      },
    });

    if (backendResponse.ok) {
      const userData = await backendResponse.json();
      return NextResponse.json({ user: userData });
    }

    if (backendResponse.status === 401 && refreshToken) {
      const refreshBody = {
        token: accessToken,
        refreshToken: refreshToken,
      };

      const refreshResponse = await fetch(`${API_BASE_URL}/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(refreshBody),
      });

      const refreshData = await refreshResponse.json();

      if (refreshResponse.ok) {
        const userResponse = await fetch(`${API_BASE_URL}/users/me`, {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${refreshData.token}`,
          },
        });

        if (userResponse.ok) {
          const userData = await userResponse.json();
          const response = NextResponse.json({ user: userData });

          response.cookies.set('access_token', refreshData.token, {
            httpOnly: true,
            secure: process.env.NODE_ENV === 'production',
            sameSite: 'lax',
            path: '/',
            maxAge: refreshData.expiresIn || 1800,
          });

          response.cookies.set('refresh_token', refreshData.refreshToken, {
            httpOnly: true,
            secure: process.env.NODE_ENV === 'production',
            sameSite: 'lax',
            path: '/',
            maxAge: 7 * 24 * 60 * 60,
          });

          return response;
        }
      }
    }

    const response = NextResponse.json({ user: null }, { status: 401 });
    response.cookies.set('access_token', '', { path: '/', maxAge: 0 });
    response.cookies.set('refresh_token', '', { path: '/', maxAge: 0 });
    return response;
  } catch (error) {
    return NextResponse.json({ user: null }, { status: 500 });
  }
}
