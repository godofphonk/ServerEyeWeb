import { apiClient } from '@/lib/api';

export interface SubscriptionPlan {
  id: string;
  planType: number;
  name: string;
  description: string;
  monthlyPrice: number;
  yearlyPrice: number;
  maxServers: number;
  metricsRetentionDays: number;
  hasAlerts: boolean;
  hasApiAccess: boolean;
  hasPrioritySupport: boolean;
  features: string[];
}

export interface Subscription {
  id: string;
  userId: string;
  planType: number;
  planName: string;
  status: number;
  amount: number;
  currency: string;
  isYearly: boolean;
  currentPeriodStart?: string;
  currentPeriodEnd?: string;
  canceledAt?: string;
  trialEnd?: string;
  createdAt: string;
}

export interface Payment {
  id: string;
  userId: string;
  amount: number;
  currency: string;
  status: number;
  receiptUrl?: string;
  invoiceUrl?: string;
  createdAt: string;
}

export interface CreateCheckoutRequest {
  planType: number;
  isYearly: boolean;
  successUrl?: string;
  cancelUrl?: string;
}

export interface CreateCheckoutResponse {
  sessionId: string;
  sessionUrl: string;
}

export interface SubscriptionLimits {
  maxServers: number;
  hasAlerts: boolean;
  hasApi: boolean;
}

export const billingApi = {
  async getPlans(): Promise<SubscriptionPlan[]> {
    return await apiClient.get<SubscriptionPlan[]>('/subscription/plans');
  },

  async getCurrentSubscription(): Promise<Subscription | null> {
    try {
      return await apiClient.get<Subscription>('/subscription/current');
    } catch (error: unknown) {
      if (error && typeof error === 'object' && 'response' in error) {
        const axiosError = error as { response?: { status?: number } };
        if (axiosError.response?.status === 404) {
          return null;
        }
      }
      throw error;
    }
  },

  async createCheckout(request: CreateCheckoutRequest): Promise<CreateCheckoutResponse> {
    return await apiClient.post<CreateCheckoutResponse>('/subscription/checkout', request);
  },

  async updatePlan(planType: number, isYearly: boolean): Promise<Subscription> {
    return await apiClient.put<Subscription>('/subscription/plan', {
      newPlanType: planType,
      isYearly,
    });
  },

  async cancelSubscription(cancelImmediately: boolean = false): Promise<void> {
    await apiClient.post('/subscription/cancel', {
      cancelImmediately,
      cancellationReason: '',
    });
  },

  async reactivateSubscription(): Promise<Subscription> {
    return await apiClient.post<Subscription>('/subscription/reactivate');
  },

  async getLimits(): Promise<SubscriptionLimits> {
    return await apiClient.get<SubscriptionLimits>('/subscription/limits');
  },

  async getPaymentHistory(limit: number = 50): Promise<Payment[]> {
    return await apiClient.get<Payment[]>(`/payment/history?limit=${limit}`);
  },
};
