import { NextRequest, NextResponse } from 'next/server';

export async function POST(request: NextRequest) {
  try {
    const refreshToken = request.cookies.get('refresh_token')?.value;

    if (!refreshToken) {
      return NextResponse.json({ error: 'No refresh token found' }, { status: 401 });
    }

    // Forward refresh request to backend
    const backendUrl = process.env.NEXT_PUBLIC_API_URL + '/api'!;

    const response = await fetch(`${backendUrl}/auth/refresh`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      credentials: 'include',
      body: JSON.stringify({ refreshToken }),
    });

    if (!response.ok) {
      return NextResponse.json({ error: 'Refresh token failed' }, { status: response.status });
    }

    const data = await response.json();

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
