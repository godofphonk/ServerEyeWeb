"use client";

import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { User } from '@/types';

interface AuthContextType {
  user: User | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
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
        // TODO: Implement token validation with your backend
        // For now, just check if token exists
        setUser({
          id: '1',
          email: 'user@example.com',
          username: 'user',
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
    // TODO: Implement actual login with your backend
    // For demo purposes, simulate login
    const mockUser: User = {
      id: '1',
      email,
      username: email.split('@')[0],
      role: 'user',
      createdAt: new Date().toISOString()
    };
    
    const mockToken = 'mock-jwt-token';
    localStorage.setItem('accessToken', mockToken);
    setUser(mockUser);
  };

  const register = async (email: string, username: string, password: string) => {
    // TODO: Implement actual registration with your backend
    const mockUser: User = {
      id: '1',
      email,
      username,
      role: 'user',
      createdAt: new Date().toISOString()
    };
    
    const mockToken = 'mock-jwt-token';
    localStorage.setItem('accessToken', mockToken);
    setUser(mockUser);
  };

  const logout = async () => {
    localStorage.removeItem('accessToken');
    setUser(null);
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
