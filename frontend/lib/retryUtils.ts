import { GoApiError } from '@/types';

export interface RetryOptions {
  maxRetries?: number;
  initialDelay?: number;
  maxDelay?: number;
  backoffMultiplier?: number;
  shouldRetry?: (error: unknown, attempt: number) => boolean;
}

// Helper function to safely check error properties
function hasResponseStatus(error: unknown): error is { response: { status: number } } {
  return (
    typeof error === 'object' &&
    error !== null &&
    'response' in error &&
    typeof (error as any).response === 'object' &&
    (error as any).response !== null &&
    'status' in (error as any).response &&
    typeof (error as any).response.status === 'number'
  );
}

function hasErrorCode(error: unknown): error is { code: string } {
  return (
    typeof error === 'object' &&
    error !== null &&
    'code' in error &&
    typeof (error as any).code === 'string'
  );
}

const DEFAULT_OPTIONS: Required<RetryOptions> = {
  maxRetries: 3,
  initialDelay: 1000,
  maxDelay: 10000,
  backoffMultiplier: 2,
  shouldRetry: (error: unknown) => {
    if (error instanceof GoApiError) {
      return error.isTemporary;
    }

    // Retry 500 Internal Server Error (temporary backend issues)
    if (hasResponseStatus(error) && error.response.status === 500) {
      return true;
    }

    // Retry 502 Bad Gateway (temporary backend issues)
    if (hasResponseStatus(error) && error.response.status === 502) {
      return true;
    }

    // Retry 503 Service Unavailable (temporary backend issues)
    if (hasResponseStatus(error) && error.response.status === 503) {
      return true;
    }

    // Retry 504 Gateway Timeout (temporary backend issues)
    if (hasResponseStatus(error) && error.response.status === 504) {
      return true;
    }

    // Retry network errors (no response)
    if (!hasResponseStatus(error) && hasErrorCode(error) && error.code === 'ECONNABORTED') {
      return true;
    }

    return false;
  },
};

function delay(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms));
}

function calculateDelay(attempt: number, options: Required<RetryOptions>): number {
  const exponentialDelay = options.initialDelay * Math.pow(options.backoffMultiplier, attempt);
  return Math.min(exponentialDelay, options.maxDelay);
}

export async function fetchWithRetry<T>(
  fetchFn: () => Promise<T>,
  options: RetryOptions = {},
): Promise<T> {
  const opts = { ...DEFAULT_OPTIONS, ...options };
  let lastError: any;

  for (let attempt = 0; attempt <= opts.maxRetries; attempt++) {
    try {
      return await fetchFn();
    } catch (error: unknown) {
      lastError = error;

      if (attempt === opts.maxRetries) {
        throw error;
      }

      const shouldRetry = opts.shouldRetry(error, attempt);

      if (!shouldRetry) {
        throw error;
      }

      const delayMs = calculateDelay(attempt, opts);

      if (error instanceof GoApiError) {
      } else if (hasResponseStatus(error)) {
      } else {
      }

      await delay(delayMs);
    }
  }

  throw lastError;
}

export function isGoApiTemporaryError(error: any): boolean {
  return error instanceof GoApiError && error.isTemporary;
}

export function getRetryDelay(error: any, attempt: number = 0): number {
  if (error instanceof GoApiError) {
    switch (error.errorType) {
      case 'Timeout':
        return 1000;
      case 'NetworkError':
        return 2000 * Math.pow(2, attempt);
      case 'ServiceUnavailable':
        return 5000 * Math.pow(2, attempt);
      default:
        return 1000;
    }
  }
  return 1000;
}
