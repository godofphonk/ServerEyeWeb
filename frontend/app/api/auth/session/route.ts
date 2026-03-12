// SESSION ROUTE - SHOULD LOAD!
console.log('================================');
console.log('SESSION ROUTE FILE IS BEING LOADED!');
console.log('================================');

import { NextRequest, NextResponse } from 'next/server';

console.log('Session route file loaded!');

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://127.0.0.1:5246/api';

export async function POST(request: NextRequest) {
  console.log('Session API route called via POST!');
  
  try {
    const body = await request.json();
    const { token, refreshToken } = body;
    
    console.log('Session POST - tokens received:', {
      hasToken: !!token,
      tokenLength: token?.length,
      hasRefreshToken: !!refreshToken,
      refreshTokenLength: refreshToken?.length
    });
    
    if (!token) {
      console.log('Session POST - no token provided');
      return NextResponse.json({ error: 'No token provided' }, { status: 400 });
    }
    
    const response = NextResponse.json({ success: true });
    
    // Set cookies
    response.cookies.set('access_token', token, {
      httpOnly: true,
      secure: false,
      sameSite: 'lax',
      path: '/',
      maxAge: 3600, // 1 hour
    });
    
    if (refreshToken) {
      response.cookies.set('refresh_token', refreshToken, {
        httpOnly: true,
        secure: false,
        sameSite: 'lax',
        path: '/',
        maxAge: 7 * 24 * 60 * 60, // 7 days
      });
    }
    
    console.log('Session POST - cookies set successfully');
    return response;
    
  } catch (error) {
    console.error('Session POST error:', error);
    return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
  }
}

export async function GET(request: NextRequest) {
  console.log('Session API route called via GET!');
  console.log('API_BASE_URL:', API_BASE_URL);
  console.log('Environment NEXT_PUBLIC_API_BASE_URL:', process.env.NEXT_PUBLIC_API_BASE_URL);
  console.log('Request URL:', request.url);
  console.log('Request headers:', Object.fromEntries(request.headers.entries()));

  try {
    const accessToken = request.cookies.get('access_token')?.value;
    const refreshToken = request.cookies.get('refresh_token')?.value;

    console.log('Session check - cookies:', {
      hasAccessToken: !!accessToken,
      accessTokenLength: accessToken?.length,
      accessTokenValue: accessToken?.substring(0, 50) + '...',
      hasRefreshToken: !!refreshToken,
      refreshTokenLength: refreshToken?.length,
      refreshTokenValue: refreshToken?.substring(0, 50) + '...',
      allCookies: request.cookies
        .getAll()
        .map(c => ({ name: c.name, value: c.value?.substring(0, 20) + '...' })),
    });

    if (!accessToken) {
      console.log('Session check - no access token found');
      return NextResponse.json({ user: null }, { status: 401 });
    }

    // Temporarily skip backend call and just return success if token exists
    console.log('Session check - token found, returning mock user for testing');
    
    // Try to decode token to get basic user info
    try {
      const tokenParts = accessToken.split('.');
      if (tokenParts.length === 3) {
        const payload = JSON.parse(atob(tokenParts[1]));
        const mockUser = {
          id: payload.sub || payload.nameid || 'unknown',
          email: payload.email || 'test@example.com',
          username: payload.username || payload.name || 'testuser',
          role: payload.role || 'user',
          isEmailVerified: true
        };
        console.log('Session check - decoded token user:', mockUser);
        return NextResponse.json({ user: mockUser });
      }
    } catch (decodeError) {
      console.log('Session check - failed to decode token:', decodeError);
    }

    // Fallback - return mock user
    const mockUser = {
      id: 'test-user-id',
      email: 'test@example.com',
      username: 'testuser',
      role: 'user',
      isEmailVerified: true
    };
    
    console.log('Session check - returning mock user');
    return NextResponse.json({ user: mockUser });
  } catch (error) {
    console.error('Session API route error:', error);
    return NextResponse.json({ user: null }, { status: 500 });
  }
}
