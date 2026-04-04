import { notificationApi } from '@/lib/notificationApi';
import { Notification, NotificationType } from '@/types';

jest.mock('@/lib/api', () => ({
  apiClient: {
    get: jest.fn(),
    post: jest.fn(),
  },
}));

const { apiClient } = require('@/lib/api');

const makeNotification = (overrides: Partial<Notification> = {}): Notification => ({
  id: 'notif-1',
  userId: 'user-1',
  type: NotificationType.TicketCreated,
  title: 'Test Notification',
  message: 'Notification message',
  ticketId: null,
  isRead: false,
  createdAt: '2024-01-01T00:00:00Z',
  ...overrides,
});

describe('notificationApi', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('getNotifications', () => {
    it('wraps an array response into NotificationResponse format', async () => {
      const notifications = [makeNotification(), makeNotification({ id: 'notif-2' })];
      apiClient.get.mockResolvedValue(notifications);

      const result = await notificationApi.getNotifications();

      expect(apiClient.get).toHaveBeenCalledWith('/notifications?page=1&pageSize=50');
      expect(result.notifications).toEqual(notifications);
      expect(result.pagination.totalCount).toBe(2);
      expect(result.pagination.hasNextPage).toBe(false);
      expect(result.pagination.hasPreviousPage).toBe(false);
    });

    it('passes through a paginated NotificationResponse unchanged', async () => {
      const paginatedResponse = {
        notifications: [makeNotification()],
        pagination: {
          page: 2,
          pageSize: 10,
          totalCount: 25,
          totalPages: 3,
          hasNextPage: true,
          hasPreviousPage: true,
        },
      };
      apiClient.get.mockResolvedValue(paginatedResponse);

      const result = await notificationApi.getNotifications(2, 10);

      expect(apiClient.get).toHaveBeenCalledWith('/notifications?page=2&pageSize=10');
      expect(result).toEqual(paginatedResponse);
    });

    it('uses default page=1 and pageSize=50 when no parameters given', async () => {
      apiClient.get.mockResolvedValue([]);

      await notificationApi.getNotifications();

      expect(apiClient.get).toHaveBeenCalledWith('/notifications?page=1&pageSize=50');
    });

    it('builds URL with custom page and pageSize parameters', async () => {
      apiClient.get.mockResolvedValue([]);

      await notificationApi.getNotifications(3, 20);

      expect(apiClient.get).toHaveBeenCalledWith('/notifications?page=3&pageSize=20');
    });

    it('returns correct pagination for empty array response', async () => {
      apiClient.get.mockResolvedValue([]);

      const result = await notificationApi.getNotifications();

      expect(result.notifications).toEqual([]);
      expect(result.pagination.totalCount).toBe(0);
      expect(result.pagination.page).toBe(1);
    });

    it('propagates errors from apiClient', async () => {
      const error = new Error('Network error');
      apiClient.get.mockRejectedValue(error);

      await expect(notificationApi.getNotifications()).rejects.toThrow('Network error');
    });
  });

  describe('getUnreadCount', () => {
    it('returns unread count from api', async () => {
      const unreadCount = { count: 5 };
      apiClient.get.mockResolvedValue(unreadCount);

      const result = await notificationApi.getUnreadCount();

      expect(apiClient.get).toHaveBeenCalledWith('/notifications/unread-count');
      expect(result).toEqual(unreadCount);
    });

    it('returns zero count when no unread notifications', async () => {
      apiClient.get.mockResolvedValue({ count: 0 });

      const result = await notificationApi.getUnreadCount();

      expect(result.count).toBe(0);
    });

    it('propagates errors from apiClient', async () => {
      apiClient.get.mockRejectedValue(new Error('Unauthorized'));

      await expect(notificationApi.getUnreadCount()).rejects.toThrow('Unauthorized');
    });
  });

  describe('markAsRead', () => {
    it('calls the correct endpoint with notification id', async () => {
      apiClient.post.mockResolvedValue(undefined);

      await notificationApi.markAsRead('notif-123');

      expect(apiClient.post).toHaveBeenCalledWith('/notifications/notif-123/mark-read');
    });

    it('uses the provided notification ID in the URL', async () => {
      apiClient.post.mockResolvedValue(undefined);

      await notificationApi.markAsRead('abc-def-456');

      expect(apiClient.post).toHaveBeenCalledWith('/notifications/abc-def-456/mark-read');
    });

    it('propagates errors from apiClient', async () => {
      apiClient.post.mockRejectedValue(new Error('Not found'));

      await expect(notificationApi.markAsRead('invalid-id')).rejects.toThrow('Not found');
    });
  });

  describe('markAllAsRead', () => {
    it('calls the mark-all-read endpoint', async () => {
      apiClient.post.mockResolvedValue(undefined);

      await notificationApi.markAllAsRead();

      expect(apiClient.post).toHaveBeenCalledWith('/notifications/mark-all-read');
    });

    it('propagates errors from apiClient', async () => {
      apiClient.post.mockRejectedValue(new Error('Server error'));

      await expect(notificationApi.markAllAsRead()).rejects.toThrow('Server error');
    });
  });
});
