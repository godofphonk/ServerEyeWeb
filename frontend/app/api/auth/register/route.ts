import { NextRequest, NextResponse } from 'next/server';

console.log('Register route file loaded!');

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://127.0.0.1:5246/api/';

export async function POST(request: NextRequest) {
  try {
    console.log('Register API route called via POST!');
    console.log('API_BASE_URL:', API_BASE_URL);
    console.log('Environment NEXT_PUBLIC_API_BASE_URL:', process.env.NEXT_PUBLIC_API_BASE_URL);
    
    const backendUrl = `${API_BASE_URL}/users/register`;
    console.log('Full backend URL:', backendUrl);
    
    console.log('About to parse request body...');
    const body = await request.json();
    console.log('Request body parsed:', body);

    console.log('About to make fetch request to:', backendUrl);
    const backendResponse = await fetch(`${API_BASE_URL}/users/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });

    console.log('Register - backend response:', {
      status: backendResponse.status,
      statusText: backendResponse.statusText,
      ok: backendResponse.ok
    });

    if (!backendResponse.ok) {
      const errorData = await backendResponse.json().catch(() => ({}));
      console.log('Register - backend error:', errorData);
      return NextResponse.json(
        { message: errorData.message || 'Registration failed' },
        { status: backendResponse.status }
      );
    }

    const data = await backendResponse.json();

    const response = NextResponse.json({
      user: data.user,
      expiresIn: data.expiresIn,
    });

    response.cookies.set('accessToken', data.token, {
      httpOnly: true,
      secure: process.env.NODE_ENV === 'production',
      sameSite: 'lax',
      path: '/',
      maxAge: data.expiresIn || 1800,
    });

    response.cookies.set('refreshToken', data.refreshToken, {
      httpOnly: true,
      secure: process.env.NODE_ENV === 'production',
      sameSite: 'lax',
      path: '/',
      maxAge: 7 * 24 * 60 * 60,
    });

    return response;
  } catch (error) {
    console.error('Register API route error:', error);
    return NextResponse.json(
      { message: 'Internal server error' },
      { status: 500 }
    );
  }
}
