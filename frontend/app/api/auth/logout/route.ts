import { NextRequest, NextResponse } from 'next/server';

const API_BASE_URL = process.env.INTERNAL_API_URL || 'http://backend:8080/api';
const COOKIE_DOMAIN = process.env.NEXT_PUBLIC_COOKIE_DOMAIN;

function clearAuthCookies(response: NextResponse) {
  const baseOptions = {
    httpOnly: true,
    secure: process.env.NODE_ENV === 'production',
    sameSite: 'lax' as const,
    path: '/',
    maxAge: 0,
  };

  // Delete host-only cookies (set by email/password login)
  response.cookies.set('access_token', '', baseOptions);
  response.cookies.set('refresh_token', '', baseOptions);

  // Also delete cookies with COOKIE_DOMAIN (set by OAuth session endpoint)
  if (COOKIE_DOMAIN) {
    response.cookies.set('access_token', '', { ...baseOptions, domain: COOKIE_DOMAIN });
    response.cookies.set('refresh_token', '', { ...baseOptions, domain: COOKIE_DOMAIN });
  }
}

export async function POST(request: NextRequest) {
  try {
    const accessToken = request.cookies.get('access_token')?.value;

    if (accessToken) {
      await fetch(`${API_BASE_URL}/auth/logout`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${accessToken}`,
        },
      }).catch(() => {});
    }

    const response = NextResponse.json({ success: true });
    clearAuthCookies(response);
    return response;
  } catch (_error) {
    const response = NextResponse.json({ success: true });
    clearAuthCookies(response);
    return response;
  }
}
