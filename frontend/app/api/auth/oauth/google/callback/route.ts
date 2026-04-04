import { NextRequest, NextResponse } from 'next/server';

export async function GET(request: NextRequest) {
  const searchParams = request.nextUrl.searchParams;
  const code = searchParams.get('code');
  const state = searchParams.get('state');
  const error = searchParams.get('error');

  // Handle OAuth errors
  if (error) {
    return NextResponse.redirect(new URL(`/login?error=${encodeURIComponent(error)}`, request.url));
  }

  // Validate required parameters
  if (!code || !state) {
    return NextResponse.redirect(new URL('/login?error=missing_parameters', request.url));
  }

  try {
    // Forward the callback to the backend as POST (backend expects POST, not GET)
    const backendUrl = `${process.env.NEXT_PUBLIC_API_BASE_URL?.replace('/api', '') || 'http://backend:80'}/api/auth/oauth/callback`;

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

    if (!response.ok) {
      const errorText = await response.text();
      return NextResponse.redirect(new URL(`/login?error=backend_error`, request.url));
    }

    // Get response data to check if user was created/updated
    const responseData = await response.json();

    // Backend should set httpOnly cookies and return user data
    // Redirect to dashboard on success
    return NextResponse.redirect(new URL('/dashboard', request.url));
  } catch (error) {
    return NextResponse.redirect(new URL('/login?error=callback_exception', request.url));
  }
}
