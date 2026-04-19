'use client';

import { useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { motion } from 'framer-motion';
import { Menu, X, User, LogOut, AlertTriangle, Crown, Sparkles } from 'lucide-react';
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

  // Don't show premium styling if loading or not authenticated
  const showPremium = isAuthenticated && hasPremium && !loading;

  return (
    <motion.nav
      initial={{ y: -100 }}
      animate={{ y: 0 }}
      className='fixed top-0 left-0 right-0 z-50 bg-black/50 backdrop-blur-xl border-b border-white/10'
    >
      <div className='container mx-auto px-6'>
        <div className='flex items-center justify-between h-16'>
          {/* Logo */}
          <motion.div whileHover={{ scale: 1.05 }}>
            <Link href='/' className='flex items-center gap-2'>
              <motion.div
                animate={{ rotate: [0, 360] }}
                transition={{ duration: 20, repeat: Infinity, ease: 'linear' }}
                className='w-8 h-8 bg-gradient-to-br from-blue-600 to-purple-600 rounded-lg flex items-center justify-center shadow-lg shadow-purple-500/30'
              >
                <Sparkles className='w-5 h-5 text-white' />
              </motion.div>
              <span className='text-xl font-bold text-white bg-clip-text text-transparent bg-gradient-to-r from-blue-400 to-purple-400'>
                ServerEye
              </span>
            </Link>
          </motion.div>

          {/* Desktop Navigation */}
          <div className='hidden md:flex items-center gap-8'>
            {links.map((link, _i) => (
              <motion.div key={link.href} whileHover={{ scale: 1.05 }} whileTap={{ scale: 0.95 }}>
                <Link
                  href={link.href}
                  className={cn(
                    'text-sm font-medium transition-colors relative',
                    pathname === link.href ? 'text-white' : 'text-gray-400 hover:text-white',
                  )}
                >
                  {link.label}
                  {pathname === link.href && (
                    <motion.div
                      layoutId='activeLink'
                      className='absolute -bottom-2 left-0 right-0 h-0.5 bg-gradient-to-r from-purple-500 to-pink-500'
                      initial={false}
                      transition={{ duration: 0.3 }}
                    />
                  )}
                </Link>
              </motion.div>
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
          <motion.button
            whileTap={{ scale: 0.9 }}
            className='md:hidden text-white'
            onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
          >
            {isMobileMenuOpen ? <X className='w-6 h-6' /> : <Menu className='w-6 h-6' />}
          </motion.button>
        </div>

        {/* Mobile Menu */}
        {isMobileMenuOpen && (
          <motion.div
            initial={{ opacity: 0, y: -20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
            className='md:hidden py-4 border-t border-white/10'
          >
            <div className='flex flex-col gap-4'>
              {links.map((link, i) => (
                <motion.div
                  key={link.href}
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  transition={{ delay: i * 0.05 }}
                >
                  <Link
                    href={link.href}
                    className={cn(
                      'text-sm font-medium transition-colors',
                      pathname === link.href ? 'text-white' : 'text-gray-400',
                    )}
                    onClick={() => setIsMobileMenuOpen(false)}
                  >
                    {link.label}
                  </Link>
                </motion.div>
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
          </motion.div>
        )}
      </div>
    </motion.nav>
  );
}
