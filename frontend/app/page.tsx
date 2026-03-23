'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import Hero from '@/components/sections/Hero';
import Visualization from '@/components/sections/Visualization';
import PainPoint from '@/components/sections/PainPoint';
import HowItWorks from '@/components/sections/HowItWorks';
import Security from '@/components/sections/Security';
import OpenSource from '@/components/sections/OpenSource';
import Testimonials from '@/components/sections/Testimonials';
import Footer from '@/components/sections/Footer';

// Optimized Telegram callback handler
function TelegramCallbackHandler() {
  useEffect(() => {
    // Check if this is a Telegram OAuth callback
    const urlHash = typeof window !== 'undefined' ? window.location.hash : '';
    
    if (urlHash?.includes('tgAuthResult=')) {
      window.location.href = `/telegram-callback${urlHash}`;
    }
  }, []);

  return null;
}

export default function Home() {
  return (
    <>
      <TelegramCallbackHandler />
      <main className='min-h-screen bg-black text-white'>
        <Hero />
        <Visualization />
        <PainPoint />
        <HowItWorks />
        <Security />
        <OpenSource />
        <Testimonials />
        <Footer />
      </main>
    </>
  );
}
