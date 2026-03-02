import { NextRequest, NextResponse } from 'next/server';

export function adminAuthMiddleware(request: NextRequest) {
  // Get token from cookies
  const token = request.cookies.get('access_token')?.value;

  if (!token) {
    return NextResponse.redirect(new URL('/login', request.url));
  }

  // Decode JWT to check role (basic check - real validation on backend)
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));

    // Check if user is admin
    if (payload.role !== 'admin') {
      return NextResponse.redirect(new URL('/dashboard', request.url));
    }

    // Admin authorized
    return NextResponse.next();
  } catch (error) {
    return NextResponse.redirect(new URL('/login', request.url));
  }
}
