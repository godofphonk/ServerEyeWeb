import { NextRequest, NextResponse } from 'next/server';

export async function GET(request: NextRequest) {
  try {
    const accessToken = request.cookies.get('accessToken')?.value;

    if (!accessToken) {
      return NextResponse.json({ error: 'No access token found' }, { status: 401 });
    }

    return NextResponse.json({ token: accessToken });
  } catch (error) {
    console.error('Token API route error:', error);
    return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
  }
}
