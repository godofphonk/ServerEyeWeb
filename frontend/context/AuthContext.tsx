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
  ExternalLogin
} from '@/types';
import { apiClient } from '@/lib/api';

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
  getOAuthChallenge: (provider: string, returnUrl?: string, action?: string) => Promise<OAuthChallengeResponse>;
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
    console.log('[AuthContext] setUser called with:', newUser ? `user: ${newUser.email || newUser.username} (${newUser.id})` : 'null');
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

    console.log('[AuthContext] mapBackendUser - Backend user:', {
      id: backendUser.id,
      email: backendUser.email,
      userName: backendUser.userName,
      role: backendUser.role,
      isEmailVerified: backendUser.isEmailVerified,
      hasPassword: backendUser.hasPassword,
    });
    console.log('[AuthContext] mapBackendUser - Mapped user:', {
      id: user.id,
      email: user.email,
      username: user.username,
      role: user.role,
      isEmailVerified: user.isEmailVerified,
      hasPassword: user.hasPassword,
    });

    return user;
  };

  const checkAuth = useCallback(async () => {
    try {
      console.log('[AuthContext] checkAuth called');
      console.log('[AuthContext] Current state:', { user: !!user, loading });
      
      setLoading(true);
      
      // First check if we have tokens in localStorage (from OAuth callback)
      if (typeof window !== 'undefined') {
        console.log('[AuthContext] Checking localStorage...');
        const token = localStorage.getItem('jwt_token') || localStorage.getItem('access_token');
        console.log('[AuthContext] Found token:', !!token, token ? token.substring(0, 20) + '...' : 'none');
        
        if (token && !user) {
          console.log('[AuthContext] Found token in localStorage, attempting to decode user');
          try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            console.log('[AuthContext] Decoded payload:', payload);
            
            const userId = payload.sub || payload.nameid || payload.userId || payload.id;
            const email = payload.email || payload.Email || '';
            const username = payload.username || payload.UserName || payload.name || payload.unique_name || email.split('@')[0] || 'user';
            const role = payload.role || payload.Role || 'user';
            // OAuth пользователи обычно не имеют email или email пустой
            const isOAuthUser = !email || email.trim() === '' || email.includes('telegram.local') || email.includes('@oauth.');
            const hasPassword = payload.hasPassword ?? payload.HasPassword ?? !isOAuthUser;
            
            console.log('[AuthContext] Extracted claims:', { userId, email, username, role, hasPassword, isOAuthUser });
            console.log('[AuthContext] Token source check - email_verified:', payload.email_verified);
            console.log('[AuthContext] This looks like OAuth token:', isOAuthUser);
            
            // Clear OAuth tokens from localStorage if user is not actually OAuth user
            if (email.includes('telegram.local') || email.includes('@oauth.')) {
              console.log('[AuthContext] Detected OAuth token in localStorage, clearing it...');
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
                isEmailVerified: payload.email_verified === 'TRUE' || payload.email_verified === true,
                hasPassword: hasPassword,
              };
              
              setUserWithLogging(localStorageUser);
              console.log('[AuthContext] User restored from localStorage token:', localStorageUser);
              setLoading(false);
              return;
            }
          } catch (decodeError) {
            console.log('[AuthContext] Failed to decode localStorage token:', decodeError);
          }
        }
      }
      
      // Always try session API as fallback or if no user found
      console.log('[AuthContext] Trying session API as fallback');
      console.log('[AuthContext] Available cookies:', document.cookie);
      
      const res = await fetch('/api/auth/session', { credentials: 'include' });
      console.log('[AuthContext] Session API response status:', res.status);
      console.log('[AuthContext] Session API response headers:', Object.fromEntries(res.headers.entries()));
      
      if (res.ok) {
        const data = await res.json();
        console.log('[AuthContext] Session API response data:', data);
        
        if (data.user) {
          const mappedUser = mapBackendUser(data.user);
          setUserWithLogging(mappedUser);
          console.log('[AuthContext] User authenticated via session:', mappedUser);
          console.log('[AuthContext] Email verification status:', mappedUser.isEmailVerified);
          console.log(
            '[AuthContext] isEmailVerified computed:',
            mappedUser?.isEmailVerified || false,
          );

          // Also save token to localStorage for apiClient
          if (typeof window !== 'undefined') {
            // Get token from session API response
            try {
              const tokenResponse = await fetch('/api/auth/token', { credentials: 'include' });
              if (tokenResponse.ok) {
                const tokenData = await tokenResponse.json();
                if (tokenData.token) {
                  localStorage.setItem('jwt_token', tokenData.token);
                  console.log(
                    '[AuthContext] Token saved to localStorage from API, length:',
                    tokenData.token?.length,
                  );
                }
              }
            } catch (error) {
              console.log('[AuthContext] Failed to get token from API:', error);
            }
          }
          return;
        }
      } else {
        console.log('[AuthContext] Session API returned status:', res.status);
        console.log('[AuthContext] Session API response text:', await res.text());
      }
      
      console.log('[AuthContext] No valid session found');
      clearAuthData();
    } catch (error) {
      console.log('[AuthContext] checkAuth error:', error);
      clearAuthData();
    } finally {
      setLoading(false);
    }
  }, []); // Убираем зависимость user чтобы избежать бесконечного цикла

  useEffect(() => {
    console.log('[AuthContext] useEffect called - checkAuthCalled.current:', checkAuthCalled.current);
    checkAuth();
    checkAuthCalled.current = true; // Устанавливаем флаг после вызова
  }, []); // Пустые зависимости - вызывается только при монтировании

  const login = async (email: string, password: string) => {
    console.log('AuthContext login - attempting login with:', {
      email,
      passwordLength: password.length,
    });

    const loginUrl = '/api/auth/login?t=' + Date.now();
    console.log('AuthContext login - full URL:', window.location.origin + loginUrl);

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

    console.log('AuthContext login - response status:', res.status, res.statusText);
    console.log('AuthContext login - response headers:', Object.fromEntries(res.headers.entries()));

    if (!res.ok) {
      const errorData = await res.json().catch(() => ({}));
      console.log('AuthContext login - error response:', errorData);
      throw new Error(errorData.message || 'Login failed');
    }

    const data = await res.json();
    console.log('AuthContext login - success response:', {
      hasUser: !!data.user,
      userId: data.user?.id,
    });

    if (!data.user?.id) {
      throw new Error('Invalid login response');
    }

    setUserWithLogging(mapBackendUser(data.user));
    console.log('AuthContext login - user set successfully');
    console.log('AuthContext login - mapped user:', mapBackendUser(data.user));
    checkAuthCalled.current = false; // Сбрасываем флаг после успешного входa

    // Also save token to localStorage for apiClient
    if (typeof window !== 'undefined') {
      const cookies = document.cookie.split(';');
      const accessTokenCookie = cookies.find(c => c.trim().startsWith('access_token='));
      if (accessTokenCookie) {
        const token = accessTokenCookie.split('=')[1];
        localStorage.setItem('jwt_token', token);
        console.log('AuthContext login - token saved to localStorage');
      }
    }
  };

  const setTokensFromCallback = async (token: string, refreshToken: string) => {
    console.log('AuthContext setTokensFromCallback - setting tokens from OAuth callback');
    
    // Store tokens in localStorage for apiClient
    if (typeof window !== 'undefined') {
      localStorage.setItem('jwt_token', token);
      localStorage.setItem('access_token', token);
      localStorage.setItem('refresh_token', refreshToken);
      console.log('AuthContext setTokensFromCallback - tokens saved to localStorage');
      
      // Also try to set cookies for backend compatibility
      document.cookie = `access_token=${token}; path=/; max-age=3600; SameSite=Lax`;
      document.cookie = `refresh_token=${refreshToken}; path=/; max-age=604800; SameSite=Lax`;
      console.log('AuthContext setTokensFromCallback - cookies set');
    }

    // Decode JWT token to get user info
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      console.log('AuthContext setTokensFromCallback - decoded payload:', payload);
      
      // Handle different claim formats
      const userId = payload.sub || payload.nameid || payload.userId || payload.id;
      const email = payload.email || payload.Email;
      const username = payload.username || payload.UserName || payload.name || payload.unique_name;
      const role = payload.role || payload.Role || 'user';
      
      console.log('AuthContext setTokensFromCallback - extracted claims:', {
        userId,
        email,
        username,
        role
      });
      
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
        console.log('AuthContext setTokensFromCallback - user set from OAuth:', user);
        return; // Success, don't try fallback
      } else {
        console.log('AuthContext setTokensFromCallback - missing required claims:', {
          userId,
          email,
          username,
          role
        });
      }
    } catch (error) {
      console.error('AuthContext setTokensFromCallback - failed to decode token:', error);
      console.log('AuthContext setTokensFromCallback - token structure:', token.split('.')[0], '...', token.split('.')[2]);
      
      // Fallback: try to get user data from API
      try {
        console.log('AuthContext setTokensFromCallback - trying API fallback...');
        const res = await fetch('/api/auth/session', { credentials: 'include' });
        if (res.ok) {
          const data = await res.json();
          console.log('AuthContext setTokensFromCallback - API response:', data);
          if (data.user) {
            const mappedUser = mapBackendUser(data.user);
            setUserWithLogging(mappedUser);
            console.log('AuthContext setTokensFromCallback - user set from API fallback:', mappedUser);
            return;
          }
        } else {
          console.log('AuthContext setTokensFromCallback - API fallback failed with status:', res.status);
        }
      } catch (fallbackError) {
        console.error('AuthContext setTokensFromCallback - fallback failed:', fallbackError);
      }
    }
    
    // If we get here, both JWT decode and API fallback failed
    console.error('AuthContext setTokensFromCallback - both JWT decode and API fallback failed');
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
    console.log('[AuthContext] Clearing auth data');
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
    console.log('[AuthContext] refreshUserData called');
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
  const getOAuthChallenge = useCallback(async (provider: string, returnUrl?: string, action?: string): Promise<OAuthChallengeResponse> => {
    const params = new URLSearchParams();
    if (returnUrl) params.append('returnUrl', returnUrl);
    if (action) params.append('action', action);
    
    const response = await apiClient.get<OAuthChallengeResponse>(
      `/auth/oauth/${provider}/challenge?${params.toString()}`
    );
    
    console.log('[AuthContext] OAuth challenge response:', response);
    console.log('[AuthContext] Using challenge URL from backend:', response.challengeUrl);
    console.log('[AuthContext] Action:', action || 'auto');
    
    return response;
  }, []);

  const getExternalLogins = useCallback(async (): Promise<ExternalLoginsResponse> => {
    const response = await apiClient.get<ExternalLogin[]>('/auth/oauth/providers');
    // Backend возвращает массив напрямую, оборачиваем в ожидаемую структуру
    return { externalLogins: response };
  }, []);

  const linkExternalAccount = useCallback(async (provider: string, code: string, state: string): Promise<void> => {
    await apiClient.post<LinkOAuthRequest>('/auth/oauth/link', {
      provider,
      code,
      state,
    });
  }, []);

  const unlinkExternalAccount = useCallback(async (provider: string): Promise<void> => {
    await apiClient.delete(`/auth/oauth/${provider}`);
  }, []);

  // Legacy OAuth methods (updated to use new challenge flow)
  const getOAuthURL = useCallback(async (provider: string, action?: string): Promise<string> => {
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
  }, [getOAuthChallenge]);

  const loginWithOAuth = useCallback(async (provider: string, code: string, state: string): Promise<void> => {
    // This method is now handled by the backend callback endpoint
    // Frontend just needs to redirect to the OAuth URL
    const challenge = await getOAuthChallenge(provider);
    window.location.href = challenge.challengeUrl;
  }, [getOAuthChallenge]);

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
