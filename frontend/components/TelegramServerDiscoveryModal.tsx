import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  X,
  Server,
  CheckCircle2,
  AlertCircle,
  Download,
  ExternalLink,
  Loader2,
  Bot,
  Clock,
  Shield,
  Zap,
  RefreshCw,
  Link,
  MessageCircle,
} from 'lucide-react';
import { DiscoveredServersResponse, DiscoveredServer, ExternalLogin, OAuthProvider } from '@/types';
import { useAuth } from '@/context/AuthContext';
import { Button } from '@/components/ui/Button';

interface TelegramServerDiscoveryModalProps {
  isOpen: boolean;
  discovered: DiscoveredServersResponse | null;
  isLoading: boolean;
  error: string | null;
  onImport: (serverIds: string[]) => Promise<void>;
  onDismiss: () => void;
  onRetry: () => void;
  onDiscoverServers?: () => Promise<DiscoveredServersResponse | null>; // Add this for manual discovery
}

interface ImportProgress {
  importing: boolean;
  imported: number;
  total: number;
  errors: string[];
}

export function TelegramServerDiscoveryModal({
  isOpen,
  discovered,
  isLoading,
  error,
  onImport,
  onDismiss,
  onRetry,
  onDiscoverServers,
}: TelegramServerDiscoveryModalProps) {
  const { getExternalLogins, linkExternalAccount, getOAuthChallenge } = useAuth();
  const [selectedServers, setSelectedServers] = useState<Set<string>>(new Set());
  const [importProgress, setImportProgress] = useState<ImportProgress>({
    importing: false,
    imported: 0,
    total: 0,
    errors: [],
  });
  const [showSuccess, setShowSuccess] = useState(false);
  const [externalLogins, setExternalLogins] = useState<ExternalLogin[]>([]);
  const [isLinkingTelegram, setIsLinkingTelegram] = useState(false);

  // Reset selection when discovered data changes
  React.useEffect(() => {
    if (discovered) {
      const importableServerIds = discovered.servers
        .filter(server => server.can_import)
        .map(server => server.server_id);

      setSelectedServers(new Set(importableServerIds));
    }
  }, [discovered]);

  // Load external logins to check if Telegram is linked
  React.useEffect(() => {
    const loadExternalLogins = async () => {
      try {
        const response = await getExternalLogins();
        setExternalLogins(response.externalLogins || []);

        // If Telegram is now linked and we have discoverServers function, trigger discovery
        const isLinked = response.externalLogins?.some(
          login => login.provider === OAuthProvider.Telegram,
        );
        if (isLinked && onDiscoverServers && !discovered && !isLoading) {
          onDiscoverServers();
        }
      } catch (error) { /* ignore error */ }
    };

    if (isOpen) {
      loadExternalLogins();
    }
  }, [isOpen, getExternalLogins, onDiscoverServers, discovered, isLoading]);

  const isTelegramLinked = externalLogins.some(login => login.provider === OAuthProvider.Telegram);

  const handleLinkTelegram = async () => {
    try {
      setIsLinkingTelegram(true);
      const challenge = await getOAuthChallenge('telegram', window.location.href, 'link');

      // Store linking info in sessionStorage
      sessionStorage.setItem(
        'oauth_linking',
        JSON.stringify({
          action: 'link',
          provider: 'telegram',
          state: challenge.state,
        }),
      );

      // Redirect to Telegram OAuth
      window.location.href = challenge.challengeUrl.toString();
    } catch (error) {
      setIsLinkingTelegram(false);
    }
  };

  const handleServerToggle = (serverId: string) => {
    setSelectedServers(prev => {
      const newSet = new Set(prev);
      if (newSet.has(serverId)) {
        newSet.delete(serverId);
      } else {
        newSet.add(serverId);
      }
      return newSet;
    });
  };

  const handleSelectAll = () => {
    if (!discovered) return;

    const importableServerIds = discovered.servers
      .filter(server => server.can_import)
      .map(server => server.server_id);

    setSelectedServers(new Set(importableServerIds));
  };

  const handleDeselectAll = () => {
    setSelectedServers(new Set());
  };

  const handleImport = async () => {
    if (!discovered || selectedServers.size === 0) return;

    setImportProgress({
      importing: true,
      imported: 0,
      total: selectedServers.size,
      errors: [],
    });

    try {
      await onImport(Array.from(selectedServers));

      // Success state
      setImportProgress(prev => ({
        ...prev,
        importing: false,
        imported: selectedServers.size,
      }));
      setShowSuccess(true);

      // Auto-close after success
      setTimeout(() => {
        setShowSuccess(false);
        onDismiss();
      }, 2000);
    } catch (err) {
      setImportProgress(prev => ({
        ...prev,
        importing: false,
        errors: ['Failed to import some servers. Please try again.'],
      }));
    }
  };

  const getServerIcon = (server: DiscoveredServer) => {
    if (server.operating_system?.toLowerCase().includes('linux')) return '🐧';
    if (server.operating_system?.toLowerCase().includes('windows')) return '🪟';
    if (server.operating_system?.toLowerCase().includes('mac')) return '🍎';
    return '🖥️';
  };

  const formatLastSeen = (lastSeen?: string) => {
    if (!lastSeen) return 'Never';

    const date = new Date(lastSeen);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / (1000 * 60));

    if (diffMins < 60) return `${diffMins} minutes ago`;
    if (diffMins < 1440) return `${Math.floor(diffMins / 60)} hours ago`;
    return `${Math.floor(diffMins / 1440)} days ago`;
  };

  if (!isOpen) return null;

  return (
    <AnimatePresence>
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        className='fixed inset-0 bg-black/80 backdrop-blur-md z-50 flex items-center justify-center p-4'
        onClick={onDismiss}
      >
        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 20 }}
          transition={{ type: 'spring', duration: 0.5, bounce: 0.3 }}
          className='bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900 border border-gray-700/50 rounded-2xl shadow-2xl max-w-4xl w-full max-h-[85vh] overflow-hidden backdrop-blur-xl'
          onClick={e => e.stopPropagation()}
        >
          {/* Header */}
          <div className='flex items-center justify-between p-6 border-b border-gray-800 bg-gradient-to-r from-blue-600/10 via-purple-600/10 to-pink-600/10'>
            <div className='flex items-center gap-3'>
              <div className='w-12 h-12 bg-gradient-to-br from-blue-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg'>
                <Bot className='w-6 h-6 text-white' />
              </div>
              <div>
                <h2 className='text-xl font-bold text-white'>Telegram Servers Discovered</h2>
                <p className='text-gray-400 text-sm'>
                  Found servers connected to your Telegram bot
                </p>
              </div>
            </div>
            <button
              onClick={onDismiss}
              className='p-2 hover:bg-white/10 rounded-lg transition-colors'
              disabled={importProgress.importing}
            >
              <X className='w-5 h-5 text-gray-400' />
            </button>
          </div>

          {/* Content */}
          <div className='p-6 overflow-y-auto max-h-[50vh] bg-gradient-to-b from-transparent to-gray-900/30'>
            {/* Telegram not linked state */}
            {!isTelegramLinked && !isLoading && !error && (
              <div className='text-center py-8'>
                <div className='w-16 h-16 bg-gradient-to-br from-blue-500/20 to-cyan-500/20 rounded-full flex items-center justify-center mx-auto mb-4 border border-blue-500/30'>
                  <MessageCircle className='w-8 h-8 text-blue-400' />
                </div>
                <h3 className='text-xl font-semibold text-white mb-2'>Connect Telegram Bot</h3>
                <p className='text-gray-400 mb-6 max-w-md mx-auto'>
                  To discover servers from Telegram, you need to link your Telegram account first.
                  This allows us to securely access your bot's server information.
                </p>

                <div className='bg-gradient-to-br from-gray-800/50 to-gray-900/50 border border-gray-700/50 rounded-xl p-4 mb-6 max-w-sm mx-auto backdrop-blur-sm'>
                  <div className='flex items-center gap-3 mb-3'>
                    <Shield className='w-5 h-5 text-green-400 flex-shrink-0' />
                    <span className='text-green-400 font-medium'>Secure Connection</span>
                  </div>
                  <p className='text-gray-400 text-sm'>
                    We use OAuth to securely link your account without storing your Telegram
                    credentials.
                  </p>
                </div>

                <Button
                  onClick={handleLinkTelegram}
                  disabled={isLinkingTelegram}
                  className='bg-gradient-to-r from-blue-500 to-cyan-500 hover:from-blue-600 hover:to-cyan-600 text-white px-6 py-3 rounded-xl font-medium transition-all duration-200 shadow-lg shadow-blue-500/25 disabled:opacity-50 disabled:cursor-not-allowed'
                >
                  {isLinkingTelegram ? (
                    <>
                      <Loader2 className='w-5 h-5 mr-2 animate-spin' />
                      Connecting...
                    </>
                  ) : (
                    <>
                      <Link className='w-5 h-5 mr-2' />
                      Link Telegram Account
                    </>
                  )}
                </Button>
              </div>
            )}

            {isLoading && isTelegramLinked && (
              <div className='flex flex-col items-center justify-center py-12'>
                <div className='relative'>
                  <div className='w-8 h-8 border-2 border-gray-700 rounded-full'></div>
                  <div className='w-8 h-8 border-2 border-transparent border-t-blue-500 rounded-full animate-spin absolute top-0'></div>
                </div>
                <p className='text-gray-400 mt-4'>Discovering servers...</p>
              </div>
            )}

            {error && (
              <div className='bg-red-500/10 border border-red-500/20 rounded-xl p-4 mb-4'>
                <div className='flex items-center gap-3'>
                  <AlertCircle className='w-5 h-5 text-red-400 flex-shrink-0' />
                  <div>
                    <p className='text-red-400 font-medium'>Discovery Failed</p>
                    <p className='text-red-300/80 text-sm'>{error}</p>
                  </div>
                </div>
                <button
                  onClick={onRetry}
                  className='mt-3 px-4 py-2 bg-red-500/20 text-red-400 rounded-lg hover:bg-red-500/30 transition-colors text-sm'
                >
                  Try Again
                </button>
              </div>
            )}

            {showSuccess && (
              <div className='bg-green-500/10 border border-green-500/20 rounded-xl p-4 mb-4'>
                <div className='flex items-center gap-3'>
                  <CheckCircle2 className='w-5 h-5 text-green-400 flex-shrink-0' />
                  <div>
                    <p className='text-green-400 font-medium'>Import Successful!</p>
                    <p className='text-green-300/80 text-sm'>
                      {importProgress.imported} servers added to your dashboard
                    </p>
                  </div>
                </div>
              </div>
            )}

            {discovered && !isLoading && !error && isTelegramLinked && (
              <div>
                {/* Stats */}
                <div className='grid grid-cols-3 gap-4 mb-6'>
                  <div className='bg-gradient-to-br from-gray-800/50 to-gray-900/50 border border-gray-700/50 rounded-xl p-4 text-center backdrop-blur-sm'>
                    <div className='text-2xl font-bold text-white'>{discovered.total_count}</div>
                    <div className='text-sm text-gray-400'>Total Servers</div>
                  </div>
                  <div className='bg-gradient-to-br from-blue-500/20 to-purple-500/20 border border-blue-500/30 rounded-xl p-4 text-center backdrop-blur-sm'>
                    <div className='text-2xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent'>
                      {discovered.servers.filter(s => s.can_import).length}
                    </div>
                    <div className='text-sm text-blue-300/80'>Available</div>
                  </div>
                  <div className='bg-gradient-to-br from-gray-800/50 to-gray-900/50 border border-gray-700/50 rounded-xl p-4 text-center backdrop-blur-sm'>
                    <div className='text-2xl font-bold text-gray-400'>
                      {discovered.servers.filter(s => !s.can_import).length}
                    </div>
                    <div className='text-sm text-gray-400'>Already Added</div>
                  </div>
                </div>

                {/* Selection Controls */}
                {discovered.servers.some(s => s.can_import) && (
                  <div className='flex items-center justify-between mb-4'>
                    <div className='flex items-center gap-2'>
                      <button
                        onClick={handleSelectAll}
                        className='px-3 py-1.5 bg-gradient-to-r from-gray-800 to-gray-700 text-gray-300 rounded-lg hover:from-gray-700 hover:to-gray-600 transition-all duration-200 text-sm border border-gray-600/50'
                      >
                        Select All
                      </button>
                      <button
                        onClick={handleDeselectAll}
                        className='px-3 py-1.5 bg-gradient-to-r from-gray-800 to-gray-700 text-gray-300 rounded-lg hover:from-gray-700 hover:to-gray-600 transition-all duration-200 text-sm border border-gray-600/50'
                      >
                        Deselect All
                      </button>
                    </div>
                    <div className='text-sm text-gray-400'>
                      {selectedServers.size} of{' '}
                      {discovered.servers.filter(s => s.can_import).length} selected
                    </div>
                  </div>
                )}

                {/* Server List */}
                <div className='relative space-y-3'>
                  <div className='absolute inset-0 bg-gradient-to-b from-gray-800/10 via-gray-900/20 to-gray-800/10 rounded-2xl pointer-events-none'></div>
                  {discovered.servers.map(server => (
                    <motion.div
                      key={server.server_id}
                      initial={{ opacity: 0, x: -20 }}
                      animate={{ opacity: 1, x: 0 }}
                      transition={{ duration: 0.3 }}
                      className={`border rounded-xl p-4 transition-all cursor-pointer backdrop-blur-sm relative z-10 ${
                        server.can_import
                          ? selectedServers.has(server.server_id)
                            ? 'border-blue-500/60 bg-gradient-to-r from-blue-500/15 to-purple-500/15 shadow-lg shadow-blue-500/30'
                            : 'border-gray-700/60 bg-gray-800/50 hover:border-blue-500/40 hover:bg-gray-800/70'
                          : 'border-gray-800/60 bg-gray-800/40 opacity-60'
                      }`}
                      onClick={() => server.can_import && handleServerToggle(server.server_id)}
                    >
                      <div className='flex items-center justify-between'>
                        <div className='flex items-center gap-4'>
                          <div className='text-2xl filter drop-shadow-sm'>
                            {getServerIcon(server)}
                          </div>
                          <div>
                            <div className='flex items-center gap-2'>
                              <h3 className='font-medium text-white'>{server.hostname}</h3>
                              {!server.can_import && (
                                <span className='px-2 py-1 bg-gradient-to-r from-gray-700 to-gray-600 text-gray-400 text-xs rounded-full border border-gray-600/50'>
                                  Already Added
                                </span>
                              )}
                            </div>
                            <div className='flex items-center gap-4 mt-1 text-sm text-gray-400'>
                              <span>{server.operating_system || 'Unknown OS'}</span>
                              <span>•</span>
                              <span className='flex items-center gap-1'>
                                <Clock className='w-3 h-3' />
                                {formatLastSeen(server.last_seen)}
                              </span>
                            </div>
                          </div>
                        </div>

                        {server.can_import && (
                          <div className='flex items-center gap-3'>
                            <div className='text-right'>
                              <div className='text-xs text-gray-500'>Added via</div>
                              <div className='text-sm text-blue-400'>{server.added_via}</div>
                            </div>
                            <div
                              className={`w-5 h-5 rounded border-2 flex items-center justify-center transition-all duration-200 ${
                                selectedServers.has(server.server_id)
                                  ? 'border-blue-500 bg-gradient-to-r from-blue-500 to-purple-500 shadow-lg'
                                  : 'border-gray-600 bg-gray-800/50'
                              }`}
                            >
                              {selectedServers.has(server.server_id) && (
                                <CheckCircle2 className='w-3 h-3 text-white drop-shadow-sm' />
                              )}
                            </div>
                          </div>
                        )}
                      </div>
                    </motion.div>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Footer */}
          <div className='flex items-center justify-between p-6 border-t border-gray-800/50 bg-gradient-to-r from-gray-900/80 via-gray-800/60 to-gray-900/80 backdrop-blur-md'>
            <div className='flex items-center gap-2 text-sm text-gray-400'>
              <Shield className='w-4 h-4 text-blue-400' />
              <span>Secure import via Telegram bot</span>
            </div>

            <div className='flex items-center gap-3'>
              {/* TODO: FIX - Remove this refresh button before production release */}
              <button
                onClick={onRetry}
                disabled={importProgress.importing}
                className='px-3 py-2 bg-gradient-to-r from-gray-700/80 to-gray-600/80 text-gray-300 rounded-lg hover:from-gray-600/80 hover:to-gray-500/80 transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2 border border-gray-600/50 backdrop-blur-sm'
              >
                <RefreshCw className='w-4 h-4' />
                Refresh
              </button>

              <button
                onClick={onDismiss}
                disabled={importProgress.importing}
                className='px-4 py-2 bg-gradient-to-r from-gray-800/80 to-gray-700/80 text-gray-300 rounded-lg hover:from-gray-700/80 hover:to-gray-600/80 transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed border border-gray-600/50 backdrop-blur-sm'
              >
                {showSuccess ? 'Close' : 'Skip'}
              </button>

              {discovered && discovered.servers.some(s => s.can_import) && (
                <button
                  onClick={handleImport}
                  disabled={selectedServers.size === 0 || importProgress.importing}
                  className='px-4 py-2 bg-gradient-to-r from-blue-500 to-purple-600 text-white rounded-lg hover:from-blue-600 hover:to-purple-700 transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2 shadow-lg shadow-blue-500/20'
                >
                  {importProgress.importing ? (
                    <>
                      <Loader2 className='w-4 h-4 animate-spin' />
                      Importing...
                    </>
                  ) : (
                    <>
                      <Download className='w-4 h-4' />
                      Import {selectedServers.size} Server{selectedServers.size !== 1 ? 's' : ''}
                    </>
                  )}
                </button>
              )}
            </div>
          </div>
        </motion.div>
      </motion.div>
    </AnimatePresence>
  );
}
