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
    return apiClient.post('/users/verify-email', data);
  },

  async resendVerification(data: ResendVerificationRequest) {
    return apiClient.post('/auth/resend-verification', data);
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
