'use client';

import React, { useState, useMemo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { X, Server, CheckCircle, AlertCircle, Download, ExternalLink } from 'lucide-react';
import { DiscoveredServersResponse } from '@/types';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';

interface ServerDiscoveryModalProps {
  discovered: DiscoveredServersResponse;
  onImport: (serverIds: string[]) => Promise<void>;
  onClose: () => void;
  isImporting?: boolean;
}

export function ServerDiscoveryModal({
  discovered,
  onImport,
  onClose,
  isImporting = false,
}: ServerDiscoveryModalProps) {
  const [selectedServers, setSelectedServers] = useState<string[]>([]);

  const importableServers = useMemo(
    () => discovered.servers.filter(s => s.can_import),
    [discovered.servers],
  );

  const alreadyAddedServers = useMemo(
    () => discovered.servers.filter(s => !s.can_import),
    [discovered.servers],
  );

  const toggleServer = (serverId: string) => {
    setSelectedServers(prev =>
      prev.includes(serverId) ? prev.filter(id => id !== serverId) : [...prev, serverId],
    );
  };

  const selectAll = () => {
    setSelectedServers(importableServers.map(s => s.server_id));
  };

  const deselectAll = () => {
    setSelectedServers([]);
  };

  const handleImport = async () => {
    if (selectedServers.length === 0) return;
    await onImport(selectedServers);
  };

  const pluralize = (count: number) => {
    if (count === 1) return 'сервер';
    if (count >= 2 && count <= 4) return 'сервера';
    return 'серверов';
  };

  return (
    <AnimatePresence>
      <div className='fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm'>
        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          exit={{ opacity: 0, scale: 0.95 }}
          className='w-full max-w-2xl max-h-[90vh] overflow-hidden'
        >
          <Card className='relative'>
            <div className='p-6'>
              {/* Header */}
              <div className='flex items-start justify-between mb-6'>
                <div>
                  <h2 className='text-2xl font-bold text-white mb-2 flex items-center gap-2'>
                    🎉 Найдены серверы из Telegram!
                  </h2>
                  <p className='text-gray-400'>
                    Обнаружено {discovered.total_count} {pluralize(discovered.total_count)},
                    привязанных к вашему Telegram аккаунту
                  </p>
                </div>
                <button
                  onClick={onClose}
                  className='p-2 hover:bg-gray-700 rounded-lg transition-colors'
                  disabled={isImporting}
                >
                  <X className='w-5 h-5 text-gray-400' />
                </button>
              </div>

              {/* Content */}
              <div className='space-y-4'>
                {importableServers.length > 0 ? (
                  <>
                    <div className='flex items-center justify-between'>
                      <p className='text-sm text-gray-300'>
                        Выберите серверы для импорта в веб-аккаунт:
                      </p>
                      <div className='flex gap-2'>
                        <button
                          onClick={selectAll}
                          className='text-xs text-blue-400 hover:text-blue-300'
                          disabled={isImporting}
                        >
                          Выбрать все
                        </button>
                        <span className='text-gray-600'>|</span>
                        <button
                          onClick={deselectAll}
                          className='text-xs text-gray-400 hover:text-gray-300'
                          disabled={isImporting}
                        >
                          Снять выбор
                        </button>
                      </div>
                    </div>

                    {/* Importable Servers List */}
                    <div className='max-h-64 overflow-y-auto space-y-2 pr-2'>
                      {importableServers.map(server => (
                        <motion.div
                          key={server.server_id}
                          initial={{ opacity: 0, y: 10 }}
                          animate={{ opacity: 1, y: 0 }}
                          className={`p-4 rounded-lg border-2 transition-all cursor-pointer ${
                            selectedServers.includes(server.server_id)
                              ? 'border-blue-500 bg-blue-500/10'
                              : 'border-gray-700 bg-gray-800/50 hover:border-gray-600'
                          }`}
                          onClick={() => !isImporting && toggleServer(server.server_id)}
                        >
                          <div className='flex items-start gap-3'>
                            <div className='flex-shrink-0 mt-1'>
                              <div
                                className={`w-5 h-5 rounded border-2 flex items-center justify-center transition-colors ${
                                  selectedServers.includes(server.server_id)
                                    ? 'border-blue-500 bg-blue-500'
                                    : 'border-gray-600'
                                }`}
                              >
                                {selectedServers.includes(server.server_id) && (
                                  <CheckCircle className='w-4 h-4 text-white' />
                                )}
                              </div>
                            </div>
                            <div className='flex-1'>
                              <div className='flex items-center gap-2 mb-1'>
                                <Server className='w-4 h-4 text-blue-400' />
                                <span className='font-semibold text-white'>{server.hostname}</span>
                              </div>
                              <div className='text-sm text-gray-400'>{server.operating_system}</div>
                              <div className='flex items-center gap-4 mt-2 text-xs text-gray-500'>
                                <span>v{server.agent_version}</span>
                                <span>•</span>
                                <span>
                                  Последняя активность:{' '}
                                  {new Date(server.last_seen).toLocaleString('ru-RU')}
                                </span>
                              </div>
                            </div>
                          </div>
                        </motion.div>
                      ))}
                    </div>
                  </>
                ) : null}

                {/* Already Added Servers */}
                {alreadyAddedServers.length > 0 && (
                  <div className='mt-4'>
                    <p className='text-sm text-gray-400 mb-2'>Уже добавлены в веб-аккаунт:</p>
                    <div className='space-y-2'>
                      {alreadyAddedServers.map(server => (
                        <div
                          key={server.server_id}
                          className='p-3 rounded-lg bg-gray-800/30 border border-gray-700'
                        >
                          <div className='flex items-center gap-2'>
                            <CheckCircle className='w-4 h-4 text-green-500' />
                            <span className='text-sm text-gray-300'>{server.hostname}</span>
                            <span className='ml-auto text-xs text-gray-500 px-2 py-1 bg-gray-700 rounded'>
                              Уже добавлен
                            </span>
                          </div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* All servers already added */}
                {importableServers.length === 0 && alreadyAddedServers.length > 0 && (
                  <div className='p-4 rounded-lg bg-green-500/10 border border-green-500/30'>
                    <div className='flex items-center gap-2 text-green-400'>
                      <CheckCircle className='w-5 h-5' />
                      <span className='font-medium'>
                        Все ваши серверы уже добавлены в веб-аккаунт! 🎉
                      </span>
                    </div>
                  </div>
                )}

                {/* Telegram Bot Info */}
                {discovered.has_telegram_bot && (
                  <div className='p-4 rounded-lg bg-blue-500/10 border border-blue-500/30'>
                    <div className='flex items-start gap-3'>
                      <AlertCircle className='w-5 h-5 text-blue-400 flex-shrink-0 mt-0.5' />
                      <div className='flex-1'>
                        <p className='text-sm text-blue-300 mb-2'>
                          Серверы добавленные через Telegram бот автоматически появятся здесь!
                        </p>
                        <a
                          href={`https://t.me/${discovered.telegram_bot_username}`}
                          target='_blank'
                          rel='noopener noreferrer'
                          className='inline-flex items-center gap-1 text-sm text-blue-400 hover:text-blue-300'
                        >
                          Открыть @{discovered.telegram_bot_username}
                          <ExternalLink className='w-3 h-3' />
                        </a>
                      </div>
                    </div>
                  </div>
                )}
              </div>

              {/* Actions */}
              <div className='flex gap-3 mt-6'>
                {importableServers.length > 0 && (
                  <Button
                    onClick={handleImport}
                    disabled={selectedServers.length === 0 || isImporting}
                    variant='primary'
                    className='flex-1 flex items-center justify-center gap-2'
                  >
                    {isImporting ? (
                      <>
                        <div className='w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin' />
                        Импортируем...
                      </>
                    ) : (
                      <>
                        <Download className='w-4 h-4' />
                        Импортировать выбранные ({selectedServers.length})
                      </>
                    )}
                  </Button>
                )}
                <Button
                  onClick={onClose}
                  disabled={isImporting}
                  variant='secondary'
                  className={importableServers.length > 0 ? '' : 'flex-1'}
                >
                  {importableServers.length > 0 ? 'Пропустить' : 'Закрыть'}
                </Button>
              </div>
            </div>
          </Card>
        </motion.div>
      </div>
    </AnimatePresence>
  );
}
