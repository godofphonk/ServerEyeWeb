import { NextRequest, NextResponse } from 'next/server';

export async function GET(request: NextRequest) {
  const searchParams = request.nextUrl.searchParams;
  const code = searchParams.get('code');
  const state = searchParams.get('state');
  const error = searchParams.get('error');

  console.log('[OAuth Google Callback] Received request:', { code: !!code, state: !!state, error });

  // Handle OAuth errors
  if (error) {
    console.error('[OAuth Google Callback] Error:', error);
    return NextResponse.redirect(
      new URL(`/login?error=${encodeURIComponent(error)}`, request.url)
    );
  }

  // Validate required parameters
  if (!code || !state) {
    console.error('[OAuth Google Callback] Missing required parameters:', { code, state });
    return NextResponse.redirect(
      new URL('/login?error=missing_parameters', request.url)
    );
  }

  try {
    // Forward the callback to the backend as POST (backend expects POST, not GET)
    const backendUrl = `${process.env.NEXT_PUBLIC_API_BASE_URL?.replace('/api', '') || 'http://backend:80'}/api/auth/oauth/callback`;
    
    console.log('[OAuth Google Callback] Forwarding to backend:', backendUrl);
    
    const response = await fetch(backendUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        code,
        state,
      }),
    });

    console.log('[OAuth Google Callback] Backend response status:', response.status);

    if (!response.ok) {
      const errorText = await response.text();
      console.error('[OAuth Google Callback] Backend error:', response.status, errorText);
      return NextResponse.redirect(
        new URL(`/login?error=backend_error`, request.url)
      );
    }

    // Get response data to check if user was created/updated
    const responseData = await response.json();
    console.log('[OAuth Google Callback] Backend response data:', responseData);

    // Backend should set httpOnly cookies and return user data
    // Redirect to dashboard on success
    return NextResponse.redirect(
      new URL('/dashboard', request.url)
    );

  } catch (error) {
    console.error('[OAuth Google Callback] Exception:', error);
    return NextResponse.redirect(
      new URL('/login?error=callback_exception', request.url)
    );
  }
}
