import { getToken, isAuthenticated, isAdmin, logout } from '@/lib/auth';

jest.mock('../../lib/api', () => ({
  apiClient: {
    post: jest.fn(),
  },
}));

describe('getToken', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('returns null when no token is stored', () => {
    expect(getToken()).toBeNull();
  });

  it('returns the stored token', () => {
    localStorage.setItem('jwt_token', 'my-test-token');
    expect(getToken()).toBe('my-test-token');
  });

  it('returns null after token is removed', () => {
    localStorage.setItem('jwt_token', 'my-test-token');
    localStorage.removeItem('jwt_token');
    expect(getToken()).toBeNull();
  });
});

describe('isAuthenticated', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('returns false when no token is stored', () => {
    expect(isAuthenticated()).toBe(false);
  });

  it('returns true when a token is stored', () => {
    localStorage.setItem('jwt_token', 'my-test-token');
    expect(isAuthenticated()).toBe(true);
  });

  it('returns false after logout', () => {
    localStorage.setItem('jwt_token', 'my-test-token');
    localStorage.removeItem('jwt_token');
    expect(isAuthenticated()).toBe(false);
  });
});

describe('logout', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('removes the token from localStorage', async () => {
    localStorage.setItem('jwt_token', 'my-test-token');
    await logout();
    expect(localStorage.getItem('jwt_token')).toBeNull();
  });

  it('does not throw when no token exists', async () => {
    await expect(logout()).resolves.toBeUndefined();
  });
});

describe('isAdmin', () => {
  it('returns false for null user', () => {
    expect(isAdmin(null)).toBe(false);
  });

  it('returns false for undefined user', () => {
    expect(isAdmin(undefined)).toBe(false);
  });

  it('returns true for user with admin role (lowercase)', () => {
    expect(isAdmin({ role: 'admin' })).toBe(true);
  });

  it('returns true for user with Admin role (capitalized)', () => {
    expect(isAdmin({ role: 'Admin' })).toBe(true);
  });

  it('returns true for user with ADMIN role (uppercase)', () => {
    expect(isAdmin({ role: 'ADMIN' })).toBe(true);
  });

  it('returns false for user with user role', () => {
    expect(isAdmin({ role: 'user' })).toBe(false);
  });

  it('returns false for user with no role', () => {
    expect(isAdmin({})).toBe(false);
  });

  it('returns false for user with empty role', () => {
    expect(isAdmin({ role: '' })).toBe(false);
  });
});
