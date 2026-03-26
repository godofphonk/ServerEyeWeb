import { hasUserAccess, shouldShowEmailVerificationBanner, isOAuthUser } from '@/lib/authUtils';
import { User } from '@/types';

const makeUser = (overrides: Partial<User> = {}): User => ({
  id: '1',
  email: 'user@example.com',
  username: 'testuser',
  role: 'user',
  createdAt: '2024-01-01T00:00:00Z',
  ...overrides,
});

describe('hasUserAccess', () => {
  it('returns false when user is null', () => {
    expect(hasUserAccess(null, true)).toBe(false);
    expect(hasUserAccess(null, false)).toBe(false);
  });

  it('returns true for OAuth user regardless of email verification', () => {
    const oauthUser = makeUser({ hasPassword: false });
    expect(hasUserAccess(oauthUser, false)).toBe(true);
    expect(hasUserAccess(oauthUser, true)).toBe(true);
    expect(hasUserAccess(oauthUser, '')).toBe(true);
  });

  it('returns true for regular user with verified email (boolean)', () => {
    const user = makeUser({ hasPassword: true });
    expect(hasUserAccess(user, true)).toBe(true);
  });

  it('returns false for regular user with unverified email (boolean)', () => {
    const user = makeUser({ hasPassword: true });
    expect(hasUserAccess(user, false)).toBe(false);
  });

  it('returns true for regular user with truthy string email verification', () => {
    const user = makeUser({ hasPassword: true });
    expect(hasUserAccess(user, 'true')).toBe(true);
  });

  it('returns false for regular user with empty string email verification', () => {
    const user = makeUser({ hasPassword: true });
    expect(hasUserAccess(user, '')).toBe(false);
  });

  it('returns false for regular user when hasPassword is undefined', () => {
    const user = makeUser({ hasPassword: undefined });
    expect(hasUserAccess(user, false)).toBe(false);
  });

  it('returns true for regular user when hasPassword is undefined but email is verified', () => {
    const user = makeUser({ hasPassword: undefined });
    expect(hasUserAccess(user, true)).toBe(true);
  });
});

describe('shouldShowEmailVerificationBanner', () => {
  it('returns false when user is null', () => {
    expect(shouldShowEmailVerificationBanner(null, false)).toBe(false);
  });

  it('returns false for OAuth user', () => {
    const oauthUser = makeUser({ hasPassword: false });
    expect(shouldShowEmailVerificationBanner(oauthUser, false)).toBe(false);
    expect(shouldShowEmailVerificationBanner(oauthUser, true)).toBe(false);
  });

  it('returns true for regular user with unverified email', () => {
    const user = makeUser({ hasPassword: true, email: 'user@example.com' });
    expect(shouldShowEmailVerificationBanner(user, false)).toBe(true);
  });

  it('returns false for regular user with verified email', () => {
    const user = makeUser({ hasPassword: true, email: 'user@example.com' });
    expect(shouldShowEmailVerificationBanner(user, true)).toBe(false);
  });

  it('returns false for regular user with no email', () => {
    const user = makeUser({ hasPassword: true, email: '' });
    expect(shouldShowEmailVerificationBanner(user, false)).toBe(false);
  });

  it('returns false for regular user with whitespace-only email', () => {
    const user = makeUser({ hasPassword: true, email: '   ' });
    expect(shouldShowEmailVerificationBanner(user, false)).toBe(false);
  });

  it('returns false for regular user when email verification is truthy string', () => {
    const user = makeUser({ hasPassword: true, email: 'user@example.com' });
    expect(shouldShowEmailVerificationBanner(user, 'verified')).toBe(false);
  });
});

describe('isOAuthUser', () => {
  it('returns false when user is null', () => {
    expect(isOAuthUser(null)).toBe(false);
  });

  it('returns true when hasPassword is false', () => {
    const user = makeUser({ hasPassword: false });
    expect(isOAuthUser(user)).toBe(true);
  });

  it('returns false when hasPassword is true', () => {
    const user = makeUser({ hasPassword: true });
    expect(isOAuthUser(user)).toBe(false);
  });

  it('returns false when hasPassword is undefined', () => {
    const user = makeUser({ hasPassword: undefined });
    expect(isOAuthUser(user)).toBe(false);
  });
});
