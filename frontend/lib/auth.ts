import { apiClient } from './api';

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

  // Save token to localStorage
  if (typeof window !== 'undefined' && response.token) {
    localStorage.setItem('jwt_token', response.token);
  }

  return response;
}

export async function logout(): Promise<void> {
  if (typeof window !== 'undefined') {
    localStorage.removeItem('jwt_token');
  }
}

export function getToken(): string | null {
  if (typeof window !== 'undefined') {
    return localStorage.getItem('jwt_token');
  }
  return null;
}

export function isAuthenticated(): boolean {
  return !!getToken();
}

export function isAdmin(user: any): boolean {
  if (!user) return false;

  return String(user.role).toLowerCase() === 'admin';
}
