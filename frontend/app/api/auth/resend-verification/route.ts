import { NextRequest, NextResponse } from 'next/server';

const API_BASE_URL = process.env.INTERNAL_API_URL || 'http://backend:8080/api';

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const targetUrl = `${API_BASE_URL}/auth/resend-verification`;

    const response = await fetch(targetUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(body),
    });

    const responseData = await response.text();
    return new NextResponse(responseData, {
      status: response.status,
      headers: {
        'Content-Type': response.headers.get('Content-Type') || 'application/json',
      },
    });
  } catch (_error) {
    return NextResponse.json({ message: 'Internal server error' }, { status: 500 });
  }
}
