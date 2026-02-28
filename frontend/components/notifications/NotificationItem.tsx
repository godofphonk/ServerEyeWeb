'use client';

import { Notification, NotificationType } from '@/types';
import { Bell, MessageSquare, RefreshCw } from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';
import { ru } from 'date-fns/locale';

interface NotificationItemProps {
  notification: Notification;
  onClick: (notification: Notification) => void;
}

const getNotificationIcon = (type: NotificationType) => {
  switch (type) {
    case NotificationType.TicketCreated:
      return <Bell className='w-5 h-5 text-blue-400' />;
    case NotificationType.NewMessage:
      return <MessageSquare className='w-5 h-5 text-green-400' />;
    case NotificationType.StatusChanged:
      return <RefreshCw className='w-5 h-5 text-orange-400' />;
    default:
      return <Bell className='w-5 h-5 text-gray-400' />;
  }
};

const getNotificationBg = (type: NotificationType) => {
  switch (type) {
    case NotificationType.TicketCreated:
      return 'bg-blue-500/10';
    case NotificationType.NewMessage:
      return 'bg-green-500/10';
    case NotificationType.StatusChanged:
      return 'bg-orange-500/10';
    default:
      return 'bg-gray-500/10';
  }
};

export function NotificationItem({ notification, onClick }: NotificationItemProps) {
  const timeAgo = formatDistanceToNow(new Date(notification.createdAt), {
    addSuffix: true,
    locale: ru,
  });

  return (
    <div
      onClick={() => onClick(notification)}
      className={`
        p-4 cursor-pointer transition-all hover:bg-gray-800/50
        border-l-2 ${notification.isRead ? 'border-transparent' : 'border-blue-500'}
        ${notification.isRead ? 'opacity-60' : ''}
      `}
    >
      <div className='flex gap-3'>
        <div
          className={`flex-shrink-0 w-10 h-10 rounded-full ${getNotificationBg(notification.type)} flex items-center justify-center`}
        >
          {getNotificationIcon(notification.type)}
        </div>

        <div className='flex-1 min-w-0'>
          <div className='flex items-start justify-between gap-2'>
            <p
              className={`text-sm font-medium ${notification.isRead ? 'text-gray-300' : 'text-white'}`}
            >
              {notification.title}
            </p>
            {!notification.isRead && (
              <div className='flex-shrink-0 w-2 h-2 bg-blue-500 rounded-full mt-1' />
            )}
          </div>

          <p className='text-sm text-gray-400 mt-1 line-clamp-2'>{notification.message}</p>

          <p className='text-xs text-gray-500 mt-2'>{timeAgo}</p>
        </div>
      </div>
    </div>
  );
}
