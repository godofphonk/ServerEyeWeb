import { fetchWithRetry, isGoApiTemporaryError, getRetryDelay } from '../retryUtils';
import { GoApiError } from '@/types';

// Mock timers
jest.useFakeTimers();

describe('retryUtils', () => {
  afterEach(() => {
    jest.clearAllTimers();
  });

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

      const resultPromise = fetchWithRetry(fetchFn, { maxRetries: 3 });
      jest.runAllTimers();
      const result = await resultPromise;

      expect(result).toBe('success');
      expect(fetchFn).toHaveBeenCalledTimes(2);
    });

    it('should retry on 502 error', async () => {
      const fetchFn = jest
        .fn()
        .mockRejectedValueOnce({ response: { status: 502 } })
        .mockResolvedValue('success');

      const resultPromise = fetchWithRetry(fetchFn);
      jest.runAllTimers();
      const result = await resultPromise;

      expect(result).toBe('success');
      expect(fetchFn).toHaveBeenCalledTimes(2);
    });

    it('should retry on 503 error', async () => {
      const fetchFn = jest
        .fn()
        .mockRejectedValueOnce({ response: { status: 503 } })
        .mockResolvedValue('success');

      const resultPromise = fetchWithRetry(fetchFn);
      jest.runAllTimers();
      const result = await resultPromise;

      expect(result).toBe('success');
      expect(fetchFn).toHaveBeenCalledTimes(2);
    });

    it('should retry on 504 error', async () => {
      const fetchFn = jest
        .fn()
        .mockRejectedValueOnce({ response: { status: 504 } })
        .mockResolvedValue('success');

      const resultPromise = fetchWithRetry(fetchFn);
      jest.runAllTimers();
      const result = await resultPromise;

      expect(result).toBe('success');
      expect(fetchFn).toHaveBeenCalledTimes(2);
    });

    it('should not retry on 404 error', async () => {
      const fetchFn = jest.fn().mockRejectedValue({ response: { status: 404 } });

      await expect(fetchWithRetry(fetchFn)).rejects.toEqual({ response: { status: 404 } });
      expect(fetchFn).toHaveBeenCalledTimes(1);
    });

    it('should not retry on 401 error', async () => {
      const fetchFn = jest.fn().mockRejectedValue({ response: { status: 401 } });

      await expect(fetchWithRetry(fetchFn)).rejects.toEqual({ response: { status: 401 } });
      expect(fetchFn).toHaveBeenCalledTimes(1);
    });

    it('should throw after max retries', async () => {
      const fetchFn = jest.fn().mockRejectedValue({ response: { status: 500 } });

      const resultPromise = fetchWithRetry(fetchFn, { maxRetries: 2 });
      jest.runAllTimers();

      await expect(resultPromise).rejects.toEqual({ response: { status: 500 } });
      expect(fetchFn).toHaveBeenCalledTimes(3); // initial + 2 retries
    });

    it('should retry on network error with ECONNABORTED', async () => {
      const fetchFn = jest
        .fn()
        .mockRejectedValueOnce({ code: 'ECONNABORTED' })
        .mockResolvedValue('success');

      const resultPromise = fetchWithRetry(fetchFn);
      jest.runAllTimers();
      const result = await resultPromise;

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
        initialDelay: 100,
        backoffMultiplier: 2,
      };

      const resultPromise = fetchWithRetry(fetchFn, options);
      jest.runAllTimers();
      const result = await resultPromise;

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
        initialDelay: 1000,
        maxDelay: 2000,
        backoffMultiplier: 10,
      };

      const resultPromise = fetchWithRetry(fetchFn, options);
      jest.runAllTimers();
      const result = await resultPromise;

      expect(result).toBe('success');
    });

    it('should use custom shouldRetry function', async () => {
      const fetchFn = jest
        .fn()
        .mockRejectedValueOnce(new Error('Custom error'))
        .mockResolvedValue('success');

      const shouldRetry = jest.fn().mockReturnValue(true);

      const resultPromise = fetchWithRetry(fetchFn, { shouldRetry });
      jest.runAllTimers();
      const result = await resultPromise;

      expect(result).toBe('success');
      expect(shouldRetry).toHaveBeenCalled();
      expect(fetchFn).toHaveBeenCalledTimes(2);
    });

    it('should retry on GoApiError with isTemporary=true', async () => {
      const error = new GoApiError('Temporary error', 'Timeout', 500, true);
      const fetchFn = jest
        .fn()
        .mockRejectedValueOnce(error)
        .mockResolvedValue('success');

      const resultPromise = fetchWithRetry(fetchFn);
      jest.runAllTimers();
      const result = await resultPromise;

      expect(result).toBe('success');
      expect(fetchFn).toHaveBeenCalledTimes(2);
    });

    it('should not retry on GoApiError with isTemporary=false', async () => {
      const error = new GoApiError('Permanent error', 'ValidationError', 400, false);
      const fetchFn = jest.fn().mockRejectedValue(error);

      await expect(fetchWithRetry(fetchFn)).rejects.toThrow(error);
      expect(fetchFn).toHaveBeenCalledTimes(1);
    });
  });

  describe('isGoApiTemporaryError', () => {
    it('should return true for temporary GoApiError', () => {
      const error = new GoApiError('Temporary', 'Timeout', 500, true);
      expect(isGoApiTemporaryError(error)).toBe(true);
    });

    it('should return false for non-temporary GoApiError', () => {
      const error = new GoApiError('Permanent', 'ValidationError', 400, false);
      expect(isGoApiTemporaryError(error)).toBe(false);
    });

    it('should return false for non-GoApiError', () => {
      const error = new Error('Regular error');
      expect(isGoApiTemporaryError(error)).toBe(false);
    });
  });

  describe('getRetryDelay', () => {
    it('should return 1000ms for Timeout error', () => {
      const error = new GoApiError('Timeout', 'Timeout', 500, true);
      expect(getRetryDelay(error, 0)).toBe(1000);
    });

    it('should return exponential delay for NetworkError', () => {
      const error = new GoApiError('Network', 'NetworkError', 500, true);
      expect(getRetryDelay(error, 0)).toBe(2000);
      expect(getRetryDelay(error, 1)).toBe(4000);
      expect(getRetryDelay(error, 2)).toBe(8000);
    });

    it('should return exponential delay for ServiceUnavailable', () => {
      const error = new GoApiError('Unavailable', 'ServiceUnavailable', 503, true);
      expect(getRetryDelay(error, 0)).toBe(5000);
      expect(getRetryDelay(error, 1)).toBe(10000);
      expect(getRetryDelay(error, 2)).toBe(20000);
    });

    it('should return 1000ms for non-GoApiError', () => {
      const error = new Error('Regular error');
      expect(getRetryDelay(error, 0)).toBe(1000);
    });

    it('should return 1000ms for unknown GoApiError type', () => {
      const error = new GoApiError('Unknown', 'UnknownType' as any, 500, true);
      expect(getRetryDelay(error, 0)).toBe(1000);
    });
  });
});
