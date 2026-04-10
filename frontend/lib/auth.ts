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
  // Token is stored in HttpOnly cookies by the server
  return response;
}

export async function logout(): Promise<void> {
  // Cookies are cleared by the server on logout
  await fetch('/api/auth/logout', { method: 'POST', credentials: 'include' });
}

export function getToken(): string | null {
  // Tokens are now stored in HttpOnly cookies and not accessible from client-side JS
  return null;
}

export function isAuthenticated(): boolean {
  // Authentication is now checked via session API, not client-side token
  return false;
}

export function isAdmin(user: any): boolean {
  if (!user) return false;

  return String(user.role).toLowerCase() === 'admin';
}
