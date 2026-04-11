'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { Notification, AxiosApiError } from '@/types';
import { notificationApi } from '@/lib/notificationApi';
import { NotificationItem } from './NotificationItem';
import { Loader2, CheckCheck, Inbox } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { useToast } from '@/hooks/useToast';

interface NotificationDropdownProps {
  isOpen: boolean;
  onClose: () => void;
  onNotificationRead: () => void;
}

export function NotificationDropdown({
  isOpen,
  onClose,
  onNotificationRead,
}: NotificationDropdownProps) {
  const router = useRouter();
  const toast = useToast();
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isMarkingAllRead, setIsMarkingAllRead] = useState(false);

  useEffect(() => {
    if (isOpen) {
      loadNotifications();
    }
  }, [isOpen]);

  const loadNotifications = async () => {
    try {
      setIsLoading(true);
      const response = await notificationApi.getNotifications(1, 50);
      setNotifications(response.notifications || []);
    } catch (_error) {
      // ignore error
    } finally {
      setIsLoading(false);
    }
  };

  const handleNotificationClick = async (notification: Notification) => {
    try {
      if (!notification.isRead) {
        await notificationApi.markAsRead(notification.id);
        onNotificationRead();

        setNotifications(prev =>
          prev.map(n => (n.id === notification.id ? { ...n, isRead: true } : n)),
        );
      }

      if (notification.ticketId) {
        onClose();
        router.push(`/admin/tickets?ticketId=${notification.ticketId}`);
      }
    } catch (_error) {
      /* ignore error */
    }
  };

  const handleMarkAllAsRead = async () => {
    try {
      setIsMarkingAllRead(true);
      await notificationApi.markAllAsRead();
      onNotificationRead();

      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));

      const unreadCount = notifications.filter(n => !n.isRead).length;
      if (unreadCount > 0) {
        toast.success('All Read', `${unreadCount} notifications marked as read`);
      }
    } catch (error: unknown) {

      const errorMessage =
        (error as AxiosApiError)?.response?.data?.message || (error as AxiosApiError)?.message || 'Unknown error occurred';

      toast.error('Action Failed', `Failed to mark notifications as read: ${errorMessage}`);
    } finally {
      setIsMarkingAllRead(false);
    }
  };

  if (!isOpen) return null;

  const unreadCount = notifications.filter(n => !n.isRead).length;

  return (
    <>
      <div className='fixed inset-0 z-40' onClick={onClose} />

      <div className='absolute right-0 mt-2 w-96 bg-gray-900 border border-white/10 rounded-lg shadow-xl z-50 max-h-[600px] flex flex-col'>
        <div className='p-4 border-b border-white/10 flex items-center justify-between'>
          <div>
            <h3 className='text-lg font-semibold text-white'>Уведомления</h3>
            {unreadCount > 0 && (
              <p className='text-sm text-gray-400'>{unreadCount} непрочитанных</p>
            )}
          </div>

          {unreadCount > 0 && (
            <Button
              variant='ghost'
              size='sm'
              onClick={handleMarkAllAsRead}
              disabled={isMarkingAllRead}
              className='gap-2'
            >
              {isMarkingAllRead ? (
                <Loader2 className='w-4 h-4 animate-spin' />
              ) : (
                <CheckCheck className='w-4 h-4' />
              )}
              Прочитать все
            </Button>
          )}
        </div>

        <div className='flex-1 overflow-y-auto'>
          {isLoading ? (
            <div className='flex items-center justify-center py-12'>
              <Loader2 className='w-6 h-6 animate-spin text-blue-400' />
            </div>
          ) : notifications.length === 0 ? (
            <div className='flex flex-col items-center justify-center py-12 px-4'>
              <Inbox className='w-12 h-12 text-gray-600 mb-4' />
              <p className='text-gray-400 text-center'>Нет уведомлений</p>
            </div>
          ) : (
            <div className='divide-y divide-white/5'>
              {notifications.map(notification => (
                <NotificationItem
                  key={notification.id}
                  notification={notification}
                  onClick={handleNotificationClick}
                />
              ))}
            </div>
          )}
        </div>
      </div>
    </>
  );
}
