// LOGIN ROUTE - SHOULD LOAD!

import { NextRequest, NextResponse } from 'next/server';


const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://127.0.0.1:5246/api';

export async function POST(request: NextRequest) {
  try {

    const backendUrl = `${API_BASE_URL}/users/login`;

    const body = await request.json();

    const backendResponse = await fetch(backendUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });


    if (!backendResponse.ok) {
      const errorData = await backendResponse.json().catch(() => ({}));
      return NextResponse.json(
        { message: errorData.message || 'Login failed' },
        { status: backendResponse.status },
      );
    }

    const data = await backendResponse.json();

    const response = NextResponse.json({
      user: data.user,
      expiresIn: data.expiresIn,
    });

    response.cookies.set('access_token', data.token, {
      httpOnly: true,
      secure: process.env.NODE_ENV === 'production',
      sameSite: 'strict',
      path: '/',
      maxAge: data.expiresIn || 1800, // 30 минут
      domain: undefined, // Позволяем браузеру установить домен автоматически
    });

    response.cookies.set('refresh_token', data.refreshToken, {
      httpOnly: true,
      secure: process.env.NODE_ENV === 'production',
      sameSite: 'strict',
      path: '/',
      maxAge: 7 * 24 * 60 * 60, // 7 days
      domain: undefined, // Позволяем браузеру установить домен автоматически
    });


    return response;
  } catch (error) {
    return NextResponse.json({ message: 'Internal server error' }, { status: 500 });
  }
}
