import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

export function middleware(request: NextRequest) {
  console.log('Middleware - request:', request.nextUrl.pathname);
  
  // Add cache-busting headers to all responses
  const response = NextResponse.next();
  response.headers.set('Cache-Control', 'no-cache, no-store, must-revalidate');
  response.headers.set('Pragma', 'no-cache');
  response.headers.set('Expires', '0');
  
  // Skip middleware for static files and API routes
  if (
    request.nextUrl.pathname.startsWith('/_next') ||
    request.nextUrl.pathname.startsWith('/api') ||
    request.nextUrl.pathname.startsWith('/static')
  ) {
    console.log('Middleware - skipping API route:', request.nextUrl.pathname);
    return response;
  }

  // Check for auth tokens in cookies
  const accessToken = request.cookies.get('accessToken')?.value;
  
  // Protected routes that require authentication
  const protectedRoutes = ['/dashboard', '/profile', '/servers', '/admin'];
  const isProtectedRoute = protectedRoutes.some(route => 
    request.nextUrl.pathname.startsWith(route)
  );

  // Auth routes that should redirect to dashboard if authenticated
  const authRoutes = ['/login', '/register'];
  const isAuthRoute = authRoutes.some(route => 
    request.nextUrl.pathname.startsWith(route)
  );

  // If trying to access protected route without token, redirect to login
  if (isProtectedRoute && !accessToken) {
    return NextResponse.redirect(new URL('/login', request.url));
  }

  // Check admin routes specifically
  if (request.nextUrl.pathname.startsWith('/admin')) {
    console.log('Middleware - Checking admin access for:', request.nextUrl.pathname);
    console.log('Middleware - Token exists:', !!accessToken);
    console.log('Middleware - Token length:', accessToken?.length);
    
    if (!accessToken) {
      console.log('Middleware - No access token found');
      const loginUrl = new URL('/login', request.url);
      loginUrl.searchParams.set('callbackUrl', request.nextUrl.pathname);
      return NextResponse.redirect(loginUrl);
    }
    
    try {
      // Decode JWT to check role
      const tokenParts = accessToken.split('.');
      if (tokenParts.length !== 3) {
        console.log('Middleware - Invalid token format');
        return NextResponse.redirect(new URL('/login', request.url));
      }
      
      const payload = JSON.parse(atob(tokenParts[1]));
      console.log('Middleware - Token payload:', payload);
      
      const userRole = payload.role || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      console.log('Middleware - User role:', userRole, typeof userRole);
      
      // Check if user is admin
      const isAdmin = String(userRole).toLowerCase() === 'admin';
      
      console.log('Middleware - Is admin:', isAdmin);

      if (!isAdmin) {
        console.log('Middleware - Access denied to admin routes. Role:', userRole);
        return NextResponse.redirect(new URL('/dashboard', request.url));
      }
      
      console.log('Middleware - Admin access granted');
    } catch (error) {
      console.error('Middleware - Error checking admin role:', error);
      return NextResponse.redirect(new URL('/login', request.url));
    }
  }

  // If trying to access auth route with token, redirect to dashboard
  if (isAuthRoute && accessToken) {
    return NextResponse.redirect(new URL('/dashboard', request.url));
  }

  return NextResponse.next();
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
