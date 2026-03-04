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

  // Check if this is a linking request by passing code and state to intercept page
  // The intercept page will check sessionStorage and decide whether to link or login
  const interceptUrl = new URL('/oauth/intercept', request.url);
  interceptUrl.searchParams.set('code', code);
  interceptUrl.searchParams.set('state', state);
  
  console.log('[OAuth Universal Callback] Redirecting to intercept page for processing');
  return NextResponse.redirect(interceptUrl);
}
