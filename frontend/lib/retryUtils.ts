import { GoApiError } from '@/types';

export interface RetryOptions {
  maxRetries?: number;
  initialDelay?: number;
  maxDelay?: number;
  backoffMultiplier?: number;
  shouldRetry?: (error: any, attempt: number) => boolean;
}

const DEFAULT_OPTIONS: Required<RetryOptions> = {
  maxRetries: 3,
  initialDelay: 1000,
  maxDelay: 10000,
  backoffMultiplier: 2,
  shouldRetry: (error: any) => {
    if (error instanceof GoApiError) {
      return error.isTemporary;
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
  options: RetryOptions = {}
): Promise<T> {
  const opts = { ...DEFAULT_OPTIONS, ...options };
  let lastError: any;

  for (let attempt = 0; attempt <= opts.maxRetries; attempt++) {
    try {
      console.log(`[Retry] Attempt ${attempt + 1}/${opts.maxRetries + 1}`);
      return await fetchFn();
    } catch (error) {
      lastError = error;

      if (attempt === opts.maxRetries) {
        console.log('[Retry] Max retries reached, throwing error');
        throw error;
      }

      const shouldRetry = opts.shouldRetry(error, attempt);
      
      if (!shouldRetry) {
        console.log('[Retry] Error is not retryable, throwing immediately');
        throw error;
      }

      const delayMs = calculateDelay(attempt, opts);
      
      if (error instanceof GoApiError) {
        console.log(`[Retry] Go API temporary error (${error.errorType}), retrying in ${delayMs}ms...`);
      } else {
        console.log(`[Retry] Retryable error, retrying in ${delayMs}ms...`);
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
