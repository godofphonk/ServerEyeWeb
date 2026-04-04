'use client';

import { useState } from 'react';
import { motion } from 'framer-motion';
import { Settings, Globe, Send, Link } from 'lucide-react';
import SourceManagementModal from './SourceManagementModal';

interface ServerSourcesBadgeProps {
  serverKey: string;
  serverId: string;
  hostname: string;
  sources?: string[];
  compact?: boolean;
  onSourceUpdated?: () => void;
}

export default function ServerSourcesBadge({
  serverKey,
  serverId,
  hostname,
  sources = [],
  compact = false,
  onSourceUpdated,
}: ServerSourcesBadgeProps) {
  const [showModal, setShowModal] = useState(false);

  const getSourceIcon = (sourceType: string) => {
    switch (sourceType.toLowerCase()) {
      case 'web':
        return <Globe className='w-3 h-3' />;
      case 'telegram':
      case 'tgbot':
        return <Send className='w-3 h-3' />;
      default:
        return <Link className='w-3 h-3' />;
    }
  };

  const getSourceColor = (sourceType: string) => {
    switch (sourceType.toLowerCase()) {
      case 'web':
        return 'bg-blue-500/20 text-blue-400 border-blue-500/30';
      case 'telegram':
      case 'tgbot':
        return 'bg-sky-500/20 text-sky-400 border-sky-500/30';
      default:
        return 'bg-gray-500/20 text-gray-400 border-gray-500/30';
    }
  };

  const getSourceLabel = (sourceType: string) => {
    switch (sourceType.toLowerCase()) {
      case 'web':
        return 'Web';
      case 'telegram':
      case 'tgbot':
        return 'Telegram';
      default:
        return sourceType;
    }
  };

  if (sources.length === 0) {
    return null;
  }

  if (compact) {
    return (
      <>
        <div className='flex items-center gap-1'>
          {sources.slice(0, 2).map(source => (
            <div
              key={source}
              className={`flex items-center gap-1 px-2 py-1 rounded-full border text-xs ${getSourceColor(source)}`}
              title={`${getSourceLabel(source)} source`}
            >
              {getSourceIcon(source)}
            </div>
          ))}
          {sources.length > 2 && (
            <div className='flex items-center gap-1 px-2 py-1 rounded-full border text-xs bg-gray-500/20 text-gray-400 border-gray-500/30'>
              +{sources.length - 2}
            </div>
          )}
          <button
            onClick={() => setShowModal(true)}
            className='p-1 rounded hover:bg-white/10 transition-colors'
            title='Manage sources'
          >
            <Settings className='w-3 h-3 text-gray-400' />
          </button>
        </div>

        <SourceManagementModal
          isOpen={showModal}
          onClose={() => setShowModal(false)}
          serverKey={serverKey}
          serverId={serverId}
          hostname={hostname}
          sources={sources}
          onSourceDeleted={() => {
            onSourceUpdated?.();
            setShowModal(false);
          }}
        />
      </>
    );
  }

  return (
    <>
      <div className='flex items-center gap-2'>
        <span className='text-xs text-gray-400'>Sources:</span>
        <div className='flex items-center gap-1 flex-wrap'>
          {sources.map(source => (
            <motion.div
              key={source}
              initial={{ opacity: 0, scale: 0.8 }}
              animate={{ opacity: 1, scale: 1 }}
              className={`flex items-center gap-1 px-2 py-1 rounded-full border text-xs ${getSourceColor(source)}`}
            >
              {getSourceIcon(source)}
              <span>{getSourceLabel(source)}</span>
            </motion.div>
          ))}
        </div>
        <button
          onClick={() => setShowModal(true)}
          className='p-1 rounded hover:bg-white/10 transition-colors ml-2'
          title='Manage sources'
        >
          <Settings className='w-4 h-4 text-gray-400' />
        </button>
      </div>

      <SourceManagementModal
        isOpen={showModal}
        onClose={() => setShowModal(false)}
        serverKey={serverKey}
        serverId={serverId}
        hostname={hostname}
        sources={sources}
        onSourceDeleted={() => {
          onSourceUpdated?.();
          setShowModal(false);
        }}
      />
    </>
  );
}
