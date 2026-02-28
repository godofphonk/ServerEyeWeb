import type { Metadata } from 'next';
import { Inter } from 'next/font/google';
import './globals.css';
import { AuthProvider } from '@/context/AuthContext';
import { ToastProvider } from '@/context/ToastContext';
import { Navbar } from '@/components/layout/Navbar';
import { ToastContainer } from '@/components/ui/ToastContainer';

const inter = Inter({ subsets: ['latin'] });

export const metadata: Metadata = {
  title: 'ServerEye - Modern Server Monitoring',
  description: 'Monitor your servers in real-time with powerful insights and alerts',
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang='en'>
      <body className={inter.className}>
        <AuthProvider>
          <ToastProvider>
            <Navbar />
            <ToastContainer />
            <div className='pt-16'>{children}</div>
          </ToastProvider>
        </AuthProvider>
      </body>
    </html>
  );
}
