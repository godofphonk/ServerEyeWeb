import axios, { AxiosInstance, AxiosRequestConfig } from 'axios';

class ApiClient {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:5246/api',
      timeout: 30000, // Increased to 30s
      headers: {
        'Content-Type': 'application/json',
      },
      withCredentials: true, // Send cookies for authentication
    });

    this.setupInterceptors();
  }

  private setupInterceptors() {
    // Add JWT token to all requests
    this.client.interceptors.request.use(
      config => {
        if (typeof window !== 'undefined') {
          const token = localStorage.getItem('jwt_token') || localStorage.getItem('access_token');
          if (token) {
            config.headers.Authorization = `Bearer ${token}`;
            console.log('[API] Adding JWT token to request:', config.url);
          } else {
            console.log('[API] No JWT token found for request:', config.url);
          }
        }
        return config;
      },
      error => Promise.reject(error),
    );

    this.client.interceptors.response.use(
      response => response,
      async error => {
        if (error.response?.status === 401 && typeof window !== 'undefined') {
          console.log('[API] 401 Unauthorized - attempting token refresh');
          
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
                console.log('[API] Token refreshed successfully');
                
                // Retry the original request with new token
                if (error.config) {
                  error.config.headers.Authorization = `Bearer ${refreshData.token}`;
                  return this.client.request(error.config);
                }
              }
            }
          } catch (refreshError) {
            console.log('[API] Token refresh failed:', refreshError);
          }

          // If refresh failed, clear tokens and redirect to login
          console.log('[API] Token refresh failed, clearing auth data');
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
    console.log('[API] GET request:', url);
    const response = await this.client.get<T>(url, config);
    console.log('[API] GET response:', response.status, url);
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
}

export const apiClient = new ApiClient();
