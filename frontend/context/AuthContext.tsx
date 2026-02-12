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

  const mapBackendUser = (backendUser: BackendUser): User => ({
    id: backendUser.id,
    email: backendUser.email,
    username: backendUser.userName,
    role: 'user',
    createdAt: new Date().toISOString(),
  });

  const checkAuth = useCallback(async () => {
    try {
      const res = await fetch('/api/auth/session', { credentials: 'include' });
      if (res.ok) {
        const data = await res.json();
        if (data.user) {
          setUser(mapBackendUser(data.user));
          return;
        }
      }
      setUser(null);
    } catch {
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
    
    const loginUrl = '/api/auth/login2?t=' + Date.now();
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
