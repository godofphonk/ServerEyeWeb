'use client';

import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  useRef,
  ReactNode,
} from 'react';
import {
  User,
  BackendUser,
  OAuthChallengeResponse,
  ExternalLoginsResponse,
  LinkOAuthRequest,
  ExternalLogin,
} from '@/types';
import { apiClient } from '@/lib/api';
import { logger } from '@/lib/telemetry/logger';

interface AuthContextType {
  user: User | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  clearAuthData: () => void;
  setTokensFromCallback: (token: string, refreshToken: string) => Promise<void>;
  refreshUserData: () => Promise<void>;
  refreshToken: () => Promise<void>;
  loginWithOAuth: (provider: string, code: string, state: string) => Promise<void>;
  getOAuthURL: (provider: string, action?: string) => Promise<string>;
  getOAuthChallenge: (
    provider: string,
    returnUrl?: string,
    action?: string,
  ) => Promise<OAuthChallengeResponse>;
  getExternalLogins: () => Promise<ExternalLoginsResponse>;
  linkExternalAccount: (provider: string, code: string, state: string) => Promise<void>;
  unlinkExternalAccount: (provider: string) => Promise<void>;
  isAuthenticated: boolean;
  checkAuth: () => Promise<void>;
  isEmailVerified: boolean | string;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);

  // Wrap setUser to add logging
  const setUserWithLogging = useCallback((newUser: User | null) => {
    setUser(newUser);
  }, []);
  const [loading, setLoading] = useState(true);
  const checkAuthCalled = useRef(false); // Отслеживаем был ли вызван checkAuth

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
      isEmailVerified: Boolean(backendUser.isEmailVerified),
      hasPassword: backendUser.hasPassword ?? true, // По умолчанию true для обратной совместимости
    };

    return user;
  };

  const checkAuth = useCallback(async () => {
    try {
      logger.debug('Checking authentication status');
      setLoading(true);

      // First check if we have tokens in localStorage (from OAuth callback)
      if (typeof window !== 'undefined') {
        const token = localStorage.getItem('jwt_token') || localStorage.getItem('access_token');

        if (token && !user) {
          try {
            const payload = JSON.parse(atob(token.split('.')[1]));

            const userId = payload.sub || payload.nameid || payload.userId || payload.id;
            const email = payload.email || payload.Email || '';
            const username =
              payload.username ||
              payload.UserName ||
              payload.name ||
              payload.unique_name ||
              email.split('@')[0] ||
              'user';
            const role = payload.role || payload.Role || 'user';
            // OAuth пользователи обычно не имеют email или email пустой
            const isOAuthUser =
              !email ||
              email.trim() === '' ||
              email.includes('telegram.local') ||
              email.includes('@oauth.');
            const hasPassword = payload.hasPassword ?? payload.HasPassword ?? !isOAuthUser;

            // Clear OAuth tokens from localStorage if user is not actually OAuth user
            if (email.includes('telegram.local') || email.includes('@oauth.')) {
              clearAuthData();
              throw new Error('OAuth token detected, clearing and using session API');
            }

            if (userId) {
              const localStorageUser: User = {
                id: userId,
                email: email,
                username: username || email.split('@')[0] || 'user',
                role: role as 'user' | 'admin',
                createdAt: new Date().toISOString(),
                isEmailVerified:
                  payload.email_verified === 'TRUE' || payload.email_verified === true,
                hasPassword: hasPassword,
              };

              setUserWithLogging(localStorageUser);
              setLoading(false);
              return;
            }
          } catch (decodeError) {}
        }
      }

      // Always try session API as fallback or if no user found

      const res = await fetch('/api/auth/session', { credentials: 'include' });

      if (res.ok) {
        const data = await res.json();

        if (data.user) {
          const mappedUser = mapBackendUser(data.user);
          setUserWithLogging(mappedUser);

          // Also save token to localStorage for apiClient
          if (typeof window !== 'undefined') {
            // Get token from session API response
            try {
              const tokenResponse = await fetch('/api/auth/token', { credentials: 'include' });
              if (tokenResponse.ok) {
                const tokenData = await tokenResponse.json();
                if (tokenData.token) {
                  localStorage.setItem('jwt_token', tokenData.token);
                }
              }
            } catch (error) {}
          }
          return;
        }
      } else {
      }

      clearAuthData();
    } catch (error) {
      logger.warn('Authentication check failed', {
        error: error instanceof Error ? error.message : 'Unknown error',
      });
      setUserWithLogging(null);
    } finally {
      setLoading(false);
    }
  }, [setUserWithLogging]); // Убираем зависимость user чтобы избежать бесконечного цикла

  useEffect(() => {
    checkAuth();
    checkAuthCalled.current = true; // Устанавливаем флаг после вызова
  }, []); // Пустые зависимости - вызывается только при монтировании

  const login = async (email: string, password: string) => {
    const loginUrl = '/api/users/login?t=' + Date.now();

    const res = await fetch(loginUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Cache-Control': 'no-cache, no-store, must-revalidate',
        Pragma: 'no-cache',
        Expires: '0',
        'X-Auth-Bypass': Date.now().toString(),
      },
      credentials: 'include',
      body: JSON.stringify({ email, password }),
    });

    if (!res.ok) {
      const errorData = await res.json().catch(() => ({}));
      throw new Error(errorData.message || 'Login failed');
    }

    const data = await res.json();

    if (!data.user?.id) {
      throw new Error('Invalid login response');
    }

    setUserWithLogging(mapBackendUser(data.user));
    checkAuthCalled.current = false; // Сбрасываем флаг после успешного входa

    // Also save token to localStorage for apiClient
    if (typeof window !== 'undefined') {
      const cookies = document.cookie.split(';');
      const accessTokenCookie = cookies.find(c => c.trim().startsWith('access_token='));
      if (accessTokenCookie) {
        const token = accessTokenCookie.split('=')[1];
        localStorage.setItem('jwt_token', token);
      }
    }
  };

  const setTokensFromCallback = async (token: string, refreshToken: string) => {
    // Store tokens in localStorage for apiClient
    if (typeof window !== 'undefined') {
      localStorage.setItem('jwt_token', token);
      localStorage.setItem('access_token', token);
      localStorage.setItem('refresh_token', refreshToken);

      // Also try to set cookies for backend compatibility
      document.cookie = `access_token=${token}; path=/; max-age=3600; SameSite=Lax`;
      document.cookie = `refresh_token=${refreshToken}; path=/; max-age=604800; SameSite=Lax`;
    }

    // Decode JWT token to get user info
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));

      // Handle different claim formats
      const userId = payload.sub || payload.nameid || payload.userId || payload.id;
      const email = payload.email || payload.Email;
      const username = payload.username || payload.UserName || payload.name || payload.unique_name;
      const role = payload.role || payload.Role || 'user';

      // For OAuth users, email can be empty but userId is required
      if (userId) {
        const user: User = {
          id: userId,
          email: email || '', // Allow empty email for OAuth users
          username: username || 'oauth_user',
          role: role as 'user' | 'admin',
          createdAt: new Date().toISOString(),
          isEmailVerified: payload.email_verified === 'TRUE' || payload.email_verified === true,
          hasPassword: false, // OAuth users don't have passwords
        };

        setUserWithLogging(user);
        return; // Success, don't try fallback
      } else {
      }
    } catch (error) {
      // Fallback: try to get user data from API
      try {
        const response = await apiClient.get<{ user: BackendUser | null }>('/auth/session');

        if ((response as any).data.user) {
          const mappedUser = mapBackendUser((response as any).data.user);
          logger.info('User authenticated', { userId: mappedUser.id, email: mappedUser.email });
          setUserWithLogging(mappedUser);
        } else {
          logger.debug('No active session found');
          setUserWithLogging(null);
        }
      } catch (fallbackError) {}
    }

    // If we get here, both JWT decode and API fallback failed
    ('AuthContext setTokensFromCallback - both JWT decode and API fallback failed');
    throw new Error('Failed to authenticate user');
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

    setUserWithLogging(mapBackendUser(data.user));
  };

  const clearAuthData = () => {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('jwt_token');
      localStorage.removeItem('refresh_token');
      // Clear all cookies with comprehensive path and domain coverage
      document.cookie.split(';').forEach(c => {
        const cookie = c.trim();
        const eqPos = cookie.indexOf('=');
        const name = eqPos > -1 ? cookie.substr(0, eqPos) : cookie;

        // Clear cookie with all possible combinations
        document.cookie = `${name}=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/;domain=${window.location.hostname};`;
        document.cookie = `${name}=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/;domain=.${window.location.hostname};`;
        document.cookie = `${name}=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/;`;
        document.cookie = `${name}=;expires=Thu, 01 Jan 1970 00:00:00 GMT;`;
      });
    }
    setUserWithLogging(null);
    checkAuthCalled.current = false; // Сбрасываем флаг для возможности повторного входа
  };

  const refreshUserData = async () => {
    await checkAuth();
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
      clearAuthData();
      // Force re-check auth state after logout
      checkAuthCalled.current = false;
      setLoading(false);
    }
  };

  const refreshTokens = useCallback(async () => {
    await checkAuth();
  }, []); // Убираем зависимость checkAuth

  // New OAuth methods using the updated endpoints
  const getOAuthChallenge = useCallback(
    async (
      provider: string,
      returnUrl?: string,
      action?: string,
    ): Promise<OAuthChallengeResponse> => {
      const params = new URLSearchParams();
      if (returnUrl) params.append('returnUrl', returnUrl);
      if (action) params.append('action', action);

      const response = await apiClient.get<OAuthChallengeResponse>(
        `/auth/oauth/${provider}/challenge?${params.toString()}`,
      );

      return response;
    },
    [],
  );

  const getExternalLogins = useCallback(async (): Promise<ExternalLoginsResponse> => {
    const response = await apiClient.get<ExternalLogin[]>('/auth/oauth/providers');
    // Backend возвращает массив напрямую, оборачиваем в ожидаемую структуру
    return { externalLogins: response };
  }, []);

  const linkExternalAccount = useCallback(
    async (provider: string, code: string, state: string): Promise<void> => {
      await apiClient.post<LinkOAuthRequest>('/auth/oauth/link', {
        provider,
        code,
        state,
      });
    },
    [],
  );

  const unlinkExternalAccount = useCallback(async (provider: string): Promise<void> => {
    await apiClient.delete(`/auth/oauth/${provider}`);
  }, []);

  // Legacy OAuth methods (updated to use new challenge flow)
  const getOAuthURL = useCallback(
    async (provider: string, action?: string): Promise<string> => {
      const challenge = await getOAuthChallenge(provider, undefined, action);
      // Store challenge data in sessionStorage for callback handling
      if (typeof window !== 'undefined') {
        sessionStorage.setItem('oauth_state', challenge.state);
        sessionStorage.setItem('oauth_code_verifier', challenge.codeVerifier);
        sessionStorage.setItem('oauth_provider', provider);
        if (action) {
          sessionStorage.setItem('oauth_action', action);
          // For Telegram, also store action with provider prefix for special handling
          if (provider.toLowerCase() === 'telegram') {
            sessionStorage.setItem('telegram_oauth_action', action);
          }
        }
      }

      return challenge.challengeUrl;
    },
    [getOAuthChallenge],
  );

  const loginWithOAuth = useCallback(
    async (provider: string, code: string, state: string): Promise<void> => {
      // This method is now handled by the backend callback endpoint
      // Frontend just needs to redirect to the OAuth URL
      const challenge = await getOAuthChallenge(provider);
      window.location.href = challenge.challengeUrl;
    },
    [getOAuthChallenge],
  );

  const isEmailVerifiedValue = Boolean(user?.isEmailVerified);

  return (
    <AuthContext.Provider
      value={{
        user,
        loading,
        login,
        register,
        logout,
        clearAuthData,
        setTokensFromCallback,
        refreshUserData,
        refreshToken: refreshTokens,
        loginWithOAuth,
        getOAuthURL,
        getOAuthChallenge,
        getExternalLogins,
        linkExternalAccount,
        unlinkExternalAccount,
        isAuthenticated: !!user,
        checkAuth,
        isEmailVerified: isEmailVerifiedValue,
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
