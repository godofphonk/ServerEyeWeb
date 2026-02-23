import { NextRequest, NextResponse } from 'next/server';

console.log('Simple test route loaded!');

export async function GET(request: NextRequest) {
  console.log('Simple test route called!');
  return NextResponse.json({ message: 'Simple test works!', timestamp: new Date().toISOString() });
}
