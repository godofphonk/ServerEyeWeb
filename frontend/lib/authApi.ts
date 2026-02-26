import { apiClient } from './api';
import { 
  VerifyEmailRequest, 
  ResendVerificationRequest, 
  ForgotPasswordRequest, 
  ResetPasswordRequest,
  ChangeEmailRequest,
  ConfirmEmailChangeRequest,
  RequestAccountDeletionRequest,
  ConfirmAccountDeletionRequest
} from '@/types';

export const authApi = {
  // Email verification
  async verifyEmail(data: VerifyEmailRequest) {
    return apiClient.post('/auth/verify-email', data);
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
  }
};
