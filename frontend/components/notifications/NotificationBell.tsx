'use client';

import { useEffect, useState, useRef } from 'react';
import { Bell } from 'lucide-react';
import { notificationApi } from '@/lib/notificationApi';
import { NotificationDropdown } from './NotificationDropdown';

export function NotificationBell() {
  const [unreadCount, setUnreadCount] = useState(0);
  const [isOpen, setIsOpen] = useState(false);
  const bellRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    loadUnreadCount();
  }, []);

  const loadUnreadCount = async () => {
    try {
      const response = await notificationApi.getUnreadCount();
      setUnreadCount(response.count);
    } catch (error) {}
  };

  const handleNotificationRead = () => {
    loadUnreadCount();
  };

  const toggleDropdown = () => {
    setIsOpen(!isOpen);
  };

  return (
    <div className='relative'>
      <button
        ref={bellRef}
        onClick={toggleDropdown}
        className='relative p-2 text-gray-400 hover:text-white transition-colors rounded-lg hover:bg-white/5'
        aria-label='Notifications'
      >
        <Bell className='w-5 h-5' />

        {unreadCount > 0 && (
          <span className='absolute -top-1 -right-1 flex items-center justify-center min-w-[20px] h-5 px-1.5 text-xs font-bold text-white bg-red-500 rounded-full'>
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      <NotificationDropdown
        isOpen={isOpen}
        onClose={() => setIsOpen(false)}
        onNotificationRead={handleNotificationRead}
      />
    </div>
  );
}
