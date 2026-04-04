import { billingApi, SubscriptionPlan, Subscription, Payment } from '@/lib/billingApi';

jest.mock('@/lib/api', () => ({
  apiClient: {
    get: jest.fn(),
    post: jest.fn(),
    put: jest.fn(),
    delete: jest.fn(),
  },
}));

// eslint-disable-next-line @typescript-eslint/no-require-imports
const { apiClient } = require('@/lib/api');

const makePlan = (overrides: Partial<SubscriptionPlan> = {}): SubscriptionPlan => ({
  id: 'plan-1',
  planType: 0,
  name: 'Free',
  description: 'Free tier plan',
  monthlyPrice: 0,
  yearlyPrice: 0,
  maxServers: 1,
  metricsRetentionDays: 7,
  hasAlerts: false,
  hasApiAccess: false,
  hasPrioritySupport: false,
  features: ['1 server', '7-day metrics'],
  ...overrides,
});

const makeSubscription = (overrides: Partial<Subscription> = {}): Subscription => ({
  id: 'sub-1',
  userId: 'user-1',
  planType: 0,
  planName: 'Free',
  status: 0,
  amount: 0,
  currency: 'usd',
  isYearly: false,
  createdAt: '2024-01-01T00:00:00Z',
  ...overrides,
});

const makePayment = (overrides: Partial<Payment> = {}): Payment => ({
  id: 'pay-1',
  userId: 'user-1',
  amount: 1000,
  currency: 'usd',
  status: 0,
  createdAt: '2024-01-01T00:00:00Z',
  ...overrides,
});

