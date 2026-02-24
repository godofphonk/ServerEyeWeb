"use client";

import React, { createContext, useContext, useState, useEffect, useCallback, ReactNode } from 'react';
import { User, BackendUser } from '@/types';
import { apiClient } from "@/lib/api";

interface AuthContextType {
  user: User | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshToken: () => Promise<void>;
  loginWithOAuth: (provider: string, code: string, state: string) => Promise<void>;
  getOAuthURL: (provider: string) => Promise<string>;
  isAuthenticated: boolean;
  checkAuth: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  const mapBackendUser = (backendUser: BackendUser): User => {
    // Convert backend role to frontend format
    let role: 'user' | 'admin' = 'user';
    if (backendUser.role) {
      const backendRole = String(backendUser.role).toLowerCase();
      role = backendRole === 'admin' ? 'admin' : 'user';
    }
    
    const user = {
      id: backendUser.id,
      email: backendUser.email,
      username: backendUser.userName,
      role: role,
      createdAt: new Date().toISOString(),
    };
    return user;
  };

  const checkAuth = useCallback(async () => {
    try {
      console.log('[AuthContext] checkAuth called');
      const res = await fetch('/api/auth/session', { credentials: 'include' });
      console.log('[AuthContext] checkAuth response:', res.status);
      if (res.ok) {
        const data = await res.json();
        console.log('[AuthContext] checkAuth data:', data);
        if (data.user) {
          setUser(mapBackendUser(data.user));
          console.log('[AuthContext] User authenticated via session');
          
          // Also save token to localStorage for apiClient
          if (typeof window !== 'undefined') {
            // Get token from session API response
            try {
              const tokenResponse = await fetch('/api/auth/token', { credentials: 'include' });
              if (tokenResponse.ok) {
                const tokenData = await tokenResponse.json();
                if (tokenData.token) {
                  localStorage.setItem('jwt_token', tokenData.token);
                  console.log('[AuthContext] Token saved to localStorage from API, length:', tokenData.token?.length);
                }
              }
            } catch (error) {
              console.log('[AuthContext] Failed to get token from API:', error);
            }
          }
          return;
        }
      }
      console.log('[AuthContext] No valid session found');
      setUser(null);
    } catch (error) {
      console.log('[AuthContext] checkAuth error:', error);
      setUser(null);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    checkAuth();
  }, [checkAuth]);

  const login = async (email: string, password: string) => {
    console.log('AuthContext login - attempting login with:', { email, passwordLength: password.length });
    
    const loginUrl = '/api/auth/login?t=' + Date.now();
    console.log('AuthContext login - full URL:', window.location.origin + loginUrl);
    
    const res = await fetch(loginUrl, {
      method: 'POST',
      headers: { 
        'Content-Type': 'application/json',
        'Cache-Control': 'no-cache, no-store, must-revalidate',
        'Pragma': 'no-cache',
        'Expires': '0',
        'X-Auth-Bypass': Date.now().toString()
      },
      credentials: 'include',
      body: JSON.stringify({ email, password }),
    });

    console.log('AuthContext login - response status:', res.status, res.statusText);
    console.log('AuthContext login - response headers:', Object.fromEntries(res.headers.entries()));

    if (!res.ok) {
      const errorData = await res.json().catch(() => ({}));
      console.log('AuthContext login - error response:', errorData);
      throw new Error(errorData.message || 'Login failed');
    }

    const data = await res.json();
    console.log('AuthContext login - success response:', { hasUser: !!data.user, userId: data.user?.id });
    
    if (!data.user?.id) {
      throw new Error('Invalid login response');
    }

    setUser(mapBackendUser(data.user));
    console.log('AuthContext login - user set successfully');
    
    // Also save token to localStorage for apiClient
    if (typeof window !== 'undefined') {
      const cookies = document.cookie.split(';');
      const accessTokenCookie = cookies.find(c => c.trim().startsWith('accessToken='));
      if (accessTokenCookie) {
        const token = accessTokenCookie.split('=')[1];
        localStorage.setItem('jwt_token', token);
        console.log('AuthContext login - token saved to localStorage');
      }
    }
  };

  const register = async (email: string, username: string, password: string) => {
    const res = await fetch('/api/auth/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ userName: username, email, password }),
    });

    if (!res.ok) {
      const errorData = await res.json().catch(() => ({}));
      throw new Error(errorData.message || 'Registration failed');
    }

    const data = await res.json();
    if (!data.user?.id) {
      throw new Error('Invalid registration response');
    }

    setUser(mapBackendUser(data.user));
  };

  const logout = async () => {
    try {
      await fetch('/api/auth/logout', {
        method: 'POST',
        credentials: 'include',
      });
    } catch {
      // Ignore logout API errors
    } finally {
      setUser(null);
    }
  };

  const refreshTokens = useCallback(async () => {
    await checkAuth();
  }, [checkAuth]);

  const loginWithOAuth = async (provider: string, code: string, state: string) => {
    const res = await apiClient.post<{ user: BackendUser }>(`/users/oauth/${provider}`, {
      code,
      state,
    });

    if (!res?.user?.id) {
      throw new Error('Invalid OAuth response');
    }

    setUser(mapBackendUser(res.user));
  };

  const getOAuthURL = async (provider: string): Promise<string> => {
    const res = await apiClient.get<{ url: string }>(`/users/oauth/${provider}/url`);
    return res.url;
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        loading,
        login,
        register,
        logout,
        refreshToken: refreshTokens,
        loginWithOAuth,
        getOAuthURL,
        isAuthenticated: !!user,
        checkAuth,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
