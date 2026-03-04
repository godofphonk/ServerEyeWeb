import { NextRequest, NextResponse } from 'next/server';

export async function GET(request: NextRequest) {
  const searchParams = request.nextUrl.searchParams;
  const code = searchParams.get('code');
  const state = searchParams.get('state');
  const error = searchParams.get('error');

  // Get stored OAuth data from session
  const returnUrl = request.cookies.get('oauth_return_url')?.value || '/dashboard';
  const action = request.cookies.get('oauth_action')?.value || 'login';

  // Clear OAuth cookies
  const response = NextResponse.next();
  response.cookies.delete('oauth_return_url');
  response.cookies.delete('oauth_action');

  // Handle OAuth errors
  if (error) {
    console.error('[OAuth Link Callback] Error:', error);
    return NextResponse.redirect(
      new URL(`${returnUrl}?error=${encodeURIComponent(error)}`, request.url)
    );
  }

  // Validate required parameters
  if (!code || !state) {
    console.error('[OAuth Link Callback] Missing required parameters:', { code, state });
    return NextResponse.redirect(
      new URL(`${returnUrl}?error=missing_parameters`, request.url)
    );
  }

  try {
    // Forward the callback to the backend as POST (backend expects POST, not GET)
    const backendUrl = `${process.env.NEXT_PUBLIC_API_BASE_URL?.replace('/api', '') || 'http://localhost:5246'}/api/auth/oauth/callback`;
    
    console.log('[OAuth Link Callback] Forwarding to backend:', backendUrl);
    
    const backendResponse = await fetch(backendUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Cookie': request.headers.get('cookie') || '',
      },
      body: JSON.stringify({
        code,
        state,
      }),
    });

    console.log('[OAuth Link Callback] Backend response status:', backendResponse.status);

    if (!backendResponse.ok) {
      const errorText = await backendResponse.text();
      console.error('[OAuth Link Callback] Backend error:', backendResponse.status, errorText);
      return NextResponse.redirect(
        new URL(`${returnUrl}?error=backend_error`, request.url)
      );
    }

    // Get response data to check if account was linked
    const responseData = await backendResponse.json();
    console.log('[OAuth Link Callback] Backend response data:', responseData);

    // Backend should set httpOnly cookies and return user data
    // Redirect to return URL on success
    return NextResponse.redirect(
      new URL(returnUrl, request.url)
    );

  } catch (error) {
    console.error('[OAuth Link Callback] Exception:', error);
    return NextResponse.redirect(
      new URL(`${returnUrl}?error=callback_exception`, request.url)
    );
  }
}
