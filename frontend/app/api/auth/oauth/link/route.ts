import { NextRequest, NextResponse } from 'next/server';

export async function POST(request: NextRequest) {
  const body = await request.json();
  const { code, state, provider } = body;

  if (!code || !state) {
    return NextResponse.json({ error: 'Missing required parameters' }, { status: 400 });
  }

  try {
    // Get JWT token from cookies
    const jwtToken =
      request.cookies.get('jwt_token')?.value || request.cookies.get('access_token')?.value;

    if (!jwtToken) {
      return NextResponse.json({ error: 'No authentication token' }, { status: 401 });
    }

    // Forward to backend with Authorization header
    const backendUrl = `${process.env.INTERNAL_API_URL || 'http://backend:8080/api'}/auth/oauth/link`;

    const response = await fetch(backendUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${jwtToken}`,
      },
      body: JSON.stringify({
        provider,
        code,
        state,
      }),
    });

    if (!response.ok) {
      const errorText = await response.text();
      return NextResponse.json(
        { error: 'Backend error', details: errorText },
        { status: response.status },
      );
    }

    const responseData = await response.json();

    return NextResponse.json(responseData);
  } catch (_error) {
    return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
  }
}
