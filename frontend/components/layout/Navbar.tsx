'use client';

import { useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { Menu, X, User, LogOut, AlertTriangle, Crown } from 'lucide-react';
import { useAuth } from '@/context/AuthContext';
import { Button } from '@/components/ui/Button';
import { cn } from '@/lib/utils';
import { isAdmin } from '@/lib/auth';
import { useSubscription } from '@/hooks/useSubscription';

const publicLinks = [
  { href: '/', label: 'Home' },
  { href: '/docs', label: 'Docs' },
  { href: '/pricing', label: 'Pricing' },
  { href: '/install', label: 'Install' },
  { href: '/support', label: 'Support' },
];

const privateLinks = [
  { href: '/dashboard', label: 'Dashboard' },
  { href: '/docs', label: 'Docs' },
  { href: '/pricing', label: 'Pricing' },
  { href: '/install', label: 'Install' },
  { href: '/support', label: 'Support' },
];

export function Navbar() {
  const pathname = usePathname();
  const { user, isAuthenticated, logout, isEmailVerified } = useAuth();
  const { hasPremium, loading } = useSubscription();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const links = isAuthenticated ? privateLinks : publicLinks;

  const handleLogout = async () => {
    await logout();
    // Force page reload to ensure all state is cleared
    window.location.reload();
  };

  // Hide navbar on auth pages
  if (pathname === '/login' || pathname === '/register') {
    return null;
  }

  // Don't show premium styling if loading or not authenticated
  const showPremium = isAuthenticated && hasPremium && !loading;

  return (
    <nav className='fixed top-0 left-0 right-0 z-50 bg-black/50 backdrop-blur-xl border-b border-white/10'>
      <div className='container mx-auto px-6'>
        <div className='flex items-center justify-between h-16'>
          {/* Logo */}
          <Link href='/' className='flex items-center gap-2'>
            <div className='w-8 h-8 bg-gradient-to-br from-blue-600 to-purple-600 rounded-lg' />
            <span className='text-xl font-bold text-white'>ServerEye</span>
          </Link>

          {/* Desktop Navigation */}
          <div className='hidden md:flex items-center gap-8'>
            {links.map(link => (
              <Link
                key={link.href}
                href={link.href}
                className={cn(
                  'text-sm font-medium transition-colors',
                  pathname === link.href ? 'text-white' : 'text-gray-400 hover:text-white',
                )}
              >
                {link.label}
              </Link>
            ))}
          </div>

          {/* Desktop Auth Buttons */}
          <div className='hidden md:flex items-center gap-4'>
            {isAuthenticated ? (
              <>
                {/* <NotificationBell /> */}
                <Link href='/profile'>
                  <Button
                    variant='ghost'
                    size='sm'
                    className={cn(
                      'relative',
                      showPremium && [
                        'bg-gradient-to-r from-purple-500/20 to-blue-500/20',
                        'border border-purple-500/30',
                        'shadow-lg shadow-purple-500/10',
                        'hover:from-purple-500/30 hover:to-blue-500/30',
                        'hover:border-purple-500/50',
                        'hover:shadow-purple-500/20',
                      ],
                    )}
                  >
                    <User className='w-4 h-4 mr-2' />
                    {user?.username}
                    {showPremium && <Crown className='w-3 h-3 ml-1 text-yellow-400' />}
                    {!isEmailVerified && (
                      <div className='absolute -top-1 -right-1 w-3 h-3 bg-yellow-500 rounded-full flex items-center justify-center'>
                        <AlertTriangle className='w-2 h-2 text-black' />
                      </div>
                    )}
                  </Button>
                </Link>
                {isAdmin(user) && (
                  <Link href='/admin/monitoring'>
                    <Button variant='secondary' size='sm'>
                      Admin
                    </Button>
                  </Link>
                )}
                <Button variant='ghost' size='sm' onClick={handleLogout}>
                  <LogOut className='w-4 h-4 mr-2' />
                  Logout
                </Button>
              </>
            ) : (
              <>
                <Link href='/login'>
                  <Button variant='ghost' size='sm'>
                    Sign In
                  </Button>
                </Link>
                <Link href='/register'>
                  <Button size='sm'>Get Started</Button>
                </Link>
              </>
            )}
          </div>

          {/* Mobile Menu Button */}
          <button
            className='md:hidden text-white'
            onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
          >
            {isMobileMenuOpen ? <X className='w-6 h-6' /> : <Menu className='w-6 h-6' />}
          </button>
        </div>

        {/* Mobile Menu */}
        {isMobileMenuOpen && (
          <div className='md:hidden py-4 border-t border-white/10'>
            <div className='flex flex-col gap-4'>
              {links.map(link => (
                <Link
                  key={link.href}
                  href={link.href}
                  className={cn(
                    'text-sm font-medium transition-colors',
                    pathname === link.href ? 'text-white' : 'text-gray-400',
                  )}
                  onClick={() => setIsMobileMenuOpen(false)}
                >
                  {link.label}
                </Link>
              ))}

              <div className='pt-4 border-t border-white/10 flex flex-col gap-2'>
                {isAuthenticated ? (
                  <>
                    <Link href='/profile' onClick={() => setIsMobileMenuOpen(false)}>
                      <Button
                        variant='ghost'
                        size='sm'
                        fullWidth
                        className={cn(
                          'relative',
                          showPremium && [
                            'bg-gradient-to-r from-purple-500/20 to-blue-500/20',
                            'border border-purple-500/30',
                            'shadow-lg shadow-purple-500/10',
                            'hover:from-purple-500/30 hover:to-blue-500/30',
                            'hover:border-purple-500/50',
                            'hover:shadow-purple-500/20',
                          ],
                        )}
                      >
                        <User className='w-4 h-4 mr-2' />
                        {user?.username}
                        {showPremium && <Crown className='w-3 h-3 ml-1 text-yellow-400' />}
                      </Button>
                    </Link>
                    {isAdmin(user) && (
                      <Link href='/admin/monitoring' onClick={() => setIsMobileMenuOpen(false)}>
                        <Button variant='secondary' size='sm' fullWidth>
                          Admin Panel
                        </Button>
                      </Link>
                    )}
                    <Button variant='ghost' size='sm' fullWidth onClick={handleLogout}>
                      <LogOut className='w-4 h-4 mr-2' />
                      Logout
                    </Button>
                  </>
                ) : (
                  <>
                    <Link href='/login' onClick={() => setIsMobileMenuOpen(false)}>
                      <Button variant='ghost' size='sm' fullWidth>
                        Sign In
                      </Button>
                    </Link>
                    <Link href='/register' onClick={() => setIsMobileMenuOpen(false)}>
                      <Button size='sm' fullWidth>
                        Get Started
                      </Button>
                    </Link>
                  </>
                )}
              </div>
            </div>
          </div>
        )}
      </div>
    </nav>
  );
}
