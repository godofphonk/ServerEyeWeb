// SESSION ROUTE
import { NextRequest, NextResponse } from 'next/server';

const API_BASE_URL = process.env.INTERNAL_API_URL || 'http://backend:8080/api';
const COOKIE_DOMAIN = process.env.NEXT_PUBLIC_COOKIE_DOMAIN;

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { token, refreshToken } = body;

    if (!token) {
      return NextResponse.json({ error: 'No token provided' }, { status: 400 });
    }

    const response = NextResponse.json({ success: true });

    // Delete old cookies first to prevent duplication
    response.cookies.delete('access_token');
    response.cookies.delete('refresh_token');

    const cookieOptions = {
      httpOnly: true,
      secure: process.env.NODE_ENV === 'production',
      sameSite: 'lax' as const,
      path: '/',
      maxAge: 3600, // 1 hour
      ...(COOKIE_DOMAIN && { domain: COOKIE_DOMAIN }),
    };

    // Set cookies
    response.cookies.set('access_token', token, cookieOptions);

    if (refreshToken) {
      response.cookies.set('refresh_token', refreshToken, {
        ...cookieOptions,
        maxAge: 7 * 24 * 60 * 60, // 7 days
      });
    }

    return response;
  } catch (error) {
    return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
  }
}

export async function GET(request: NextRequest) {
  try {
    const accessToken = request.cookies.get('access_token')?.value;
    const _refreshToken = request.cookies.get('refresh_token')?.value;

    if (!accessToken) {
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

    if (backendResponse.status === 401) {
      // Don't automatically refresh - let the client handle refresh through axios interceptor
      // This prevents infinite refresh loops
      return NextResponse.json({ user: null }, { status: 401 });
    }

    const response = NextResponse.json({ user: null }, { status: 401 });
    response.cookies.set('access_token', '', { path: '/', maxAge: 0 });
    response.cookies.set('refresh_token', '', { path: '/', maxAge: 0 });
    return response;
  } catch (error) {
    return NextResponse.json({ user: null }, { status: 500 });
  }
}
