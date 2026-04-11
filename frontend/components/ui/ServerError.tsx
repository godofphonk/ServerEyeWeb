'use client';

import React from 'react';
import { AlertTriangle, RefreshCw, Wifi, Server } from 'lucide-react';
import { Card } from './Card';
import { Button } from './Button';

interface ServerErrorProps {
  error: any; // eslint-disable-line @typescript-eslint/no-explicit-any
  onRetry?: () => void;
  showRetryButton?: boolean;
  compact?: boolean;
}

export function ServerError({
  error,
  onRetry,
  showRetryButton = true,
  compact = false,
}: ServerErrorProps) {
  const getErrorInfo = () => {
    const status = error.response?.status;
    const statusText = error.response?.statusText || 'Unknown Error';

    switch (status) {
      case 500:
        return {
          title: 'Внутренняя ошибка сервера',
          description:
            'Произошла внутренняя ошибка на сервере. Мы уже работаем над решением проблемы.',
          icon: <Server className='w-12 h-12 text-red-500' />,
          borderColor: 'border-red-500/50',
          bgColor: 'bg-red-500/5',
          isRetryable: true,
        };
      case 502:
        return {
          title: 'Некорректный ответ шлюза',
          description:
            'Сервер получил некорректный ответ от вышестоящего сервиса. Попробуйте повторить запрос.',
          icon: <AlertTriangle className='w-12 h-12 text-orange-500' />,
          borderColor: 'border-orange-500/50',
          bgColor: 'bg-orange-500/5',
          isRetryable: true,
        };
      case 503:
        return {
          title: 'Сервис временно недоступен',
          description:
            'Сервис временно недоступен из-за технических работ. Пожалуйста, попробуйте позже.',
          icon: <Server className='w-12 h-12 text-yellow-500' />,
          borderColor: 'border-yellow-500/50',
          bgColor: 'bg-yellow-500/5',
          isRetryable: true,
        };
      case 504:
        return {
          title: 'Таймаут шлюза',
          description:
            'Сервер не получил своевременного ответа от вышестоящего сервиса. Попробуйте повторить запрос.',
          icon: <Wifi className='w-12 h-12 text-orange-500' />,
          borderColor: 'border-orange-500/50',
          bgColor: 'bg-orange-500/5',
          isRetryable: true,
        };
      default:
        return {
          title: 'Ошибка сервера',
          description: `Произошла ошибка (${status} ${statusText}). Попробуйте обновить страницу.`,
          icon: <AlertTriangle className='w-12 h-12 text-red-500' />,
          borderColor: 'border-red-500/50',
          bgColor: 'bg-red-500/5',
          isRetryable: false,
        };
    }
  };

  const errorInfo = getErrorInfo();

  if (compact) {
    return (
      <div
        className={`flex items-center justify-between p-4 ${errorInfo.bgColor} border ${errorInfo.borderColor} rounded-lg`}
      >
        <div className='flex items-center gap-3'>
          <div className='flex-shrink-0'>{errorInfo.icon}</div>
          <div>
            <p className='text-sm text-white font-medium'>{errorInfo.title}</p>
            <p className='text-xs text-gray-400 mt-1'>{errorInfo.description}</p>
          </div>
        </div>

        {showRetryButton && errorInfo.isRetryable && onRetry && (
          <Button
            onClick={onRetry}
            variant='secondary'
            size='sm'
            className='flex items-center gap-2 ml-4'
          >
            <RefreshCw className='w-4 h-4' />
            Повторить
          </Button>
        )}
      </div>
    );
  }

  return (
    <Card className={`border-2 ${errorInfo.borderColor} ${errorInfo.bgColor}`}>
      <div className='p-6'>
        <div className='flex items-start gap-4'>
          <div className='flex-shrink-0'>{errorInfo.icon}</div>

          <div className='flex-1 space-y-3'>
            <div>
              <h3 className='text-lg font-semibold text-white mb-1'>{errorInfo.title}</h3>

              <p className='text-gray-300 text-sm leading-relaxed'>{errorInfo.description}</p>
            </div>

            <div className='flex items-center gap-3 pt-2'>
              {showRetryButton && errorInfo.isRetryable && onRetry && (
                <Button onClick={onRetry} variant='primary' className='flex items-center gap-2'>
                  <RefreshCw className='w-4 h-4' />
                  Повторить попытку
                </Button>
              )}

              {!errorInfo.isRetryable && (
                <div className='text-sm text-gray-400'>
                  Если проблема сохраняется, обновите страницу или свяжитесь с поддержкой
                </div>
              )}
            </div>

            <div className='pt-3 border-t border-gray-700/50'>
              <details className='text-xs text-gray-500'>
                <summary className='cursor-pointer hover:text-gray-400'>
                  Техническая информация
                </summary>
                <div className='mt-2 space-y-1 font-mono'>
                  <div>HTTP статус: {error.response?.status || 'N/A'}</div>
                  <div>Сообщение: {error.message || 'No message'}</div>
                  <div>URL: {error.config?.url || 'N/A'}</div>
                  <div>Метод: {error.config?.method?.toUpperCase() || 'N/A'}</div>
                  {error.code && <div>Код ошибки: {error.code}</div>}
                </div>
              </details>
            </div>
          </div>
        </div>
      </div>
    </Card>
  );
}

export function ServerErrorInline({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  return <ServerError error={error} onRetry={onRetry} compact={true} showRetryButton={true} />;
}
