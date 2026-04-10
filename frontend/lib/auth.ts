import { apiClient } from '@/lib/api';

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  user: {
    id: string;
    email: string;
    name?: string;
  };
}

export async function login(credentials: LoginCredentials): Promise<LoginResponse> {
  const response = await apiClient.post<LoginResponse>('/users/login', credentials);

  // Dev environment: store token in localStorage
  if (process.env.NODE_ENV === 'development' && response.token) {
    localStorage.setItem('jwt_token', response.token);
  }

  return response;
}

export async function logout(): Promise<void> {
  // Clear tokens
  if (process.env.NODE_ENV === 'development') {
    localStorage.removeItem('jwt_token');
    localStorage.removeItem('refresh_token');
  }

  // Cookies are cleared by the server on logout
  await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' });
}

export function getToken(): string | null {
  // Dev environment: use localStorage
  if (process.env.NODE_ENV === 'development') {
    return localStorage.getItem('jwt_token');
  }

  // Production: tokens are stored in HttpOnly cookies
  return null;
}

export function isAuthenticated(): boolean {
  // Dev environment: check localStorage
  if (process.env.NODE_ENV === 'development') {
    return !!localStorage.getItem('jwt_token');
  }

  // Production: authentication is checked via session API
  return false;
}

export function isAdmin(user: any): boolean {
  if (!user) return false;

  return String(user.role).toLowerCase() === 'admin';
}
