'use client';

import { Notification, NotificationType } from '@/types';
import { motion } from 'framer-motion';
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
      return 'bg-blue-500/10 border-blue-500/30';
    case NotificationType.NewMessage:
      return 'bg-green-500/10 border-green-500/30';
    case NotificationType.StatusChanged:
      return 'bg-orange-500/10 border-orange-500/30';
    default:
      return 'bg-gray-500/10 border-gray-500/30';
  }
};

export function NotificationItem({ notification, onClick }: NotificationItemProps) {
  const timeAgo = formatDistanceToNow(new Date(notification.createdAt), {
    addSuffix: true,
    locale: ru,
  });

  return (
    <motion.div
      whileHover={{ scale: 1.02, x: 5 }}
      whileTap={{ scale: 0.98 }}
      onClick={() => onClick(notification)}
      className={`
        p-4 cursor-pointer transition-all hover:bg-gray-800/50
        border-l-2 ${notification.isRead ? 'border-transparent' : 'border-blue-500'}
        ${notification.isRead ? 'opacity-60' : ''}
      `}
    >
      <div className='flex gap-3'>
        <motion.div
          whileHover={{ rotate: 360 }}
          transition={{ duration: 0.5 }}
          className={`flex-shrink-0 w-10 h-10 rounded-full ${getNotificationBg(notification.type)} flex items-center justify-center border`}
        >
          {getNotificationIcon(notification.type)}
        </motion.div>

        <div className='flex-1 min-w-0'>
          <div className='flex items-start justify-between gap-2'>
            <p
              className={`text-sm font-medium ${notification.isRead ? 'text-gray-300' : 'text-white'}`}
            >
              {notification.title}
            </p>
            {!notification.isRead && (
              <motion.div
                animate={{ scale: [1, 1.2, 1] }}
                transition={{ duration: 2, repeat: Infinity, ease: 'easeInOut' }}
                className='flex-shrink-0 w-2 h-2 bg-blue-500 rounded-full mt-1'
              />
            )}
          </div>

          <p className='text-sm text-gray-400 mt-1 line-clamp-2'>{notification.message}</p>

          <p className='text-xs text-gray-500 mt-2'>{timeAgo}</p>
        </div>
      </div>
    </motion.div>
  );
}
