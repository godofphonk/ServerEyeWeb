import { apiClient } from './api';

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
    const response = await apiClient.get<SubscriptionPlan[]>('/subscription/plans');
    return response.data;
  },

  async getCurrentSubscription(): Promise<Subscription | null> {
    try {
      const response = await apiClient.get<Subscription>('/subscription/current');
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 404) {
        return null;
      }
      throw error;
    }
  },

  async createCheckout(request: CreateCheckoutRequest): Promise<CreateCheckoutResponse> {
    const response = await apiClient.post<CreateCheckoutResponse>('/subscription/checkout', request);
    return response.data;
  },

  async updatePlan(planType: number, isYearly: boolean): Promise<Subscription> {
    const response = await apiClient.put<Subscription>('/subscription/plan', {
      newPlanType: planType,
      isYearly
    });
    return response.data;
  },

  async cancelSubscription(cancelImmediately: boolean = false): Promise<void> {
    await apiClient.post('/subscription/cancel', {
      cancelImmediately,
      cancellationReason: ''
    });
  },

  async reactivateSubscription(): Promise<Subscription> {
    const response = await apiClient.post<Subscription>('/subscription/reactivate');
    return response.data;
  },

  async getLimits(): Promise<SubscriptionLimits> {
    const response = await apiClient.get<SubscriptionLimits>('/subscription/limits');
    return response.data;
  },

  async getPaymentHistory(limit: number = 50): Promise<Payment[]> {
    const response = await apiClient.get<Payment[]>(`/payment/history?limit=${limit}`);
    return response.data;
  }
};
