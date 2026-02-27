import axios from 'axios';
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
    // Use direct axios call to avoid automatic redirect on 401
    const baseURL = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5246/api';
    
    try {
      const token = typeof window !== 'undefined' ? localStorage.getItem('jwt_token') : null;
      
      const response = await axios.post(`${baseURL}/auth/confirm-account-deletion`, data, {
        headers: {
          'Content-Type': 'application/json',
          ...(token && { 'Authorization': `Bearer ${token}` }),
        },
        withCredentials: true,
        timeout: 30000,
      });
      
      console.log('[AuthAPI] Account deletion successful:', response.status);
      return response.data;
    } catch (error: any) {
      console.log('[AuthAPI] Account deletion error:', error.response?.status, error.response?.data);
      
      // Don't automatically redirect - let the component handle it
      throw error;
    }
  }
};
