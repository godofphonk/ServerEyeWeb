import { User } from '@/types';

/**
 * Проверяет имеет ли пользователь доступ к защищенным страницам
 * 
 * Логика:
 * 1. OAuth пользователи (hasPassword: false) - доступ разрешен всегда
 * 2. Обычные пользователи (hasPassword: true) - доступ только если email верифицирован
 * 
 * @param user - объект пользователя
 * @param isEmailVerified - статус верификации email
 * @returns true если доступ разрешен, false если нет
 */
export function hasUserAccess(user: User | null, isEmailVerified: boolean | string): boolean {
  console.log('[hasUserAccess] Input:', { 
    user: user ? { id: user.id, email: user.email, hasPassword: user.hasPassword } : null, 
    isEmailVerified 
  });
  
  if (!user) {
    console.log('[hasUserAccess] No user - returning false');
    return false;
  }

  // OAuth пользователи (любые, включая Google, GitHub, Telegram) - доступ разрешен
  const isOAuthUser = user.hasPassword === false;
  console.log('[hasUserAccess] Is OAuth user:', isOAuthUser);
  
  if (isOAuthUser) {
    console.log('[hasUserAccess] OAuth user - returning true');
    return true;
  }

  // Обычные пользователи с паролем - требуется верификация email
  console.log('[hasUserAccess] Regular user - checking email verification:', isEmailVerified);
  return Boolean(isEmailVerified);
}

/**
 * Проверяет нужно ли показывать баннер верификации email
 * 
 * @param user - объект пользователя
 * @param isEmailVerified - статус верификации email
 * @returns true если нужно показать баннер
 */
export function shouldShowEmailVerificationBanner(user: User | null, isEmailVerified: boolean | string): boolean {
  if (!user) {
    return false;
  }

  // OAuth пользователи не видят баннер верификации
  const isOAuthUser = user.hasPassword === false;
  if (isOAuthUser) {
    return false;
  }

  // Обычные пользователи - показываем баннер если email не верифицирован
  const hasEmail = Boolean(user.email && user.email.trim() !== '');
  const isVerified = Boolean(isEmailVerified);
  return hasEmail && !isVerified;
}

/**
 * Проверяет является ли пользователь OAuth пользователем
 * 
 * @param user - объект пользователя
 * @returns true если это OAuth пользователь (любой, включая с email)
 */
export function isOAuthUser(user: User | null): boolean {
  if (!user) {
    return false;
  }

  return user.hasPassword === false;
}
