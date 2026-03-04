import { User } from '@/types';

/**
 * Проверяет имеет ли пользователь доступ к защищенным страницам
 * 
 * Логика:
 * 1. OAuth пользователи без email - доступ разрешен
 * 2. Пользователи с email - доступ только если email верифицирован
 * 
 * @param user - объект пользователя
 * @param isEmailVerified - статус верификации email
 * @returns true если доступ разрешен, false если нет
 */
export function hasUserAccess(user: User | null, isEmailVerified: boolean): boolean {
  if (!user) {
    return false;
  }

  // OAuth пользователи без email (например Telegram) - доступ разрешен
  const isOAuthUserWithoutEmail = !user.email || user.email.trim() === '';
  
  if (isOAuthUserWithoutEmail) {
    return true;
  }

  // Пользователи с email - требуется верификация
  return isEmailVerified;
}

/**
 * Проверяет нужно ли показывать баннер верификации email
 * 
 * @param user - объект пользователя
 * @param isEmailVerified - статус верификации email
 * @returns true если нужно показать баннер
 */
export function shouldShowEmailVerificationBanner(user: User | null, isEmailVerified: boolean): boolean {
  if (!user) {
    return false;
  }

  // Если email есть и не верифицирован - показываем баннер
  const hasEmail = user.email && user.email.trim() !== '';
  return hasEmail && !isEmailVerified;
}

/**
 * Проверяет является ли пользователь OAuth пользователем без email
 * 
 * @param user - объект пользователя
 * @returns true если это OAuth пользователь без email
 */
export function isOAuthUserWithoutEmail(user: User | null): boolean {
  if (!user) {
    return false;
  }

  return !user.email || user.email.trim() === '';
}
