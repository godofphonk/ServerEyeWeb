"use client";

import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { User, BackendUser, BackendAuthResponse } from '@/types';
import {apiClient} from "@/lib/api";

interface AuthContextType {
  user: User | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  loginWithOAuth: (provider: string, code: string, state: string) => Promise<void>;
  getOAuthURL: (provider: string) => Promise<string>;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [isMounted, setIsMounted] = useState(false);

  useEffect(() => {
    setIsMounted(true);
    checkAuth();
  }, []);

  const checkAuth = async () => {
    try {
      const token = localStorage.getItem('accessToken');
      if (token) {
        // Verify token with backend and get user data
        const response = await apiClient.get<BackendUser>('/users/me');
        const user: BackendUser = response;
        
        setUser({
          id: user.id,
          email: user.email,
          username: user.userName,
          role: 'user',
          createdAt: new Date().toISOString()
        });
      }
    } catch (error) {
      console.error('Auth check failed:', error);
      localStorage.removeItem('accessToken');
    } finally {
      setLoading(false);
    }
  };

  const login = async (email: string, password: string) => {
    try {
      const response = await apiClient.post<BackendAuthResponse>('/users/login', {
        email,
        login: email,
        password
      });

      const { user, token } = response;
      
      localStorage.setItem('accessToken', token);
      setUser({
        id: user.id,
        email: user.email,
        username: user.userName,
        role: 'user',
        createdAt: new Date().toISOString()
      });
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'Login failed');
    }
  };

  const register = async (email: string, username: string, password: string) => {
    try {
      const response = await apiClient.post<BackendAuthResponse>('/users/register', {
        userName: username,
        email,
        password
      });

      const { user, token } = response;
      
      localStorage.setItem('accessToken', token);
      setUser({
        id: user.id,
        email: user.email,
        username: user.userName,
        role: 'user',
        createdAt: new Date().toISOString()
      });
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'Registration failed');
    }
  };

  const logout = async () => {
    localStorage.removeItem('accessToken');
    setUser(null);
  };

  const loginWithOAuth = async (provider: string, code: string, state: string) => {
    try {
      const response = await apiClient.post<BackendAuthResponse>('/users/oauth/' + provider, {
        code,
        state
      });

      const { user, token } = response;
      
      localStorage.setItem('accessToken', token);
      setUser({
        id: user.id,
        email: user.email,
        username: user.userName,
        role: 'user',
        createdAt: new Date().toISOString()
      });
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'OAuth authentication failed');
    }
  };

  const getOAuthURL = async (provider: string): Promise<string> => {
    try {
      const response = await apiClient.get<{ url: string }>(`/users/oauth/${provider}/url`);
      return response.url;
    } catch (error: any) {
      throw new Error(error.response?.data?.message || 'Failed to get OAuth URL');
    }
  };

  // Don't render children until mounted (client-side only)
  if (!isMounted) {
    return <div className="min-h-screen bg-black flex items-center justify-center">
      <div className="text-white">Loading...</div>
    </div>;
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        loading,
        login,
        register,
        logout,
        loginWithOAuth,
        getOAuthURL,
        isAuthenticated: !!user,
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