describe('billingApi', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('getPlans', () => {
    it('returns available subscription plans', async () => {
      const plans = [makePlan(), makePlan({ id: 'plan-2', name: 'Pro', planType: 1 })];
      apiClient.get.mockResolvedValue(plans);

      const result = await billingApi.getPlans();

      expect(apiClient.get).toHaveBeenCalledWith('/subscription/plans');
      expect(result).toEqual(plans);
      expect(result).toHaveLength(2);
    });

    it('returns empty array when no plans available', async () => {
      apiClient.get.mockResolvedValue([]);

      const result = await billingApi.getPlans();

      expect(result).toEqual([]);
    });

    it('propagates errors from apiClient', async () => {
      apiClient.get.mockRejectedValue(new Error('Service unavailable'));

      await expect(billingApi.getPlans()).rejects.toThrow('Service unavailable');
    });
  });

  describe('getCurrentSubscription', () => {
    it('returns the current subscription', async () => {
      const subscription = makeSubscription();
      apiClient.get.mockResolvedValue(subscription);

      const result = await billingApi.getCurrentSubscription();

      expect(apiClient.get).toHaveBeenCalledWith('/subscription/current');
      expect(result).toEqual(subscription);
    });

    it('returns null when no subscription found (404)', async () => {
      apiClient.get.mockRejectedValue({ response: { status: 404 } });

      const result = await billingApi.getCurrentSubscription();

      expect(result).toBeNull();
    });

    it('propagates non-404 errors', async () => {
      apiClient.get.mockRejectedValue({ response: { status: 500 }, message: 'Server error' });

      await expect(billingApi.getCurrentSubscription()).rejects.toMatchObject({
        response: { status: 500 },
      });
    });
  });

  describe('createCheckout', () => {
    it('creates a checkout session', async () => {
      const checkoutResponse = {
        sessionId: 'sess_123',
        sessionUrl: 'https://checkout.stripe.com/pay/sess_123',
      };
      apiClient.post.mockResolvedValue(checkoutResponse);

      const result = await billingApi.createCheckout({ planType: 1, isYearly: false });

      expect(apiClient.post).toHaveBeenCalledWith('/subscription/checkout', {
        planType: 1,
        isYearly: false,
      });
      expect(result.sessionId).toBe('sess_123');
      expect(result.sessionUrl).toContain('stripe.com');
    });

    it('creates a yearly checkout session', async () => {
      const checkoutResponse = {
        sessionId: 'sess_yearly',
        sessionUrl: 'https://checkout.stripe.com/pay/sess_yearly',
      };
      apiClient.post.mockResolvedValue(checkoutResponse);

      const result = await billingApi.createCheckout({ planType: 2, isYearly: true });

      expect(apiClient.post).toHaveBeenCalledWith('/subscription/checkout', {
        planType: 2,
        isYearly: true,
      });
      expect(result).toEqual(checkoutResponse);
    });

    it('propagates errors from apiClient', async () => {
      apiClient.post.mockRejectedValue(new Error('Payment failed'));

      await expect(billingApi.createCheckout({ planType: 1, isYearly: false })).rejects.toThrow(
        'Payment failed',
      );
    });
  });

  describe('updatePlan', () => {
    it('updates subscription plan', async () => {
      const updatedSubscription = makeSubscription({ planType: 1, planName: 'Pro' });
      apiClient.put.mockResolvedValue(updatedSubscription);

      const result = await billingApi.updatePlan(1, false);

      expect(apiClient.put).toHaveBeenCalledWith('/subscription/plan', {
        newPlanType: 1,
        isYearly: false,
      });
      expect(result.planType).toBe(1);
      expect(result.planName).toBe('Pro');
    });

    it('updates to yearly billing', async () => {
      const updatedSubscription = makeSubscription({ isYearly: true });
      apiClient.put.mockResolvedValue(updatedSubscription);

      const result = await billingApi.updatePlan(1, true);

      expect(apiClient.put).toHaveBeenCalledWith('/subscription/plan', {
        newPlanType: 1,
        isYearly: true,
      });
      expect(result.isYearly).toBe(true);
    });
  });

  describe('cancelSubscription', () => {
    it('cancels subscription with default parameters', async () => {
      apiClient.post.mockResolvedValue(undefined);

      await billingApi.cancelSubscription();

      expect(apiClient.post).toHaveBeenCalledWith('/subscription/cancel', {
        cancelImmediately: false,
        cancellationReason: '',
      });
    });

    it('cancels subscription immediately when requested', async () => {
      apiClient.post.mockResolvedValue(undefined);

      await billingApi.cancelSubscription(true);

      expect(apiClient.post).toHaveBeenCalledWith('/subscription/cancel', {
        cancelImmediately: true,
        cancellationReason: '',
      });
    });
  });

  describe('reactivateSubscription', () => {
    it('reactivates a cancelled subscription', async () => {
      const reactivatedSub = makeSubscription({ status: 0 });
      apiClient.post.mockResolvedValue(reactivatedSub);

      const result = await billingApi.reactivateSubscription();

      expect(apiClient.post).toHaveBeenCalledWith('/subscription/reactivate');
      expect(result).toEqual(reactivatedSub);
    });

    it('propagates errors from apiClient', async () => {
      apiClient.post.mockRejectedValue(new Error('No cancelled subscription'));

      await expect(billingApi.reactivateSubscription()).rejects.toThrow(
        'No cancelled subscription',
      );
    });
  });

  describe('getLimits', () => {
    it('returns subscription limits', async () => {
      const limits = { maxServers: 5, hasAlerts: true, hasApi: true };
      apiClient.get.mockResolvedValue(limits);

      const result = await billingApi.getLimits();

      expect(apiClient.get).toHaveBeenCalledWith('/subscription/limits');
      expect(result.maxServers).toBe(5);
      expect(result.hasAlerts).toBe(true);
    });

    it('returns free tier limits', async () => {
      const limits = { maxServers: 1, hasAlerts: false, hasApi: false };
      apiClient.get.mockResolvedValue(limits);

      const result = await billingApi.getLimits();

      expect(result.maxServers).toBe(1);
      expect(result.hasAlerts).toBe(false);
    });
  });

  describe('getPaymentHistory', () => {
    it('returns payment history with default limit', async () => {
      const payments = [makePayment(), makePayment({ id: 'pay-2' })];
      apiClient.get.mockResolvedValue(payments);

      const result = await billingApi.getPaymentHistory();

      expect(apiClient.get).toHaveBeenCalledWith('/payment/history?limit=50');
      expect(result).toHaveLength(2);
    });

    it('returns payment history with custom limit', async () => {
      const payments = [makePayment()];
      apiClient.get.mockResolvedValue(payments);

      const result = await billingApi.getPaymentHistory(10);

      expect(apiClient.get).toHaveBeenCalledWith('/payment/history?limit=10');
      expect(result).toHaveLength(1);
    });

    it('returns empty array when no payment history', async () => {
      apiClient.get.mockResolvedValue([]);

      const result = await billingApi.getPaymentHistory();

      expect(result).toEqual([]);
    });

    it('propagates errors from apiClient', async () => {
      apiClient.get.mockRejectedValue(new Error('Unauthorized'));

      await expect(billingApi.getPaymentHistory()).rejects.toThrow('Unauthorized');
    });
  });
});
