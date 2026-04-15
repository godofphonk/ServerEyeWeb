import { apiClient } from '@/lib/api';
import {
  VerifyEmailRequest,
  ResendVerificationRequest,
  ForgotPasswordRequest,
  ResetPasswordRequest,
  ChangeEmailRequest,
  ConfirmEmailChangeRequest,
  RequestAccountDeletionRequest,
  ConfirmAccountDeletionRequest,
} from '@/types';

export const authApi = {
  // Email verification
  async verifyEmail(data: VerifyEmailRequest) {
    return apiClient.post('/auth/verify-email', data);
  },

  // Email verification without authorization (for registration)
  async verifyEmailWithoutAuth(data: VerifyEmailRequest) {
    // Используем отдельный API route без авторизации
    const response = await fetch('/api/users/verify-email', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: 'Failed to verify email' }));
      throw new Error(errorData.message || 'Failed to verify email');
    }

    return response.json();
  },

  async resendVerification(data: ResendVerificationRequest) {
    return apiClient.post('/auth/resend-verification', data);
  },

  async resendVerificationWithoutAuth(data: ResendVerificationRequest) {
    // Используем отдельный API route без авторизации
    const response = await fetch('/api/auth/resend-verification', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(data),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ message: 'Failed to resend code' }));
      throw new Error(errorData.message || 'Failed to resend code');
    }

    return response.json();
  },

  // Password reset
  async forgotPassword(data: ForgotPasswordRequest) {
    return apiClient.post('/auth/forgot-password', data);
  },

  async resetPassword(data: ResetPasswordRequest) {
    return apiClient.post('/auth/reset-password', data);
  },

  // Email change
  async changeEmail(data: ChangeEmailRequest) {
    return apiClient.post('/auth/change-email', data);
  },

  async confirmEmailChange(data: ConfirmEmailChangeRequest) {
    return apiClient.post('/auth/confirm-email-change', data);
  },

  // Account deletion
  async requestAccountDeletion(data: RequestAccountDeletionRequest) {
    return apiClient.post('/auth/request-account-deletion', data);
  },

  async confirmAccountDeletion(data: ConfirmAccountDeletionRequest) {
    return apiClient.post('/auth/confirm-account-deletion', data);
  },

  // Direct account deletion for OAuth users without email
  async deleteAccountDirect() {
    const response = await apiClient.post('/auth/delete-account-direct', {});

    // Clear cookies on successful deletion
    if (typeof window !== 'undefined') {
      document.cookie.split(';').forEach(c => {
        document.cookie = c
          .replace(/^ +/, '')
          .replace(/=.*/, '=;expires=' + new Date().toUTCString() + ';path=/');
      });
    }

    return response;
  },
};
