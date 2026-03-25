import { fetchWithRetry, isGoApiTemporaryError, getRetryDelay } from '@/lib/retryUtils';
import { GoApiError, GoApiErrorResponse } from '@/types';

const makeGoApiError = (
  errorType: string,
  isTemporary: boolean,
  httpStatus = 503
): GoApiError => {
  const response: GoApiErrorResponse = {
    error: 'test error',
    message: 'Test error message',
    user_message: 'User-facing error message',
    error_code: 'GO_API_SERVICEUNAVAILABLE',
    support_contact: 'support@example.com',
    timestamp: new Date().toISOString(),
    details: {
      error_type: errorType as any,
      is_temporary: isTemporary,
    },
  };
  return new GoApiError(response, httpStatus);
};

describe('isGoApiTemporaryError', () => {
  it('returns true for a temporary GoApiError', () => {
    const error = makeGoApiError('ServiceUnavailable', true);
    expect(isGoApiTemporaryError(error)).toBe(true);
  });

  it('returns false for a non-temporary GoApiError', () => {
    const error = makeGoApiError('Unauthorized', false);
    expect(isGoApiTemporaryError(error)).toBe(false);
  });

  it('returns false for a plain Error', () => {
    expect(isGoApiTemporaryError(new Error('plain error'))).toBe(false);
  });

  it('returns false for null', () => {
    expect(isGoApiTemporaryError(null)).toBe(false);
  });

  it('returns false for a string error', () => {
    expect(isGoApiTemporaryError('some error string')).toBe(false);
  });
});

describe('getRetryDelay', () => {
  it('returns 1000 for GoApiError with Timeout type', () => {
    const error = makeGoApiError('Timeout', true);
    expect(getRetryDelay(error, 0)).toBe(1000);
  });

  it('returns 2000 for GoApiError with NetworkError type at attempt 0', () => {
    const error = makeGoApiError('NetworkError', true);
    expect(getRetryDelay(error, 0)).toBe(2000);
  });

  it('returns 4000 for GoApiError with NetworkError type at attempt 1', () => {
    const error = makeGoApiError('NetworkError', true);
    expect(getRetryDelay(error, 1)).toBe(4000);
  });

  it('returns 5000 for GoApiError with ServiceUnavailable at attempt 0', () => {
    const error = makeGoApiError('ServiceUnavailable', true);
    expect(getRetryDelay(error, 0)).toBe(5000);
  });

  it('returns 10000 for GoApiError with ServiceUnavailable at attempt 1', () => {
    const error = makeGoApiError('ServiceUnavailable', true);
    expect(getRetryDelay(error, 1)).toBe(10000);
  });

  it('returns 1000 for GoApiError with unknown type', () => {
    const error = makeGoApiError('InvalidResponse', false);
    expect(getRetryDelay(error, 0)).toBe(1000);
  });

  it('returns 1000 for non-GoApiError', () => {
    expect(getRetryDelay(new Error('generic'))).toBe(1000);
    expect(getRetryDelay(null)).toBe(1000);
    expect(getRetryDelay({ response: { status: 500 } })).toBe(1000);
  });

  it('returns 1000 when no attempt is specified', () => {
    const error = makeGoApiError('Timeout', true);
    expect(getRetryDelay(error)).toBe(1000);
  });
});

describe('fetchWithRetry', () => {
  it('returns the result when fetch succeeds on first attempt', async () => {
    const mockFetch = jest.fn().mockResolvedValue('success');
    const result = await fetchWithRetry(mockFetch, { maxRetries: 3, initialDelay: 0 });
    expect(result).toBe('success');
    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  it('retries on 500 status error and returns result on second attempt', async () => {
    const error500 = { response: { status: 500 } };
    const mockFetch = jest
      .fn()
      .mockRejectedValueOnce(error500)
      .mockResolvedValueOnce('success after retry');

    const result = await fetchWithRetry(mockFetch, { maxRetries: 3, initialDelay: 0 });
    expect(result).toBe('success after retry');
    expect(mockFetch).toHaveBeenCalledTimes(2);
  });

  it('throws immediately for non-retryable error (e.g. 400)', async () => {
    const error400 = { response: { status: 400 } };
    const mockFetch = jest.fn().mockRejectedValue(error400);

    await expect(
      fetchWithRetry(mockFetch, { maxRetries: 3, initialDelay: 0 })
    ).rejects.toEqual(error400);

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  it('throws after exhausting all retries', async () => {
    const error503 = { response: { status: 503 } };
    const mockFetch = jest.fn().mockRejectedValue(error503);

    await expect(
      fetchWithRetry(mockFetch, { maxRetries: 2, initialDelay: 0 })
    ).rejects.toEqual(error503);

    expect(mockFetch).toHaveBeenCalledTimes(3);
  });

  it('respects custom maxRetries option', async () => {
    const error = { response: { status: 502 } };
    const mockFetch = jest.fn().mockRejectedValue(error);

    await expect(
      fetchWithRetry(mockFetch, { maxRetries: 1, initialDelay: 0 })
    ).rejects.toEqual(error);

    expect(mockFetch).toHaveBeenCalledTimes(2);
  });

  it('respects custom shouldRetry option that always returns false', async () => {
    const error = { response: { status: 500 } };
    const mockFetch = jest.fn().mockRejectedValue(error);

    await expect(
      fetchWithRetry(mockFetch, { maxRetries: 3, shouldRetry: () => false })
    ).rejects.toEqual(error);

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });

  it('retries on ECONNABORTED error', async () => {
    const networkError = { code: 'ECONNABORTED' };
    const mockFetch = jest
      .fn()
      .mockRejectedValueOnce(networkError)
      .mockResolvedValueOnce('recovered');

    const result = await fetchWithRetry(mockFetch, { maxRetries: 3, initialDelay: 0 });
    expect(result).toBe('recovered');
    expect(mockFetch).toHaveBeenCalledTimes(2);
  });

  it('retries for temporary GoApiError', async () => {
    const tempError = makeGoApiError('ServiceUnavailable', true);
    const mockFetch = jest
      .fn()
      .mockRejectedValueOnce(tempError)
      .mockResolvedValueOnce('recovered');

    const result = await fetchWithRetry(mockFetch, { maxRetries: 3, initialDelay: 0 });
    expect(result).toBe('recovered');
    expect(mockFetch).toHaveBeenCalledTimes(2);
  });

  it('does not retry for non-temporary GoApiError', async () => {
    const nonTempError = makeGoApiError('Unauthorized', false, 401);
    const mockFetch = jest.fn().mockRejectedValue(nonTempError);

    await expect(
      fetchWithRetry(mockFetch, { maxRetries: 3, initialDelay: 0 })
    ).rejects.toBe(nonTempError);

    expect(mockFetch).toHaveBeenCalledTimes(1);
  });
});
