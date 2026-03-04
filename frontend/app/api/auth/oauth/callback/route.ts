import { NextRequest, NextResponse } from 'next/server';

export async function GET(request: NextRequest) {
  const searchParams = request.nextUrl.searchParams;
  const code = searchParams.get('code');
  const state = searchParams.get('state');
  const error = searchParams.get('error');

  console.log('[OAuth Universal Callback] Received request:', { code: !!code, state: !!state, error });

  // Handle OAuth errors
  if (error) {
    console.error('[OAuth Universal Callback] Error:', error);
    return NextResponse.redirect(
      new URL(`/login?error=${encodeURIComponent(error)}`, request.url)
    );
  }

  // Validate required parameters
  if (!code || !state) {
    console.error('[OAuth Universal Callback] Missing required parameters:', { code, state });
    return NextResponse.redirect(
      new URL('/login?error=missing_parameters', request.url)
    );
  }

  try {
    // Forward the callback to the backend as POST (backend expects POST, not GET)
    const backendUrl = `${process.env.NEXT_PUBLIC_API_BASE_URL?.replace('/api', '') || 'http://localhost:5246'}/api/auth/oauth/callback`;
    
    console.log('[OAuth Universal Callback] Forwarding to backend:', backendUrl);
    
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

    console.log('[OAuth Universal Callback] Backend response status:', response.status);

    if (!response.ok) {
      const errorText = await response.text();
      console.error('[OAuth Universal Callback] Backend error:', response.status, errorText);
      return NextResponse.redirect(
        new URL('/auth?error=oauth_failed', request.url)
      );
    }

    // Get response data and cookies from backend
    const responseData = await response.json();
    console.log('[OAuth Universal Callback] Backend response data:', responseData);

    // Extract tokens from response data
    const { token, refreshToken, provider } = responseData;
    
    if (!token || !refreshToken || !provider) {
      console.error('[OAuth Universal Callback] Missing tokens in response:', responseData);
      return NextResponse.redirect(
        new URL('/login?error=missing_tokens', request.url)
      );
    }

    // Redirect to callback page with tokens
    const callbackUrl = new URL('/auth/callback', request.url);
    callbackUrl.searchParams.set('token', token);
    callbackUrl.searchParams.set('refreshToken', refreshToken);
    callbackUrl.searchParams.set('provider', provider);
    
    console.log('[OAuth Universal Callback] Redirecting to callback page with tokens');
    
    // Create response and copy cookies from backend response
    const frontendResponse = NextResponse.redirect(callbackUrl);

    // Copy Set-Cookie headers from backend response
    const setCookieHeaders = response.headers.get('set-cookie');
    if (setCookieHeaders) {
      console.log('[OAuth Universal Callback] Copying cookies from backend');
      frontendResponse.headers.set('Set-Cookie', setCookieHeaders);
    } else {
      console.log('[OAuth Universal Callback] No cookies received from backend');
    }

    return frontendResponse;

  } catch (error) {
    console.error('[OAuth Universal Callback] Exception:', error);
    return NextResponse.redirect(
      new URL('/login?error=callback_exception', request.url)
    );
  }
}
