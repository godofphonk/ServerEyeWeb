import { getToken, isAuthenticated, isAdmin, logout } from '@/lib/auth';

jest.mock('../../lib/api', () => ({
  apiClient: {
    post: jest.fn(),
  },
}));

global.fetch = jest.fn();

describe('getToken', () => {
  it('returns null (tokens are stored in HttpOnly cookies)', () => {
    expect(getToken()).toBeNull();
  });
});

describe('isAuthenticated', () => {
  it('returns false (authentication is checked via session API)', () => {
    expect(isAuthenticated()).toBe(false);
  });
});

describe('logout', () => {
  beforeEach(() => {
    (global.fetch as jest.Mock).mockClear();
  });

  it('calls logout API endpoint', async () => {
    (global.fetch as jest.Mock).mockResolvedValue({ ok: true });
    await logout();
    expect(global.fetch).toHaveBeenCalledWith('/api/auth/logout', {
      method: 'POST',
      credentials: 'include',
    });
  });

  it('handles API call errors gracefully', async () => {
    (global.fetch as jest.Mock).mockRejectedValue(new Error('Network error'));
    await expect(logout()).rejects.toThrow('Network error');
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
    expect(isAdmin({ id: '1', email: 'admin@example.com', role: 'admin' })).toBe(true);
  });

  it('returns true for user with Admin role (capitalized)', () => {
    expect(isAdmin({ id: '1', email: 'admin@example.com', role: 'Admin' })).toBe(true);
  });

  it('returns true for user with ADMIN role (uppercase)', () => {
    expect(isAdmin({ id: '1', email: 'admin@example.com', role: 'ADMIN' })).toBe(true);
  });

  it('returns false for user with user role', () => {
    expect(isAdmin({ id: '1', email: 'user@example.com', role: 'user' })).toBe(false);
  });

  it('returns false for user with no role', () => {
    expect(isAdmin({ id: '1', email: 'user@example.com' })).toBe(false);
  });

  it('returns false for user with empty role', () => {
    expect(isAdmin({ id: '1', email: 'user@example.com', role: '' })).toBe(false);
  });
});
