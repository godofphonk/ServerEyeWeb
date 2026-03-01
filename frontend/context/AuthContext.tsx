'use client';

import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  ReactNode,
} from 'react';
import { User, BackendUser, OAuthChallengeResponse, ExternalLoginsResponse, LinkOAuthRequest } from '@/types';
import { apiClient } from '@/lib/api';

interface AuthContextType {
  user: User | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  clearAuthData: () => void;
  refreshUserData: () => Promise<void>;
  refreshToken: () => Promise<void>;
  loginWithOAuth: (provider: string, code: string, state: string) => Promise<void>;
  getOAuthURL: (provider: string) => Promise<string>;
  getOAuthChallenge: (provider: string, returnUrl?: string) => Promise<OAuthChallengeResponse>;
  getExternalLogins: () => Promise<ExternalLoginsResponse>;
  linkExternalAccount: (provider: string, code: string, state: string) => Promise<void>;
  unlinkExternalAccount: (provider: string) => Promise<void>;
  isAuthenticated: boolean;
  checkAuth: () => Promise<void>;
  isEmailVerified: boolean;
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
      isEmailVerified: backendUser.isEmailVerified || false,
    };

    console.log('[AuthContext] mapBackendUser - Backend user:', {
      id: backendUser.id,
      email: backendUser.email,
      userName: backendUser.userName,
      role: backendUser.role,
      isEmailVerified: backendUser.isEmailVerified,
    });
    console.log('[AuthContext] mapBackendUser - Mapped user:', {
      id: user.id,
      email: user.email,
      username: user.username,
      role: user.role,
      isEmailVerified: user.isEmailVerified,
    });

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
          const mappedUser = mapBackendUser(data.user);
          setUser(mappedUser);
          console.log('[AuthContext] User authenticated via session');
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
      }
      console.log('[AuthContext] No valid session found');
      clearAuthData();
    } catch (error) {
      console.log('[AuthContext] checkAuth error:', error);
      clearAuthData();
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    checkAuth();
  }, []);

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

  const clearAuthData = () => {
    if (typeof window !== 'undefined') {
      localStorage.removeItem('jwt_token');
      localStorage.removeItem('refresh_token');
      // Clear all cookies
      document.cookie.split(';').forEach(c => {
        document.cookie = c
          .replace(/^ +/, '')
          .replace(/=.*/, '=;expires=' + new Date().toUTCString() + ';path=/');
      });
    }
    setUser(null);
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
    }
  };

  const refreshTokens = useCallback(async () => {
    await checkAuth();
  }, [checkAuth]);

  // New OAuth methods using the updated endpoints
  const getOAuthChallenge = useCallback(async (provider: string, returnUrl?: string): Promise<OAuthChallengeResponse> => {
    const params = new URLSearchParams();
    if (returnUrl) params.append('returnUrl', returnUrl);
    
    const response = await apiClient.get<OAuthChallengeResponse>(
      `/auth/oauth/${provider}/challenge?${params.toString()}`
    );
    
    console.log('[AuthContext] Using challenge URL from backend:', response.challengeUrl);
    
    return response;
  }, []);

  const getExternalLogins = useCallback(async (): Promise<ExternalLoginsResponse> => {
    const response = await apiClient.get<ExternalLoginsResponse>('/auth/oauth/external-logins');
    return response;
  }, []);

  const linkExternalAccount = useCallback(async (provider: string, code: string, state: string): Promise<void> => {
    await apiClient.post<LinkOAuthRequest>('/auth/oauth/link', {
      provider,
      code,
      state,
    });
  }, []);

  const unlinkExternalAccount = useCallback(async (provider: string): Promise<void> => {
    await apiClient.delete(`/auth/oauth/unlink/${provider}`);
  }, []);

  // Legacy OAuth methods (updated to use new challenge flow)
  const getOAuthURL = useCallback(async (provider: string): Promise<string> => {
    const challenge = await getOAuthChallenge(provider);
    // Store challenge data in sessionStorage for callback handling
    if (typeof window !== 'undefined') {
      sessionStorage.setItem('oauth_state', challenge.state);
      sessionStorage.setItem('oauth_code_verifier', challenge.codeVerifier);
      sessionStorage.setItem('oauth_provider', provider);
    }
    
    return challenge.challengeUrl;
  }, [getOAuthChallenge]);

  const loginWithOAuth = useCallback(async (provider: string, code: string, state: string): Promise<void> => {
    // This method is now handled by the backend callback endpoint
    // Frontend just needs to redirect to the OAuth URL
    const challenge = await getOAuthChallenge(provider);
    window.location.href = challenge.challengeUrl;
  }, [getOAuthChallenge]);

  const isEmailVerifiedValue = user?.isEmailVerified || false;
  console.log('[AuthContext] Provider - User:', user);
  console.log('[AuthContext] Provider - isEmailVerified:', isEmailVerifiedValue);

  return (
    <AuthContext.Provider
      value={{
        user,
        loading,
        login,
        register,
        logout,
        clearAuthData,
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
