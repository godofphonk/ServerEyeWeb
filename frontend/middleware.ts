import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

export function middleware(request: NextRequest) {
  console.log('[Middleware] === NEW REQUEST ===');
  console.log('[Middleware] URL:', request.url);
  console.log('[Middleware] Pathname:', request.nextUrl.pathname);
  
  // Add security headers
  const response = NextResponse.next();
  
  // Skip middleware for static files and API routes
  if (
    request.nextUrl.pathname.startsWith('/_next') ||
    request.nextUrl.pathname.startsWith('/static') ||
    request.nextUrl.pathname.startsWith('/api') ||
    request.nextUrl.pathname.startsWith('/debug')
  ) {
    console.log('[Middleware] Skipping static/API/debug route');
    return response;
  }

  // Check for auth tokens in cookies
  const accessToken = request.cookies.get('access_token')?.value;
  console.log('[Middleware] Access token exists:', !!accessToken);
  console.log('[Middleware] Access token length:', accessToken?.length || 0);

  // Protected routes that require authentication
  const protectedRoutes = ['/dashboard', '/profile', '/servers', '/admin'];
  const isProtectedRoute = protectedRoutes.some(route =>
    request.nextUrl.pathname.startsWith(route),
  );
  
  console.log('[Middleware] Is protected route:', isProtectedRoute);

  // If trying to access protected route without token, redirect to login
  if (isProtectedRoute && !accessToken) {
    console.log('[Middleware] No token for protected route, redirecting to login');
    return NextResponse.redirect(new URL('/login', request.url));
  }

  // For dashboard route with token, let it through (don't check JWT structure for now)
  if (request.nextUrl.pathname.startsWith('/dashboard') && accessToken) {
    console.log('[Middleware] Dashboard route with token - allowing through');
    return response;
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
