'use client';

import { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { X, Share2, Mail, Shield, UserPlus, Trash2 } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Card } from '@/components/ui/Card';
import { apiClient } from '@/lib/api';
import { AccessLevel } from '@/types';

interface SharedUser {
  userId: string;
  email: string;
  accessLevel: AccessLevel;
  sharedAt: string;
}

interface ShareServerModalProps {
  isOpen: boolean;
  onClose: () => void;
  serverId: string;
  serverName: string;
  currentAccessLevel: AccessLevel;
}

export default function ShareServerModal({
  isOpen,
  onClose,
  serverId,
  serverName,
  currentAccessLevel,
}: ShareServerModalProps) {
  const [email, setEmail] = useState('');
  const [accessLevel, setAccessLevel] = useState<'Admin' | 'Viewer'>('Viewer');
  const [isSharing, setIsSharing] = useState(false);
  const [error, setError] = useState('');
  const [sharedUsers, setSharedUsers] = useState<SharedUser[]>([]);
  const [loadingUsers, setLoadingUsers] = useState(false);

  const canShare = currentAccessLevel === 'Owner';

  const loadSharedUsers = async () => {
    if (!canShare) return;

    try {
      setLoadingUsers(true);
      // TODO: Implement API endpoint to get shared users
      // const users = await apiClient.get<SharedUser[]>(`/monitoredservers/${serverId}/users`);
      // setSharedUsers(users);
    } catch (err) {
      setError('Failed to load shared users');
    } finally {
      setLoadingUsers(false);
    }
  };

  const handleShare = async () => {
    if (!email.trim()) {
      setError('Please enter an email address');
      return;
    }

    if (!email.includes('@')) {
      setError('Please enter a valid email address');
      return;
    }

    setIsSharing(true);
    setError('');

    try {
      await apiClient.post(`/monitoredservers/${serverId}/share`, {
        userEmail: email,
        accessLevel: accessLevel,
      });

      setEmail('');
      setAccessLevel('Viewer');
      await loadSharedUsers();
    } catch (err: any) {
      setError(err?.response?.data?.message || 'Failed to share server. Please try again.');
    } finally {
      setIsSharing(false);
    }
  };

  const handleRemoveUser = async (userId: string) => {
    if (!confirm("Are you sure you want to remove this user's access?")) {
      return;
    }

    try {
      await apiClient.delete(`/monitoredservers/${serverId}/users/${userId}`);
      await loadSharedUsers();
    } catch (err) {
      setError('Failed to remove user');
      alert('Failed to remove user access. Please try again.');
    }
  };

  if (!isOpen) return null;

  return (
    <AnimatePresence>
      <div className='fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm'>
        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          exit={{ opacity: 0, scale: 0.95 }}
          className='bg-gray-900 border border-white/10 rounded-xl max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto'
        >
          {/* Header */}
          <div className='flex items-center justify-between p-6 border-b border-white/10'>
            <div className='flex items-center gap-3'>
              <div className='w-12 h-12 bg-blue-500/20 rounded-full flex items-center justify-center'>
                <Share2 className='w-6 h-6 text-blue-400' />
              </div>
              <div>
                <h2 className='text-xl font-bold'>Share Server</h2>
                <p className='text-sm text-gray-400'>{serverName}</p>
              </div>
            </div>
            <button
              onClick={onClose}
              className='p-2 hover:bg-white/10 rounded-lg transition-colors'
            >
              <X className='w-5 h-5' />
            </button>
          </div>

          {/* Content */}
          <div className='p-6 space-y-6'>
            {!canShare ? (
              <Card className='p-6 bg-yellow-500/10 border-yellow-500/20'>
                <div className='flex items-center gap-3'>
                  <Shield className='w-6 h-6 text-yellow-400' />
                  <div>
                    <p className='font-semibold'>Limited Access</p>
                    <p className='text-sm text-gray-400 mt-1'>
                      Only the server owner can share access with other users.
                    </p>
                  </div>
                </div>
              </Card>
            ) : (
              <>
                {/* Share Form */}
                <div className='space-y-4'>
                  <div>
                    <label className='block text-sm font-medium mb-2'>
                      Email Address <span className='text-red-400'>*</span>
                    </label>
                    <div className='relative'>
                      <Mail className='absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400' />
                      <Input
                        type='email'
                        value={email}
                        onChange={e => setEmail(e.target.value)}
                        placeholder='user@example.com'
                        className='pl-10'
                        disabled={isSharing}
                      />
                    </div>
                  </div>

                  <div>
                    <label className='block text-sm font-medium mb-2'>
                      Access Level <span className='text-red-400'>*</span>
                    </label>
                    <div className='grid grid-cols-2 gap-3'>
                      <button
                        onClick={() => setAccessLevel('Viewer')}
                        disabled={isSharing}
                        className={`p-4 rounded-lg border-2 transition-all ${
                          accessLevel === 'Viewer'
                            ? 'border-blue-500 bg-blue-500/10'
                            : 'border-white/10 bg-white/5 hover:bg-white/10'
                        }`}
                      >
                        <div className='text-left'>
                          <p className='font-semibold mb-1'>Viewer</p>
                          <p className='text-xs text-gray-400'>Can only view metrics</p>
                        </div>
                      </button>

                      <button
                        onClick={() => setAccessLevel('Admin')}
                        disabled={isSharing}
                        className={`p-4 rounded-lg border-2 transition-all ${
                          accessLevel === 'Admin'
                            ? 'border-blue-500 bg-blue-500/10'
                            : 'border-white/10 bg-white/5 hover:bg-white/10'
                        }`}
                      >
                        <div className='text-left'>
                          <p className='font-semibold mb-1'>Admin</p>
                          <p className='text-xs text-gray-400'>Can view and manage</p>
                        </div>
                      </button>
                    </div>
                  </div>

                  {error && (
                    <div className='p-3 bg-red-500/10 border border-red-500/20 rounded-lg'>
                      <p className='text-sm text-red-400'>{error}</p>
                    </div>
                  )}

                  <Button
                    onClick={handleShare}
                    disabled={isSharing || !email.trim()}
                    className='w-full'
                  >
                    {isSharing ? (
                      <>
                        <div className='w-4 h-4 border-2 border-white/20 border-t-white rounded-full animate-spin mr-2' />
                        Sharing...
                      </>
                    ) : (
                      <>
                        <UserPlus className='w-4 h-4 mr-2' />
                        Share Server
                      </>
                    )}
                  </Button>
                </div>

                {/* Shared Users List */}
                <div className='border-t border-white/10 pt-6'>
                  <h3 className='text-lg font-semibold mb-4'>Shared With</h3>

                  {loadingUsers ? (
                    <div className='text-center py-8 text-gray-400'>
                      <div className='inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500'></div>
                    </div>
                  ) : sharedUsers.length === 0 ? (
                    <div className='text-center py-8 text-gray-400'>
                      <Shield className='w-12 h-12 mx-auto mb-3 opacity-50' />
                      <p>Not shared with anyone yet</p>
                    </div>
                  ) : (
                    <div className='space-y-3'>
                      {sharedUsers.map(user => (
                        <div
                          key={user.userId}
                          className='flex items-center justify-between p-4 bg-white/5 rounded-lg border border-white/10'
                        >
                          <div className='flex items-center gap-3'>
                            <div className='w-10 h-10 bg-blue-500/20 rounded-full flex items-center justify-center'>
                              <Mail className='w-5 h-5 text-blue-400' />
                            </div>
                            <div>
                              <p className='font-semibold'>{user.email}</p>
                              <p className='text-xs text-gray-400'>
                                Shared {new Date(user.sharedAt).toLocaleDateString()}
                              </p>
                            </div>
                          </div>
                          <div className='flex items-center gap-3'>
                            <span
                              className={`px-3 py-1 rounded-lg text-sm font-medium ${
                                user.accessLevel === 'Admin'
                                  ? 'bg-purple-500/20 text-purple-400'
                                  : 'bg-blue-500/20 text-blue-400'
                              }`}
                            >
                              {user.accessLevel}
                            </span>
                            <button
                              onClick={() => handleRemoveUser(user.userId)}
                              className='p-2 hover:bg-red-500/20 rounded-lg transition-colors group'
                              title='Remove access'
                            >
                              <Trash2 className='w-4 h-4 text-gray-400 group-hover:text-red-400' />
                            </button>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </>
            )}
          </div>

          {/* Footer */}
          <div className='flex justify-end gap-3 p-6 border-t border-white/10'>
            <Button variant='secondary' onClick={onClose}>
              Close
            </Button>
          </div>
        </motion.div>
      </div>
    </AnimatePresence>
  );
}
