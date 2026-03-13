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
} from 'lucide-react';
import { DiscoveredServersResponse, DiscoveredServer } from '@/types';

interface TelegramServerDiscoveryModalProps {
  isOpen: boolean;
  discovered: DiscoveredServersResponse | null;
  isLoading: boolean;
  error: string | null;
  onImport: (serverIds: string[]) => Promise<void>;
  onDismiss: () => void;
  onRetry: () => void;
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
}: TelegramServerDiscoveryModalProps) {
  const [selectedServers, setSelectedServers] = useState<Set<string>>(new Set());
  const [importProgress, setImportProgress] = useState<ImportProgress>({
    importing: false,
    imported: 0,
    total: 0,
    errors: [],
  });
  const [showSuccess, setShowSuccess] = useState(false);

  // Reset selection when discovered data changes
  React.useEffect(() => {
    if (discovered) {
      const importableServerIds = discovered.servers
        .filter(server => server.can_import)
        .map(server => server.server_id);
      
      setSelectedServers(new Set(importableServerIds));
    }
  }, [discovered]);

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
        className="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4"
        onClick={onDismiss}
      >
        <motion.div
          initial={{ opacity: 0, scale: 0.95, y: 20 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.95, y: 20 }}
          transition={{ type: "spring", duration: 0.5, bounce: 0.3 }}
          className="bg-gray-900 border border-gray-800 rounded-2xl shadow-2xl max-w-4xl w-full max-h-[85vh] overflow-hidden"
          onClick={(e) => e.stopPropagation()}
        >
          {/* Header */}
          <div className="flex items-center justify-between p-6 border-b border-gray-800">
            <div className="flex items-center gap-3">
              <div className="w-12 h-12 bg-blue-500/20 rounded-xl flex items-center justify-center">
                <Bot className="w-6 h-6 text-blue-400" />
              </div>
              <div>
                <h2 className="text-xl font-bold text-white">Telegram Servers Discovered</h2>
                <p className="text-gray-400 text-sm">
                  Found servers connected to your Telegram bot
                </p>
              </div>
            </div>
            <button
              onClick={onDismiss}
              className="p-2 hover:bg-gray-800 rounded-lg transition-colors"
              disabled={importProgress.importing}
            >
              <X className="w-5 h-5 text-gray-400" />
            </button>
          </div>

          {/* Content */}
          <div className="p-6 overflow-y-auto max-h-[50vh]">
            {isLoading && (
              <div className="flex flex-col items-center justify-center py-12">
                <Loader2 className="w-8 h-8 text-blue-400 animate-spin mb-4" />
                <p className="text-gray-400">Discovering servers...</p>
              </div>
            )}

            {error && (
              <div className="bg-red-500/10 border border-red-500/20 rounded-xl p-4 mb-4">
                <div className="flex items-center gap-3">
                  <AlertCircle className="w-5 h-5 text-red-400 flex-shrink-0" />
                  <div>
                    <p className="text-red-400 font-medium">Discovery Failed</p>
                    <p className="text-red-300/80 text-sm">{error}</p>
                  </div>
                </div>
                <button
                  onClick={onRetry}
                  className="mt-3 px-4 py-2 bg-red-500/20 text-red-400 rounded-lg hover:bg-red-500/30 transition-colors text-sm"
                >
                  Try Again
                </button>
              </div>
            )}

            {showSuccess && (
              <div className="bg-green-500/10 border border-green-500/20 rounded-xl p-4 mb-4">
                <div className="flex items-center gap-3">
                  <CheckCircle2 className="w-5 h-5 text-green-400 flex-shrink-0" />
                  <div>
                    <p className="text-green-400 font-medium">Import Successful!</p>
                    <p className="text-green-300/80 text-sm">
                      {importProgress.imported} servers added to your dashboard
                    </p>
                  </div>
                </div>
              </div>
            )}

            {discovered && !isLoading && !error && (
              <div>
                {/* Stats */}
                <div className="grid grid-cols-3 gap-4 mb-6">
                  <div className="bg-gray-800/50 rounded-xl p-4 text-center">
                    <div className="text-2xl font-bold text-white">{discovered.total_count}</div>
                    <div className="text-sm text-gray-400">Total Servers</div>
                  </div>
                  <div className="bg-blue-500/10 border border-blue-500/20 rounded-xl p-4 text-center">
                    <div className="text-2xl font-bold text-blue-400">
                      {discovered.servers.filter(s => s.can_import).length}
                    </div>
                    <div className="text-sm text-blue-300/80">Available</div>
                  </div>
                  <div className="bg-gray-800/50 rounded-xl p-4 text-center">
                    <div className="text-2xl font-bold text-gray-400">
                      {discovered.servers.filter(s => !s.can_import).length}
                    </div>
                    <div className="text-sm text-gray-400">Already Added</div>
                  </div>
                </div>

                {/* Selection Controls */}
                {discovered.servers.some(s => s.can_import) && (
                  <div className="flex items-center justify-between mb-4">
                    <div className="flex items-center gap-2">
                      <button
                        onClick={handleSelectAll}
                        className="px-3 py-1.5 bg-gray-800 text-gray-300 rounded-lg hover:bg-gray-700 transition-colors text-sm"
                      >
                        Select All
                      </button>
                      <button
                        onClick={handleDeselectAll}
                        className="px-3 py-1.5 bg-gray-800 text-gray-300 rounded-lg hover:bg-gray-700 transition-colors text-sm"
                      >
                        Deselect All
                      </button>
                    </div>
                    <div className="text-sm text-gray-400">
                      {selectedServers.size} of {discovered.servers.filter(s => s.can_import).length} selected
                    </div>
                  </div>
                )}

                {/* Server List */}
                <div className="space-y-3">
                  {discovered.servers.map((server) => (
                    <motion.div
                      key={server.server_id}
                      initial={{ opacity: 0, x: -20 }}
                      animate={{ opacity: 1, x: 0 }}
                      transition={{ duration: 0.3 }}
                      className={`border rounded-xl p-4 transition-all cursor-pointer ${
                        server.can_import
                          ? selectedServers.has(server.server_id)
                            ? 'border-blue-500 bg-blue-500/10'
                            : 'border-gray-700 bg-gray-800/30 hover:border-gray-600'
                          : 'border-gray-800 bg-gray-800/50 opacity-60'
                      }`}
                      onClick={() => server.can_import && handleServerToggle(server.server_id)}
                    >
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-4">
                          <div className="text-2xl">{getServerIcon(server)}</div>
                          <div>
                            <div className="flex items-center gap-2">
                              <h3 className="font-medium text-white">{server.hostname}</h3>
                              {!server.can_import && (
                                <span className="px-2 py-1 bg-gray-700 text-gray-400 text-xs rounded-full">
                                  Already Added
                                </span>
                              )}
                            </div>
                            <div className="flex items-center gap-4 mt-1 text-sm text-gray-400">
                              <span>{server.operating_system || 'Unknown OS'}</span>
                              <span>•</span>
                              <span className="flex items-center gap-1">
                                <Clock className="w-3 h-3" />
                                {formatLastSeen(server.last_seen)}
                              </span>
                            </div>
                          </div>
                        </div>
                        
                        {server.can_import && (
                          <div className="flex items-center gap-3">
                            <div className="text-right">
                              <div className="text-xs text-gray-500">Added via</div>
                              <div className="text-sm text-blue-400">{server.added_via}</div>
                            </div>
                            <div className={`w-5 h-5 rounded border-2 flex items-center justify-center ${
                              selectedServers.has(server.server_id)
                                ? 'border-blue-500 bg-blue-500'
                                : 'border-gray-600'
                            }`}>
                              {selectedServers.has(server.server_id) && (
                                <CheckCircle2 className="w-3 h-3 text-white" />
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
          <div className="flex items-center justify-between p-6 border-t border-gray-800">
            <div className="flex items-center gap-2 text-sm text-gray-400">
              <Shield className="w-4 h-4" />
              <span>Secure import via Telegram bot</span>
            </div>
            
            <div className="flex items-center gap-3">
              {/* TODO: FIX - Remove this refresh button before production release */}
              <button
                onClick={onRetry}
                disabled={importProgress.importing}
                className="px-3 py-2 bg-gray-700 text-gray-300 rounded-lg hover:bg-gray-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
              >
                <RefreshCw className="w-4 h-4" />
                Refresh
              </button>
              
              <button
                onClick={onDismiss}
                disabled={importProgress.importing}
                className="px-4 py-2 bg-gray-800 text-gray-300 rounded-lg hover:bg-gray-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {showSuccess ? 'Close' : 'Skip'}
              </button>
              
              {discovered && discovered.servers.some(s => s.can_import) && (
                <button
                  onClick={handleImport}
                  disabled={selectedServers.size === 0 || importProgress.importing}
                  className="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                >
                  {importProgress.importing ? (
                    <>
                      <Loader2 className="w-4 h-4 animate-spin" />
                      Importing...
                    </>
                  ) : (
                    <>
                      <Download className="w-4 h-4" />
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
