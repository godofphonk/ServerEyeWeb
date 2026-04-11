import { apiClient } from '@/lib/api';

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  refreshToken?: string;
  user: {
    id: string;
    email: string;
    name?: string;
  };
}

export async function login(credentials: LoginCredentials): Promise<LoginResponse> {
  const response = await apiClient.post<LoginResponse>('/users/login', credentials);

  // Store tokens in HttpOnly cookies via session API
  if (response.token) {
    await fetch('/api/auth/session', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ token: response.token, refreshToken: response.refreshToken }),
    });
  }

  return response;
}

export async function logout(): Promise<void> {
  // Cookies are cleared by the server on logout
  await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' });
}

export function getToken(): string | null {
  // Tokens are stored in HttpOnly cookies, managed server-side
  return null;
}

export function isAuthenticated(): boolean {
  // Authentication is checked via session API
  return false;
}

export interface User {
  id: string;
  email: string;
  name?: string;
  role?: string;
}

export function isAdmin(user: User | null | undefined): boolean {
  if (!user) return false;

  return String(user.role).toLowerCase() === 'admin';
}
