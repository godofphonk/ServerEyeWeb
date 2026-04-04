import { fetchWithRetry, isGoApiTemporaryError, getRetryDelay } from '../retryUtils';
import { GoApiError, GoApiErrorResponse } from '@/types';

describe('retryUtils', () => {
  // Helper function to create GoApiError mock
  const makeGoApiError = (
    errorType: string,
    isTemporary: boolean,
    httpStatus = 503,
  ): GoApiError => {
    const response: GoApiErrorResponse = {
      error: 'Test error',
      message: 'Test message',
      user_message: 'Test user message',
      error_code: `GO_API_${errorType.toUpperCase()}` as any,
      support_contact: 'support@test.com',
      timestamp: new Date().toISOString(),
      details: {
        error_type: errorType as any,
        is_temporary: isTemporary,
      },
    };
    return new GoApiError(response, httpStatus);
  };

  describe('fetchWithRetry', () => {
    it('should return result on successful fetch', async () => {
      const fetchFn = jest.fn().mockResolvedValue('success');

      const resultPromise = fetchWithRetry(fetchFn);
      jest.runAllTimers();
      const result = await resultPromise;

      expect(result).toBe('success');
      expect(fetchFn).toHaveBeenCalledTimes(1);
    });

    it('should retry on 500 error', async () => {
      const fetchFn = jest
        .fn()
        .mockRejectedValueOnce({ response: { status: 500 } })
        .mockResolvedValue('success');

      const result = await fetchWithRetry(fetchFn, { maxRetries: 3 });

      expect(result).toBe('success');
      expect(fetchFn).toHaveBeenCalledTimes(2);
    });

    it('should retry on 502 error', async () => {
      const fetchFn = jest
        .fn()
        .mockRejectedValueOnce({ response: { status: 502 } })
        .mockResolvedValue('success');

      const result = await fetchWithRetry(fetchFn);

      expect(result).toBe('success');
      expect(fetchFn).toHaveBeenCalledTimes(2);
    });

    it('should retry on 503 error', async () => {
      const fetchFn = jest
        .fn()
        .mockRejectedValueOnce({ response: { status: 503 } })
        .mockResolvedValue('success');

      const result = await fetchWithRetry(fetchFn);

      expect(result).toBe('success');
      expect(fetchFn).toHaveBeenCalledTimes(2);
    });

    it('should retry on 504 error', async () => {
      const fetchFn = jest
        .fn()
        .mockRejectedValueOnce({ response: { status: 504 } })
        .mockResolvedValue('success');

      const result = await fetchWithRetry(fetchFn);

      expect(result).toBe('success');
      expect(fetchFn).toHaveBeenCalledTimes(2);
    });

    it('should throw after max retries', async () => {
      const fetchFn = jest.fn().mockRejectedValue({ response: { status: 500 } });

      await expect(fetchWithRetry(fetchFn, { maxRetries: 2 })).rejects.toEqual({
        response: { status: 500 },
      });
      expect(fetchFn).toHaveBeenCalledTimes(3); // initial + 2 retries
    });

    it('should retry on network error with ECONNABORTED', async () => {
      const fetchFn = jest
        .fn()
        .mockRejectedValueOnce({ code: 'ECONNABORTED' })
        .mockResolvedValue('success');

      const result = await fetchWithRetry(fetchFn, { maxRetries: 3 });

      expect(result).toBe('success');
      expect(fetchFn).toHaveBeenCalledTimes(2);
    });

    it('should use exponential backoff', async () => {
      const fetchFn = jest
        .fn()
        .mockRejectedValueOnce({ response: { status: 500 } })
        .mockRejectedValueOnce({ response: { status: 500 } })
        .mockResolvedValue('success');

      const options = {
        maxRetries: 3,
        baseDelay: 1000,
        maxDelay: 5000,
      };

      const result = await fetchWithRetry(fetchFn, options);

      expect(result).toBe('success');
      expect(fetchFn).toHaveBeenCalledTimes(3);
    });

    it('should respect max delay', async () => {
      const fetchFn = jest
        .fn()
        .mockRejectedValueOnce({ response: { status: 500 } })
        .mockResolvedValue('success');

      const options = {
        maxRetries: 3,
        baseDelay: 1000,
        maxDelay: 2000,
        backoffMultiplier: 10,
      };

      const result = await fetchWithRetry(fetchFn, options);

      expect(result).toBe('success');
    });

    it('should use custom shouldRetry function', async () => {
      const fetchFn = jest
        .fn()
        .mockRejectedValueOnce(new Error('Custom error'))
        .mockResolvedValue('success');

      const shouldRetry = jest.fn().mockReturnValue(true);

      const result = await fetchWithRetry(fetchFn, { shouldRetry });

      expect(result).toBe('success');
      expect(shouldRetry).toHaveBeenCalled();
      expect(fetchFn).toHaveBeenCalledTimes(2);
    });

    it('should retry on GoApiError with isTemporary=true', async () => {
      const error = makeGoApiError('Timeout', true, 500);
      const fetchFn = jest.fn().mockRejectedValueOnce(error).mockResolvedValue('success');

      const result = await fetchWithRetry(fetchFn);

      expect(result).toBe('success');
      expect(fetchFn).toHaveBeenCalledTimes(2);
    });

    it('should not retry on GoApiError with isTemporary=false', async () => {
      const error = makeGoApiError('ValidationError', false, 400);
      const fetchFn = jest.fn().mockRejectedValue(error);

      await expect(fetchWithRetry(fetchFn)).rejects.toThrow(error);
      expect(fetchFn).toHaveBeenCalledTimes(1);
    });
  });

  describe('isGoApiTemporaryError', () => {
    it('should return true for temporary GoApiError', () => {
      const error = makeGoApiError('Timeout', true, 500);
      expect(isGoApiTemporaryError(error)).toBe(true);
    });

    it('should return false for non-temporary GoApiError', () => {
      const error = makeGoApiError('ValidationError', false, 400);
      expect(isGoApiTemporaryError(error)).toBe(false);
    });

    it('should return false for non-GoApiError', () => {
      const error = new Error('Regular error');
      expect(isGoApiTemporaryError(error)).toBe(false);
    });
  });

  describe('getRetryDelay', () => {
    it('should return 1000ms for Timeout error', () => {
      const error = makeGoApiError('Timeout', true, 500);
      expect(getRetryDelay(error, 1)).toBe(1000);
    });

    it('should return exponential delay for NetworkError', () => {
      const error = makeGoApiError('NetworkError', true, 503);
      expect(getRetryDelay(error, 2)).toBe(8000); // 2000 * 2^2 = 8000
    });

    it('should return exponential delay for ServiceUnavailable', () => {
      const error = makeGoApiError('ServiceUnavailable', true, 503);
      expect(getRetryDelay(error, 3)).toBe(40000); // 5000 * 2^3 = 40000
    });

    it('should return 1000ms for unknown GoApiError type', () => {
      const error = makeGoApiError('Unknown', true, 500);
      expect(getRetryDelay(error, 1)).toBe(1000);
    });
  });
});
