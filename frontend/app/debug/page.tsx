'use client';

import React, { useEffect, useState } from 'react';

export default function DebugPage() {
  const [cookies, setCookies] = useState<string>('');
  const [localStorage, setLocalStorage] = useState<string>('');

  useEffect(() => {
    // Get all cookies
    const allCookies = document.cookie
      .split(';')
      .map(c => c.trim())
      .join('\n');
    setCookies(allCookies);

    // Get localStorage items
    const storageItems: string[] = [];
    if (typeof window !== 'undefined') {
      for (let i = 0; i < window.localStorage.length; i++) {
        const key = window.localStorage.key(i);
        if (key) {
          storageItems.push(`${key}: ${window.localStorage.getItem(key)}`);
        }
      }
    }
    setLocalStorage(storageItems.join('\n'));
  }, []);

  return (
    <div className='container mx-auto p-8'>
      <h1 className='text-2xl font-bold mb-4'>Debug Authentication</h1>

      <div className='grid grid-cols-1 md:grid-cols-2 gap-4'>
        <div className='bg-gray-100 p-4 rounded'>
          <h2 className='font-semibold mb-2'>Cookies:</h2>
          <pre className='text-sm whitespace-pre-wrap'>{cookies || 'No cookies found'}</pre>
        </div>

        <div className='bg-gray-100 p-4 rounded'>
          <h2 className='font-semibold mb-2'>LocalStorage:</h2>
          <pre className='text-sm whitespace-pre-wrap'>
            {localStorage || 'No localStorage items found'}
          </pre>
        </div>
      </div>

      <div className='mt-4'>
        <h2 className='font-semibold mb-2'>Current URL:</h2>
        <pre>{typeof window !== 'undefined' ? window.location.href : 'N/A'}</pre>
      </div>
    </div>
  );
}
