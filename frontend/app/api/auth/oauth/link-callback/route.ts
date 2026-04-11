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
    return NextResponse.redirect(
      new URL(`${returnUrl}?error=${encodeURIComponent(error)}`, request.url),
    );
  }

  // Validate required parameters
  if (!code || !state) {
    return NextResponse.redirect(new URL(`${returnUrl}?error=missing_parameters`, request.url));
  }

  try {
    // Get JWT token from cookies or localStorage
    const jwtToken =
      request.cookies.get('jwt_token')?.value || request.cookies.get('access_token')?.value;

    // Forward the callback to the backend as POST (backend expects POST, not GET)
    const backendUrl = `${process.env.INTERNAL_API_URL || 'http://backend:8080/api'}/auth/oauth/callback`;

    const headers: HeadersInit = {
      'Content-Type': 'application/json',
      Cookie: request.headers.get('cookie') || '',
    };

    // Add Authorization header if JWT token is available (for linking)
    if (jwtToken && action === 'link') {
      headers['Authorization'] = `Bearer ${jwtToken}`;
    }

    const backendResponse = await fetch(backendUrl, {
      method: 'POST',
      headers,
      body: JSON.stringify({
        code,
        state,
      }),
    });

    if (!backendResponse.ok) {
      return NextResponse.redirect(new URL(`${returnUrl}?error=backend_error`, request.url));
    }

    // Get response data to check if account was linked
    await backendResponse.json();

    // Backend should set httpOnly cookies and return user data
    // Redirect to return URL on success
    return NextResponse.redirect(new URL(returnUrl, request.url));
  } catch (_error) {
    return NextResponse.redirect(new URL(`${returnUrl}?error=callback_exception`, request.url));
  }
}
