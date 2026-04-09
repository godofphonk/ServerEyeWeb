import { NextRequest, NextResponse } from 'next/server';

const API_BASE_URL = process.env.INTERNAL_API_URL || 'http://backend:8080/api';

async function proxyRequest(request: NextRequest, method: string) {
  try {
    const accessToken = request.cookies.get('access_token')?.value;
    const url = new URL(request.url);
    const path = url.pathname.replace('/api/proxy/', '');
    const search = url.search;
    const targetUrl = `${API_BASE_URL}${path}${search}`;

    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };

    if (accessToken) {
      headers['Authorization'] = `Bearer ${accessToken}`;
    }

    const fetchOptions: RequestInit = {
      method,
      headers,
    };

    if (method !== 'GET' && method !== 'HEAD') {
      try {
        const body = await request.json();
        fetchOptions.body = JSON.stringify(body);
      } catch {
        // No body
      }
    }

    const backendResponse = await fetch(targetUrl, fetchOptions);

    if (backendResponse.status === 401) {
      const refreshToken = request.cookies.get('refresh_token')?.value;
      if (refreshToken && accessToken) {
        const refreshResponse = await fetch(`${API_BASE_URL}auth/refresh`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ token: accessToken, refreshToken }),
        });

        if (refreshResponse.ok) {
          const refreshData = await refreshResponse.json();

          headers['Authorization'] = `Bearer ${refreshData.token}`;
          if (method !== 'GET' && method !== 'HEAD') {
            // fetchOptions.body is already set from the original request above
          }
          fetchOptions.headers = headers;

          const retryResponse = await fetch(targetUrl, fetchOptions);
          const retryData = await retryResponse.text();

          const response = new NextResponse(retryData, {
            status: retryResponse.status,
            headers: {
              'Content-Type': retryResponse.headers.get('Content-Type') || 'application/json',
            },
          });

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

    const responseData = await backendResponse.text();
    return new NextResponse(responseData, {
      status: backendResponse.status,
      headers: {
        'Content-Type': backendResponse.headers.get('Content-Type') || 'application/json',
      },
    });
  } catch (error) {
    return NextResponse.json({ message: 'Internal server error' }, { status: 500 });
  }
}

export async function GET(request: NextRequest) {
  return proxyRequest(request, 'GET');
}

export async function POST(request: NextRequest) {
  return proxyRequest(request, 'POST');
}

export async function PUT(request: NextRequest) {
  return proxyRequest(request, 'PUT');
}

export async function PATCH(request: NextRequest) {
  return proxyRequest(request, 'PATCH');
}

export async function DELETE(request: NextRequest) {
  return proxyRequest(request, 'DELETE');
}
