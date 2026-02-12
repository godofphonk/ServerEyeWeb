import { NextRequest, NextResponse } from 'next/server';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://127.0.0.1:5246/api/';

export async function POST(request: NextRequest) {
  console.log('Login API route called via POST!');
  try {
    const body = await request.json();

    const backendResponse = await fetch(`${API_BASE_URL}users/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });

    console.log('Login - backend response:', {
      status: backendResponse.status,
      statusText: backendResponse.statusText,
      ok: backendResponse.ok
    });

    if (!backendResponse.ok) {
      const errorData = await backendResponse.json().catch(() => ({}));
      console.log('Login - backend error:', errorData);
      return NextResponse.json(
        { message: errorData.message || 'Login failed' },
        { status: backendResponse.status }
      );
    }

    const data = await backendResponse.json();
    console.log('Login - backend data:', {
      hasToken: !!data.token,
      hasRefreshToken: !!data.refreshToken,
      hasUser: !!data.user,
      expiresIn: data.expiresIn
    });

    const response = NextResponse.json({
      user: data.user,
      expiresIn: data.expiresIn,
    });

    response.cookies.set('accessToken', data.token, {
      httpOnly: true,
      secure: false, // Отключаем для dev
      sameSite: 'lax',
      path: '/',
      maxAge: data.expiresIn || 1800, // 30 минут
      domain: undefined, // Позволяем браузеру установить домен автоматически
    });

    response.cookies.set('refreshToken', data.refreshToken, {
      httpOnly: true,
      secure: false, // Отключаем для dev
      sameSite: 'lax',
      path: '/',
      maxAge: 7 * 24 * 60 * 60, // 7 days
      domain: undefined, // Позволяем браузеру установить домен автоматически
    });

    console.log('Login - setting cookies:', {
      accessTokenLength: data.token?.length,
      refreshTokenLength: data.refreshToken?.length,
      isProduction: process.env.NODE_ENV === 'production',
      expiresIn: data.expiresIn
    });

    return response;
  } catch (error) {
    console.error('Login API route error:', error);
    return NextResponse.json(
      { message: 'Internal server error' },
      { status: 500 }
    );
  }
}
