'use client';

import React from 'react';
import { GoApiError } from '@/types';
import { AlertTriangle, RefreshCw, Clock, Wifi, Server, Shield } from 'lucide-react';
import { Card } from './Card';
import { Button } from './Button';

interface MonitoringServiceErrorProps {
  error: GoApiError;
  onRetry?: () => void;
  showRetryButton?: boolean;
}

export function MonitoringServiceError({
  error,
  onRetry,
  showRetryButton = true,
}: MonitoringServiceErrorProps) {
  const getErrorIcon = () => {
    switch (error.errorType) {
      case 'Timeout':
        return <Clock className="w-12 h-12 text-yellow-500" />;
      case 'NetworkError':
        return <Wifi className="w-12 h-12 text-orange-500" />;
      case 'ServiceUnavailable':
        return <Server className="w-12 h-12 text-red-500" />;
      case 'Unauthorized':
        return <Shield className="w-12 h-12 text-red-600" />;
      default:
        return <AlertTriangle className="w-12 h-12 text-yellow-500" />;
    }
  };

  const getErrorTitle = () => {
    switch (error.errorType) {
      case 'Timeout':
        return 'Превышено время ожидания';
      case 'NetworkError':
        return 'Проблемы с сетью';
      case 'ServiceUnavailable':
        return 'Сервис мониторинга недоступен';
      case 'Unauthorized':
        return 'Недостаточно прав доступа';
      case 'InvalidResponse':
        return 'Некорректный ответ сервиса';
      default:
        return 'Ошибка сервиса мониторинга';
    }
  };

  const getBorderColor = () => {
    if (error.isTemporary) {
      return 'border-yellow-500/50';
    }
    return 'border-red-500/50';
  };

  const getBackgroundColor = () => {
    if (error.isTemporary) {
      return 'bg-yellow-500/5';
    }
    return 'bg-red-500/5';
  };

  return (
    <Card className={`border-2 ${getBorderColor()} ${getBackgroundColor()}`}>
      <div className="p-6">
        <div className="flex items-start gap-4">
          <div className="flex-shrink-0">{getErrorIcon()}</div>
          
          <div className="flex-1 space-y-3">
            <div>
              <h3 className="text-lg font-semibold text-white mb-1">
                {getErrorTitle()}
              </h3>
              
              <p className="text-gray-300 text-sm leading-relaxed">
                {error.userMessage}
              </p>
            </div>

            {error.isTemporary && (
              <div className="flex items-center gap-2 text-yellow-400 text-sm">
                <Clock className="w-4 h-4" />
                <span>Это временная проблема. Попробуйте повторить запрос.</span>
              </div>
            )}

            <div className="flex items-center gap-3 pt-2">
              {showRetryButton && error.isTemporary && onRetry && (
                <Button
                  onClick={onRetry}
                  variant="primary"
                  className="flex items-center gap-2"
                >
                  <RefreshCw className="w-4 h-4" />
                  Повторить попытку
                </Button>
              )}

              {!error.isTemporary && (
                <div className="text-sm text-gray-400">
                  Если проблема сохраняется, свяжитесь с поддержкой:{' '}
                  <a
                    href={`mailto:${error.supportContact}`}
                    className="text-blue-400 hover:text-blue-300 underline"
                  >
                    {error.supportContact}
                  </a>
                </div>
              )}
            </div>

            <div className="pt-3 border-t border-gray-700/50">
              <details className="text-xs text-gray-500">
                <summary className="cursor-pointer hover:text-gray-400">
                  Техническая информация
                </summary>
                <div className="mt-2 space-y-1 font-mono">
                  <div>Код ошибки: {error.errorCode}</div>
                  <div>Тип: {error.errorType}</div>
                  <div>HTTP статус: {error.httpStatus}</div>
                  <div>Временная: {error.isTemporary ? 'Да' : 'Нет'}</div>
                  <div>Время: {new Date(error.timestamp).toLocaleString('ru-RU')}</div>
                </div>
              </details>
            </div>
          </div>
        </div>
      </div>
    </Card>
  );
}

export function MonitoringServiceErrorInline({
  error,
  onRetry,
}: {
  error: GoApiError;
  onRetry?: () => void;
}) {
  return (
    <div className="flex items-center justify-between p-4 bg-yellow-500/10 border border-yellow-500/30 rounded-lg">
      <div className="flex items-center gap-3">
        <AlertTriangle className="w-5 h-5 text-yellow-500 flex-shrink-0" />
        <div>
          <p className="text-sm text-white font-medium">
            {error.userMessage}
          </p>
          {error.isTemporary && (
            <p className="text-xs text-gray-400 mt-1">
              Временная проблема - попробуйте обновить
            </p>
          )}
        </div>
      </div>
      
      {error.isTemporary && onRetry && (
        <Button
          onClick={onRetry}
          variant="secondary"
          size="sm"
          className="flex items-center gap-2 ml-4"
        >
          <RefreshCw className="w-4 h-4" />
          Повторить
        </Button>
      )}
    </div>
  );
}
