import axios, { AxiosInstance, AxiosRequestConfig, AxiosError } from 'axios';
import { GoApiError, GoApiErrorResponse, DeleteSourceResponse, DeleteSourceIdentifiersResponse, DeleteSourceIdentifiersRequest } from '../types/index';

class ApiClient {
  private client: AxiosInstance;

  constructor() {
    // Use 127.0.0.1 for browser access to Docker container
    const baseURL = 'http://127.0.0.1:5246/api';
    
    
    this.client = axios.create({
      baseURL,
      timeout: 30000, // Increased to 30s
      headers: {
        'Content-Type': 'application/json',
      },
      withCredentials: true, // Send cookies for authentication
    });

    this.setupInterceptors();
  }

  private isGoApiError(error: any): error is AxiosError<GoApiErrorResponse> {
    return (
      error.response?.data &&
      typeof error.response.data === 'object' &&
      'error_code' in error.response.data &&
      typeof error.response.data.error_code === 'string' &&
      error.response.data.error_code.startsWith('GO_API_')
    );
  }

  private setupInterceptors() {
    // Add JWT token to all requests
    this.client.interceptors.request.use(
      config => {
        if (typeof window !== 'undefined') {
          const token = localStorage.getItem('jwt_token') || localStorage.getItem('access_token');
          if (token) {
            config.headers.Authorization = `Bearer ${token}`;
          } else {
          }
        }
        return config;
      },
      error => Promise.reject(error),
    );

    this.client.interceptors.response.use(
      response => response,
      async error => {
        // Check if this is a Go API error
        if (this.isGoApiError(error)) {
          const goApiError = new GoApiError(
            error.response!.data,
            error.response!.status
          );
          return Promise.reject(goApiError);
        }

        // Handle 401 errors (authentication)
        if (error.response?.status === 401 && typeof window !== 'undefined') {
          
          // Try to refresh token first
          try {
            const refreshResponse = await fetch('/api/auth/refresh', {
              method: 'POST',
              credentials: 'include',
              headers: {
                'Content-Type': 'application/json',
              },
            });

            if (refreshResponse.ok) {
              const refreshData = await refreshResponse.json();
              if (refreshData.token) {
                localStorage.setItem('jwt_token', refreshData.token);
                
                // Retry the original request with new token
                if (error.config) {
                  error.config.headers.Authorization = `Bearer ${refreshData.token}`;
                  return this.client.request(error.config);
                }
              }
            }
          } catch (refreshError) {
          }

          // If refresh failed, clear tokens and redirect to login
          localStorage.removeItem('jwt_token');
          localStorage.removeItem('access_token');
          localStorage.removeItem('refresh_token');
          
          // Clear cookies
          document.cookie.split(';').forEach(c => {
            document.cookie = c
              .replace(/^ +/, '')
              .replace(/=.*/, '=;expires=' + new Date().toUTCString() + ';path=/');
          });

          // Redirect to login only if not already on login page
          if (window.location.pathname !== '/login') {
            window.location.href = '/login';
          }
        }
        
        return Promise.reject(error);
      },
    );
  }

  async get<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const fullUrl = `${this.client.defaults.baseURL}${url}`;
    
    const response = await this.client.get<T>(url, config);
    return response.data;
  }

  async post<T>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.post<T>(url, data, config);
    return response.data;
  }

  async put<T>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.put<T>(url, data, config);
    return response.data;
  }

  async delete<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.delete<T>(url, config);
    return response.data;
  }

  async patch<T>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.patch<T>(url, data, config);
    return response.data;
  }

  // Source Management methods
  async deleteServerSource(serverKey: string, source: string): Promise<DeleteSourceResponse> {
    return this.delete<DeleteSourceResponse>(`/servers/by-key/${encodeURIComponent(serverKey)}/sources/${encodeURIComponent(source)}`);
  }

  async deleteServerSourceIdentifiers(serverKey: string, request: DeleteSourceIdentifiersRequest): Promise<DeleteSourceIdentifiersResponse> {
    return this.delete<DeleteSourceIdentifiersResponse>(`/servers/by-key/${encodeURIComponent(serverKey)}/sources/identifiers`, {
      data: request
    });
  }

  async deleteServerSourceIdentifiersByType(serverKey: string, sourceType: string, request: DeleteSourceIdentifiersRequest): Promise<DeleteSourceIdentifiersResponse> {
    return this.delete<DeleteSourceIdentifiersResponse>(`/servers/by-key/${encodeURIComponent(serverKey)}/sources/${encodeURIComponent(sourceType)}/identifiers`, {
      data: request
    });
  }
}

export const apiClient = new ApiClient();
