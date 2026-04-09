import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

export function middleware(request: NextRequest) {
  // Add security headers
  const response = NextResponse.next();
  response.headers.set('X-DNS-Prefetch-Control', 'on');
  if (process.env.NODE_ENV === 'production') {
    response.headers.set(
      'Strict-Transport-Security',
      'max-age=63072000; includeSubDomains; preload',
    );
  }
  response.headers.set('X-XSS-Protection', '1; mode=block');
  response.headers.set('X-Frame-Options', 'DENY');
  response.headers.set('X-Content-Type-Options', 'nosniff');
  response.headers.set('Referrer-Policy', 'origin-when-cross-origin');
  response.headers.set('Permissions-Policy', 'camera=(), microphone=(), geolocation=()');

  // Set CSP based on environment
  const isDevelopment = process.env.NODE_ENV === 'development';
  if (isDevelopment) {
    // Allow localhost and backend container connections in development
    response.headers.set(
      'Content-Security-Policy',
      "default-src 'self' 'unsafe-inline' 'unsafe-eval'; connect-src 'self' ws: wss: http://localhost:* https://localhost:* http://127.0.0.1:* https://127.0.0.1:* http://backend:* https: https://telegram.org; script-src 'self' 'unsafe-inline' 'unsafe-eval' https://telegram.org; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob: https:; font-src 'self' data:; frame-src 'self' https://telegram.org; frame-ancestors 'none';",
    );
  } else {
    // Stricter CSP for production
    response.headers.set(
      'Content-Security-Policy',
      "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self' https:; frame-ancestors 'none';",
    );
  }
  response.headers.set('Server', '');

  // Skip middleware for static files and API routes
  if (
    request.nextUrl.pathname.startsWith('/_next') ||
    request.nextUrl.pathname.startsWith('/static') ||
    request.nextUrl.pathname.startsWith('/api') ||
    request.nextUrl.pathname.startsWith('/debug')
  ) {
    return response;
  }

  // Check for auth tokens in cookies
  const accessToken = request.cookies.get('access_token')?.value;

  // Protected routes that require authentication
  const protectedRoutes = ['/dashboard', '/profile', '/servers', '/admin'];
  const isProtectedRoute = protectedRoutes.some(route =>
    request.nextUrl.pathname.startsWith(route),
  );

  // Auth routes that should redirect to dashboard if authenticated
  const authRoutes = ['/login', '/register'];
  const isAuthRoute = authRoutes.some(route => request.nextUrl.pathname.startsWith(route));

  // If trying to access protected route without token, redirect to login
  if (isProtectedRoute && !accessToken) {
    return NextResponse.redirect(new URL('/login', request.url));
  }

  // Check admin routes specifically
  if (request.nextUrl.pathname.startsWith('/admin')) {
    if (!accessToken) {
      const loginUrl = new URL('/login', request.url);
      loginUrl.searchParams.set('callbackUrl', request.nextUrl.pathname);
      return NextResponse.redirect(loginUrl);
    }

    try {
      // Decode JWT to check role
      const tokenParts = accessToken.split('.');
      if (tokenParts.length !== 3) {
        return NextResponse.redirect(new URL('/login', request.url));
      }

      const payload = JSON.parse(atob(tokenParts[1]));
      const userRole =
        payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

      // Check if user is admin
      const isAdmin = String(userRole).toLowerCase() === 'admin';

      if (!isAdmin) {
        // Не-admin пользователи не могут получить доступ к admin роутам
        return NextResponse.redirect(new URL('/dashboard', request.url));
      }
    } catch (error) {
      // For non-admin routes, continue without admin check
      if (!request.nextUrl.pathname.startsWith('/admin')) {
        return response;
      }
      return NextResponse.redirect(new URL('/login', request.url));
    }
  }

  // Check for OAuth linking parameters
  const url = request.nextUrl;
  const linkingParam = url.searchParams.get('linking');
  const code = url.searchParams.get('code');
  const state = url.searchParams.get('state');

  // Check for OAuth callback with linking state
  if (code && state && state.startsWith('linking_')) {
    // Redirect to intercept page for processing
    const interceptUrl = new URL('/oauth/intercept', request.url);
    interceptUrl.searchParams.set('code', code);
    interceptUrl.searchParams.set('state', state);

    return NextResponse.redirect(interceptUrl);
  }

  // Check for redirect to oauth/intercept (from backend callback)
  if (url.pathname === '/oauth/intercept' && code && state) {
    // Let the request continue to oauth/intercept page
    return NextResponse.next();
  }

  if (linkingParam === 'true' && code && state) {
    // Redirect to intercept page for processing
    const interceptUrl = new URL('/oauth/intercept', request.url);
    interceptUrl.searchParams.set('code', code);
    interceptUrl.searchParams.set('state', state);

    return NextResponse.redirect(interceptUrl);
  }

  // If trying to access auth route with token, redirect to dashboard
  if (isAuthRoute && accessToken) {
    return NextResponse.redirect(new URL('/dashboard', request.url));
  }

  return response;
}

export const config = {
  matcher: [
    /*
     * Match all request paths except for the ones starting with:
     * - api (API routes)
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon.ico (favicon file)
     */
    '/((?!api|_next/static|_next/image|favicon.ico).*)',
  ],
};
