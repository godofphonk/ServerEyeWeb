import axios, { AxiosInstance, AxiosRequestConfig, AxiosError } from 'axios';
import {
  GoApiError,
  GoApiErrorResponse,
  DeleteSourceResponse,
  DeleteSourceIdentifiersResponse,
  DeleteSourceIdentifiersRequest,
} from '../types/index';

class ApiClient {
  private client: AxiosInstance;
  private isRefreshing = false;
  private refreshSubscribers: ((token: string) => void)[] = [];

  constructor() {
    // API base URL must be set via environment variable
    const baseURL = process.env.NEXT_PUBLIC_API_URL!;

    this.client = axios.create({
      baseURL,
      withCredentials: true, // Send cookies for HttpOnly cookie support
    });

    // Request interceptor
    this.client.interceptors.request.use(
      config => {
        // Cookies are handled by the Next.js proxy server-side
        return config;
      },
      error => Promise.reject(error),
    );

    this.setupInterceptors();
  }

  private isGoApiError(error: unknown): error is AxiosError<GoApiErrorResponse> {
    /* eslint-disable @typescript-eslint/no-explicit-any */
    return (
      error &&
      typeof error === 'object' &&
      'response' in error &&
      (error as any).response?.data &&
      typeof (error as any).response.data === 'object' &&
      'error_code' in (error as any).response.data &&
      typeof (error as any).response.data.error_code === 'string' &&
      (error as any).response.data.error_code.startsWith('GO_API_')
    );
    /* eslint-enable @typescript-eslint/no-explicit-any */
  }

  private setupInterceptors() {
    // Cookies are sent automatically via withCredentials: true
    this.client.interceptors.request.use(
      config => config,
      error => Promise.reject(error),
    );

    this.client.interceptors.response.use(
      response => response,
      async error => {
        // Check if this is a Go API error
        if (this.isGoApiError(error)) {
          const goApiError = new GoApiError(error.response!.data, error.response!.status);
          return Promise.reject(goApiError);
        }

        // Handle 401 errors (authentication)
        if (error.response?.status === 401 && typeof window !== 'undefined') {
          const originalRequest = error.config;

          // If already refreshing, add to subscribers queue
          if (this.isRefreshing) {
            return new Promise(resolve => {
              this.refreshSubscribers.push(_token => {
                if (originalRequest) {
                  resolve(this.client.request(originalRequest));
                }
              });
            });
          }

          // Start refresh process
          this.isRefreshing = true;

          try {
            // eslint-disable-next-line no-console
            console.log('[ApiClient] Attempting token refresh...');
            const refreshResponse = await fetch('/api/auth/refresh', {
              method: 'POST',
              credentials: 'include',
              headers: {
                'Content-Type': 'application/json',
              },
            });

            // eslint-disable-next-line no-console
            console.log('[ApiClient] Refresh response status:', refreshResponse.status);

            if (refreshResponse.ok) {
              // eslint-disable-next-line no-console
              console.log('[ApiClient] Refresh successful, retrying original request');
              // Notify all subscribers
              this.refreshSubscribers.forEach(callback => callback('refreshed'));
              this.refreshSubscribers = [];

              // Small delay to ensure cookies are applied
              await new Promise(resolve => setTimeout(resolve, 100));

              // Retry the original request
              if (originalRequest) {
                return this.client.request(originalRequest);
              }
            } else {
              // eslint-disable-next-line no-console
              console.log('[ApiClient] Refresh failed with status:', refreshResponse.status);
              // Refresh failed, clear cookies and redirect
              this.refreshSubscribers.forEach(callback => callback('failed'));
              this.refreshSubscribers = [];

              document.cookie.split(';').forEach(c => {
                document.cookie = c
                  .replace(/^ +/, '')
                  .replace(/=.*/, '=;expires=' + new Date().toUTCString() + ';path=/');
              });

              // Redirect to login only if not already on login page
              if (window.location.pathname !== '/login') {
                // eslint-disable-next-line no-console
                console.log('[ApiClient] Redirecting to login');
                window.location.href = '/login';
              }
            }
          } catch (refreshError) {
             
            console.error('[ApiClient] Refresh error:', refreshError);
            // Refresh failed, notify subscribers and clear cookies
            this.refreshSubscribers.forEach(callback => callback('failed'));
            this.refreshSubscribers = [];

            document.cookie.split(';').forEach(c => {
              document.cookie = c
                .replace(/^ +/, '')
                .replace(/=.*/, '=;expires=' + new Date().toUTCString() + ';path=/');
            });

            // Redirect to login only if not already on login page
            if (window.location.pathname !== '/login') {
              // eslint-disable-next-line no-console
              console.log('[ApiClient] Redirecting to login after error');
              window.location.href = '/login';
            }
          } finally {
            this.isRefreshing = false;
          }
        }

        return Promise.reject(error);
      },
    );
  }

  async get<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.get<T>(url, config);
    return response.data;
  }

  async post<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.post<T>(url, data, config);
    return response.data;
  }

  async put<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.put<T>(url, data, config);
    return response.data;
  }

  async delete<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.delete<T>(url, config);
    return response.data;
  }

  async patch<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.patch<T>(url, data, config);
    return response.data;
  }

  // Source Management methods
  async deleteServerSource(serverKey: string, source: string): Promise<DeleteSourceResponse> {
    return this.delete<DeleteSourceResponse>(
      `/servers/by-key/${encodeURIComponent(serverKey)}/sources/${encodeURIComponent(source)}`,
    );
  }

  async deleteServerSourceIdentifiers(
    serverKey: string,
    request: DeleteSourceIdentifiersRequest,
  ): Promise<DeleteSourceIdentifiersResponse> {
    return this.delete<DeleteSourceIdentifiersResponse>(
      `/servers/by-key/${encodeURIComponent(serverKey)}/sources/identifiers`,
      {
        data: request,
      },
    );
  }

  async deleteServerSourceIdentifiersByType(
    serverKey: string,
    sourceType: string,
    request: DeleteSourceIdentifiersRequest,
  ): Promise<DeleteSourceIdentifiersResponse> {
    return this.delete<DeleteSourceIdentifiersResponse>(
      `/servers/by-key/${encodeURIComponent(serverKey)}/sources/${encodeURIComponent(sourceType)}/identifiers`,
      {
        data: request,
      },
    );
  }
}

export const apiClient = new ApiClient();
