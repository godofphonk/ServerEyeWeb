import { apiClient } from '@/lib/api';
import { Notification, NotificationResponse, UnreadCountResponse } from '@/types';

class NotificationApi {
  private baseUrl = '/notifications';

  async getNotifications(page: number = 1, pageSize: number = 50): Promise<NotificationResponse> {
    const response = await apiClient.get<Notification[] | NotificationResponse>(
      `${this.baseUrl}?page=${page}&pageSize=${pageSize}`,
    );

    // Backend returns array directly, not paginated object
    if (Array.isArray(response)) {
      return {
        notifications: response,
        pagination: {
          page: 1,
          pageSize: response.length,
          totalCount: response.length,
          totalPages: 1,
          hasNextPage: false,
          hasPreviousPage: false,
        },
      };
    }

    return response as NotificationResponse;
  }

  async getUnreadCount(): Promise<UnreadCountResponse> {
    return apiClient.get<UnreadCountResponse>(`${this.baseUrl}/unread-count`);
  }

  async markAsRead(notificationId: string): Promise<void> {
    return apiClient.post(`${this.baseUrl}/${notificationId}/mark-read`);
  }

  async markAllAsRead(): Promise<void> {
    return apiClient.post(`${this.baseUrl}/mark-all-read`);
  }
}

export const notificationApi = new NotificationApi();
