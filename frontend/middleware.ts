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
  response.headers.set('Server', '');

  // Skip middleware for static files and API routes
  if (
    request.nextUrl.pathname.startsWith('/_next') ||
    request.nextUrl.pathname.startsWith('/api') ||
    request.nextUrl.pathname.startsWith('/static')
  ) {
    return response;
  }

  // Check for auth tokens in cookies
  const accessToken = request.cookies.get('accessToken')?.value;

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
        return NextResponse.redirect(new URL('/dashboard', request.url));
      }
    } catch (error) {
      if (process.env.NODE_ENV !== 'production') {
        console.error('Middleware - Error checking admin role:', error);
      }
      return NextResponse.redirect(new URL('/login', request.url));
    }
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
