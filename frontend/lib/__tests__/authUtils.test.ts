import { hasUserAccess, shouldShowEmailVerificationBanner, isOAuthUser } from '../authUtils';
import { User } from '@/types';

const makeUser = (overrides: Partial<User> = {}): User => ({
  id: '1',
  email: 'test@example.com',
  username: 'testuser',
  role: 'user',
  createdAt: new Date().toISOString(),
  ...overrides,
});

describe('hasUserAccess', () => {
  it('should return false when user is null', () => {
    expect(hasUserAccess(null, true)).toBe(false);
  });

  it('should return true for OAuth user regardless of email verification', () => {
    const user = makeUser({ hasPassword: false });
    expect(hasUserAccess(user, false)).toBe(true);
    expect(hasUserAccess(user, true)).toBe(true);
  });

  it('should return true for regular user with verified email (boolean true)', () => {
    const user = makeUser({ hasPassword: true });
    expect(hasUserAccess(user, true)).toBe(true);
  });

  it('should return false for regular user without verified email (boolean false)', () => {
    const user = makeUser({ hasPassword: true });
    expect(hasUserAccess(user, false)).toBe(false);
  });

  it('should return true for regular user with truthy string email verification', () => {
    const user = makeUser({ hasPassword: true });
    expect(hasUserAccess(user, 'true')).toBe(true);
  });

  it('should return false for regular user with empty string email verification', () => {
    const user = makeUser({ hasPassword: true });
    expect(hasUserAccess(user, '')).toBe(false);
  });

  it('should return true for user with undefined hasPassword (no password field) and verified email', () => {
    const user = makeUser({ hasPassword: undefined });
    expect(hasUserAccess(user, true)).toBe(true);
  });

  it('should return false for user with undefined hasPassword and unverified email', () => {
    const user = makeUser({ hasPassword: undefined });
    expect(hasUserAccess(user, false)).toBe(false);
  });
});

describe('shouldShowEmailVerificationBanner', () => {
  it('should return false when user is null', () => {
    expect(shouldShowEmailVerificationBanner(null, false)).toBe(false);
  });

  it('should return false for OAuth user regardless of email verification', () => {
    const user = makeUser({ hasPassword: false });
    expect(shouldShowEmailVerificationBanner(user, false)).toBe(false);
    expect(shouldShowEmailVerificationBanner(user, true)).toBe(false);
  });

  it('should return true for regular user with email but unverified', () => {
    const user = makeUser({ hasPassword: true, email: 'test@example.com' });
    expect(shouldShowEmailVerificationBanner(user, false)).toBe(true);
  });

  it('should return false for regular user with email but already verified', () => {
    const user = makeUser({ hasPassword: true, email: 'test@example.com' });
    expect(shouldShowEmailVerificationBanner(user, true)).toBe(false);
  });

  it('should return false for regular user with empty email', () => {
    const user = makeUser({ hasPassword: true, email: '' });
    expect(shouldShowEmailVerificationBanner(user, false)).toBe(false);
  });

  it('should return false for regular user with whitespace-only email', () => {
    const user = makeUser({ hasPassword: true, email: '   ' });
    expect(shouldShowEmailVerificationBanner(user, false)).toBe(false);
  });

  it('should handle truthy string as verified', () => {
    const user = makeUser({ hasPassword: true, email: 'test@example.com' });
    expect(shouldShowEmailVerificationBanner(user, 'verified')).toBe(false);
  });

  it('should handle empty string as unverified', () => {
    const user = makeUser({ hasPassword: true, email: 'test@example.com' });
    expect(shouldShowEmailVerificationBanner(user, '')).toBe(true);
  });
});

describe('isOAuthUser', () => {
  it('should return false when user is null', () => {
    expect(isOAuthUser(null)).toBe(false);
  });

  it('should return true when hasPassword is false', () => {
    const user = makeUser({ hasPassword: false });
    expect(isOAuthUser(user)).toBe(true);
  });

  it('should return false when hasPassword is true', () => {
    const user = makeUser({ hasPassword: true });
    expect(isOAuthUser(user)).toBe(false);
  });

  it('should return false when hasPassword is undefined', () => {
    const user = makeUser({ hasPassword: undefined });
    expect(isOAuthUser(user)).toBe(false);
  });
});
