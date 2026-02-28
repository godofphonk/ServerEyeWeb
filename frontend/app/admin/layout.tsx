'use client';

import { usePathname } from 'next/navigation';
import Link from 'next/link';
import { LayoutDashboard, Ticket, Activity } from 'lucide-react';

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();

  const navItems = [
    {
      href: '/admin/monitoring',
      label: 'Monitoring',
      icon: Activity,
    },
    {
      href: '/admin/tickets',
      label: 'Tickets',
      icon: Ticket,
    },
  ];

  return (
    <div className='min-h-screen bg-black'>
      {/* Admin Navigation */}
      <nav className='bg-gray-900 border-b border-white/10'>
        <div className='max-w-7xl mx-auto px-6'>
          <div className='flex items-center gap-8 h-16'>
            <div className='flex items-center gap-2 text-white font-semibold'>
              <LayoutDashboard className='w-5 h-5' />
              Admin Panel
            </div>
            <div className='flex gap-1'>
              {navItems.map(item => {
                const Icon = item.icon;
                const isActive = pathname === item.href;

                return (
                  <Link
                    key={item.href}
                    href={item.href}
                    className={`flex items-center gap-2 px-4 py-2 rounded-lg transition-colors ${
                      isActive
                        ? 'bg-blue-600 text-white'
                        : 'text-gray-400 hover:text-white hover:bg-gray-800'
                    }`}
                  >
                    <Icon className='w-4 h-4' />
                    {item.label}
                  </Link>
                );
              })}
            </div>
          </div>
        </div>
      </nav>

      {/* Page Content */}
      {children}
    </div>
  );
}
