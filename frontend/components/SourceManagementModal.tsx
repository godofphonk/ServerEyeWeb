'use client';

import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { X, Trash2, AlertTriangle, Loader2, Eye, EyeOff } from 'lucide-react';
import { apiClient } from '@/lib/api';
import { DeleteSourceIdentifiersRequest, SourceInfo } from '@/types';
import { useToast } from '@/hooks/useToast';

interface SourceManagementModalProps {
  isOpen: boolean;
  onClose: () => void;
  serverKey: string;
  serverId: string;
  hostname: string;
  sources?: string[];
  onSourceDeleted?: () => void;
}

export default function SourceManagementModal({
  isOpen,
  onClose,
  serverKey,
  serverId,
  hostname,
  sources = [],
  onSourceDeleted,
}: SourceManagementModalProps) {
  const toast = useToast();
  const [isLoading, setIsLoading] = useState(false);
  const [sourceDetails, setSourceDetails] = useState<Record<string, SourceInfo>>({});
  const [showDetails, setShowDetails] = useState<Record<string, boolean>>({});

  const loadSourceDetails = async (sourceType: string) => {
    if (sourceDetails[sourceType]) return;

    try {
      setIsLoading(true);
      // В реальном API нужно будет добавить endpoint для получения деталей sources
      // Пока используем мок данные для демонстрации
      const mockDetails: SourceInfo = {
        source_type: sourceType,
        identifiers: [
          {
            id: 1,
            server_id: serverId,
            source_type: sourceType,
            identifier: sourceType === 'Web' ? 'user-id-example' : 'telegram-id-example',
            identifier_type: sourceType === 'Web' ? 'user_id' : 'telegram_id',
            metadata:
              sourceType === 'Web'
                ? {
                    added_at: new Date().toISOString(),
                    source: 'ServerEyeWeb',
                  }
                : {
                    chat_type: 'private',
                    username: 'example_user',
                  },
            created_at: new Date().toISOString(),
            updated_at: new Date().toISOString(),
          },
        ],
      };

      setSourceDetails(prev => ({ ...prev, [sourceType]: mockDetails }));
    } catch (_error) {
      toast.error('Failed to load source details');
    } finally {
      setIsLoading(false);
    }
  };

  const deleteSource = async (sourceType: string) => {
    if (!confirm(`Are you sure you want to remove ${sourceType} source from ${hostname}?`)) {
      return;
    }

    try {
      setIsLoading(true);
      const response = await apiClient.deleteServerSource(serverKey, sourceType);

      if (response.success) {
        toast.success(`Successfully removed ${sourceType} source`);
        onSourceDeleted?.();
        onClose();
      } else {
        toast.error(response.message || 'Failed to remove source');
      }
    } catch (_error) {
      toast.error('Failed to remove source');
    } finally {
      setIsLoading(false);
    }
  };

  const deleteIdentifiers = async (sourceType: string, identifiers: string[]) => {
    if (
      !confirm(
        `Are you sure you want to remove ${identifiers.length} identifier(s) from ${sourceType} source?`,
      )
    ) {
      return;
    }

    try {
      setIsLoading(true);
      const request: DeleteSourceIdentifiersRequest = { identifiers };
      const response = await apiClient.deleteServerSourceIdentifiersByType(
        serverKey,
        sourceType,
        request,
      );

      if (response.success) {
        toast.success(`Successfully removed ${response.deleted_identifiers.length} identifier(s)`);
        onSourceDeleted?.();
        // Reload source details
        const newDetails = { ...sourceDetails };
        delete newDetails[sourceType];
        setSourceDetails(newDetails);
      } else {
        toast.error(response.message || 'Failed to remove identifiers');
      }
    } catch (_error) {
      toast.error('Failed to remove identifiers');
    } finally {
      setIsLoading(false);
    }
  };

  const getSourceIcon = (sourceType: string) => {
    switch (sourceType.toLowerCase()) {
      case 'web':
        return '🌐';
      case 'telegram':
      case 'tgbot':
        return '✈️';
      default:
        return '🔗';
    }
  };

  const getSourceColor = (sourceType: string) => {
    switch (sourceType.toLowerCase()) {
      case 'web':
        return 'from-blue-500/20 to-blue-600/20 border-blue-500/50 text-blue-400';
      case 'telegram':
      case 'tgbot':
        return 'from-sky-500/20 to-cyan-600/20 border-sky-500/50 text-sky-400';
      default:
        return 'from-gray-500/20 to-gray-600/20 border-gray-500/50 text-gray-400';
    }
  };

  if (!isOpen) return null;

  return (
    <AnimatePresence>
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        className='fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4'
        onClick={onClose}
      >
        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 20 }}
          className='bg-gradient-to-br from-gray-900 via-purple-900/20 to-gray-900 rounded-2xl border border-purple-500/20 p-6 max-w-2xl w-full max-h-[80vh] overflow-y-auto'
          onClick={e => e.stopPropagation()}
        >
          {/* Header */}
          <div className='flex items-center justify-between mb-6'>
            <div>
              <h2 className='text-2xl font-bold text-white mb-2'>Source Management</h2>
              <p className='text-gray-400'>Manage access sources for {hostname}</p>
            </div>
            <button onClick={onClose} className='text-gray-400 hover:text-white transition-colors'>
              <X className='w-6 h-6' />
            </button>
          </div>

          {/* Server Info */}
          <div className='bg-gradient-to-r from-purple-500/10 to-pink-500/10 rounded-lg p-4 mb-6 border border-purple-500/20'>
            <div className='flex items-center gap-3'>
              <div className='w-10 h-10 bg-gradient-to-br from-purple-500 to-pink-500 rounded-lg flex items-center justify-center'>
                <span className='text-white font-bold'>🖥️</span>
              </div>
              <div>
                <p className='text-white font-semibold'>{hostname}</p>
                <p className='text-gray-400 text-sm'>Server ID: {serverId}</p>
                <p className='text-gray-400 text-sm'>Key: {serverKey.substring(0, 8)}...</p>
              </div>
            </div>
          </div>

          {/* Sources List */}
          <div className='space-y-4'>
            <h3 className='text-lg font-semibold text-white mb-4'>
              Active Sources ({sources.length})
            </h3>

            {sources.length === 0 ? (
              <div className='text-center py-8 text-gray-400'>
                <AlertTriangle className='w-12 h-12 mx-auto mb-4 opacity-50' />
                <p>No sources found for this server</p>
              </div>
            ) : (
              sources.map(sourceType => (
                <motion.div
                  key={sourceType}
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  className={`bg-gradient-to-r ${getSourceColor(sourceType)} rounded-lg p-4 border backdrop-blur-sm`}
                >
                  <div className='flex items-center justify-between'>
                    <div className='flex items-center gap-3'>
                      <span className='text-2xl'>{getSourceIcon(sourceType)}</span>
                      <div>
                        <p className='text-white font-semibold'>{sourceType}</p>
                        <p className='text-sm opacity-80'>Access source type</p>
                      </div>
                    </div>

                    <div className='flex items-center gap-2'>
                      <button
                        onClick={() => {
                          loadSourceDetails(sourceType);
                          setShowDetails(prev => ({ ...prev, [sourceType]: !prev[sourceType] }));
                        }}
                        className='p-2 rounded-lg bg-white/10 hover:bg-white/20 transition-colors'
                        title='View details'
                      >
                        {showDetails[sourceType] ? (
                          <EyeOff className='w-4 h-4' />
                        ) : (
                          <Eye className='w-4 h-4' />
                        )}
                      </button>

                      <button
                        onClick={() => deleteSource(sourceType)}
                        disabled={isLoading}
                        className='p-2 rounded-lg bg-red-500/20 hover:bg-red-500/30 text-red-400 transition-colors disabled:opacity-50'
                        title='Remove source'
                      >
                        {isLoading ? (
                          <Loader2 className='w-4 h-4 animate-spin' />
                        ) : (
                          <Trash2 className='w-4 h-4' />
                        )}
                      </button>
                    </div>
                  </div>

                  {/* Source Details */}
                  <AnimatePresence>
                    {showDetails[sourceType] && sourceDetails[sourceType] && (
                      <motion.div
                        initial={{ opacity: 0, height: 0 }}
                        animate={{ opacity: 1, height: 'auto' }}
                        exit={{ opacity: 0, height: 0 }}
                        className='mt-4 pt-4 border-t border-white/20'
                      >
                        <h4 className='text-white font-semibold mb-3'>
                          Identifiers ({sourceDetails[sourceType].identifiers.length})
                        </h4>
                        <div className='space-y-2'>
                          {sourceDetails[sourceType].identifiers.map(identifier => (
                            <div
                              key={identifier.id}
                              className='bg-black/20 rounded-lg p-3 flex items-center justify-between'
                            >
                              <div className='flex-1'>
                                <p className='text-white text-sm font-mono'>
                                  {identifier.identifier}
                                </p>
                                <p className='text-gray-400 text-xs'>
                                  {identifier.identifier_type}
                                </p>
                                {identifier.metadata && (
                                  <div className='mt-2'>
                                    {Object.entries(identifier.metadata).map(([key, value]) => (
                                      <p key={key} className='text-gray-500 text-xs'>
                                        {key}:{' '}
                                        {typeof value === 'object'
                                          ? JSON.stringify(value)
                                          : String(value)}
                                      </p>
                                    ))}
                                  </div>
                                )}
                              </div>
                              <button
                                onClick={() =>
                                  deleteIdentifiers(sourceType, [identifier.identifier])
                                }
                                disabled={isLoading}
                                className='p-1 rounded bg-red-500/20 hover:bg-red-500/30 text-red-400 transition-colors disabled:opacity-50'
                                title='Remove identifier'
                              >
                                <Trash2 className='w-3 h-3' />
                              </button>
                            </div>
                          ))}
                        </div>
                      </motion.div>
                    )}
                  </AnimatePresence>
                </motion.div>
              ))
            )}
          </div>

          {/* Warning */}
          <div className='mt-6 p-4 bg-yellow-500/10 border border-yellow-500/30 rounded-lg'>
            <div className='flex items-start gap-3'>
              <AlertTriangle className='w-5 h-5 text-yellow-400 mt-0.5' />
              <div>
                <p className='text-yellow-400 font-semibold text-sm'>Warning</p>
                <p className='text-gray-400 text-sm mt-1'>
                  Removing sources will affect access to this server. Make sure you understand the
                  implications before removing any source.
                </p>
              </div>
            </div>
          </div>
        </motion.div>
      </motion.div>
    </AnimatePresence>
  );
}
