import { NextRequest, NextResponse } from 'next/server';
import { cookies } from 'next/headers';

export async function DELETE(request: NextRequest) {
  try {
    // Get the token from cookies or Authorization header
    const cookieStore = await cookies();
    const token =
      cookieStore.get('access_token')?.value ||
      request.headers.get('Authorization')?.replace('Bearer ', '');

    if (!token) {
      return NextResponse.json({ message: 'No authentication token found' }, { status: 401 });
    }

    // Forward the request to the backend
    const backendUrl = process.env.INTERNAL_API_URL || 'http://backend:8080/api';

    const response = await fetch(`${backendUrl}/auth/delete-account-direct`, {
      method: 'DELETE',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      return NextResponse.json(errorData, { status: response.status });
    }

    const data = await response.json();

    // Clear authentication cookies on successful deletion
    cookieStore.delete('access_token');
    cookieStore.delete('refresh_token');

    return NextResponse.json(data, { status: 200 });
  } catch (error) {
    return NextResponse.json({ message: 'Internal server error' }, { status: 500 });
  }
}
