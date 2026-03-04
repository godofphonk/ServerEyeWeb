import { NextRequest, NextResponse } from 'next/server';

export async function POST(request: NextRequest) {
  const body = await request.json();
  const { code, state, provider } = body;

  console.log('[OAuth Link Direct] Received request:', { code: !!code, state: !!state, provider });

  if (!code || !state) {
    console.error('[OAuth Link Direct] Missing required parameters:', { code, state });
    return NextResponse.json(
      { error: 'Missing required parameters' },
      { status: 400 }
    );
  }

  try {
    // Get JWT token from cookies
    const jwtToken = request.cookies.get('jwt_token')?.value || 
                     request.cookies.get('access_token')?.value;
    
    console.log('[OAuth Link Direct] JWT token found:', !!jwtToken);

    if (!jwtToken) {
      console.error('[OAuth Link Direct] No JWT token found');
      return NextResponse.json(
        { error: 'No authentication token' },
        { status: 401 }
      );
    }

    // Forward to backend with Authorization header
    const backendUrl = `${process.env.NEXT_PUBLIC_API_BASE_URL?.replace('/api', '') || 'http://localhost:5246'}/api/auth/oauth/link`;
    
    console.log('[OAuth Link Direct] Forwarding to backend:', backendUrl);
    
    const response = await fetch(backendUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${jwtToken}`,
      },
      body: JSON.stringify({
        provider,
        code,
        state,
      }),
    });

    console.log('[OAuth Link Direct] Backend response status:', response.status);

    if (!response.ok) {
      const errorText = await response.text();
      console.error('[OAuth Link Direct] Backend error:', response.status, errorText);
      return NextResponse.json(
        { error: 'Backend error', details: errorText },
        { status: response.status }
      );
    }

    const responseData = await response.json();
    console.log('[OAuth Link Direct] Backend response data:', responseData);

    return NextResponse.json(responseData);

  } catch (error) {
    console.error('[OAuth Link Direct] Exception:', error);
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    );
  }
}
