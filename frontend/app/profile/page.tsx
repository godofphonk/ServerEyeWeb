'use client';

import { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { User, Mail, Lock, Save, AlertCircle, CheckCircle, Trash2 } from 'lucide-react';
import { useAuth } from '@/context/AuthContext';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { apiClient } from '@/lib/api';
import { EmailChangeModal } from '@/components/auth/EmailChangeModal';
import { DeleteAccountModal } from '@/components/auth/DeleteAccountModal';
import { useToast } from '@/hooks/useToast';

export default function ProfilePage() {
  const { user, isAuthenticated, loading, isEmailVerified } = useAuth();
  const router = useRouter();
  const toast = useToast();
  const [isEditing, setIsEditing] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
  const [showEmailChangeModal, setShowEmailChangeModal] = useState(false);
  const [showDeleteAccountModal, setShowDeleteAccountModal] = useState(false);

  const [profileData, setProfileData] = useState({
    username: '',
    email: '',
  });

  const [passwordData, setPasswordData] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });

  useEffect(() => {
    if (!loading && !isAuthenticated) {
      router.push('/login');
    }
    if (user) {
      setProfileData({
        username: user.username,
        email: user.email,
      });
    }
  }, [user, isAuthenticated, loading, router]);

  const handleProfileUpdate = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSaving(true);
    setMessage(null);

    try {
      // Real API call to update profile - send both required fields
      const updateData = {
        UserName: profileData.username, // Use PascalCase like in backend DTO
        Email: profileData.email, // Always include email
      };

      // Try different endpoints
      try {
        await apiClient.put('/users/me', updateData);
      } catch (error: any) {
        if (error.response?.status === 400 && user?.id) {
          // Try by user ID as fallback
          await apiClient.put(`/users/${user.id}`, updateData);
        } else {
          throw error;
        }
      }

      setMessage({ type: 'success', text: 'Profile updated successfully' });
      setIsEditing(false);

      // Refresh user data to get updated info
      window.location.reload();
    } catch (error: any) {
      // Show specific validation errors if available
      const errorMessage = error.response?.data?.errors
        ? Object.values(error.response.data.errors).flat().join(', ')
        : error.response?.data?.message ||
          error.response?.data?.error ||
          'Failed to update profile';

      setMessage({
        type: 'error',
        text: errorMessage,
      });
    } finally {
      setIsSaving(false);
    }
  };

  const handlePasswordChange = async (e: React.FormEvent) => {
    e.preventDefault();
    setMessage(null);

    if (passwordData.newPassword !== passwordData.confirmPassword) {
      setMessage({ type: 'error', text: 'Passwords do not match' });
      return;
    }

    if (passwordData.newPassword.length < 8) {
      setMessage({ type: 'error', text: 'Password must be at least 8 characters' });
      return;
    }

    setIsSaving(true);

    try {
      // Real API call to change password
      await apiClient.put('/users/password', {
        currentPassword: passwordData.currentPassword,
        newPassword: passwordData.newPassword,
      });

      setMessage({ type: 'success', text: 'Password changed successfully' });
      setPasswordData({ currentPassword: '', newPassword: '', confirmPassword: '' });
    } catch (error: any) {
      setMessage({
        type: 'error',
        text: error.response?.data?.message || 'Failed to change password',
      });
    } finally {
      setIsSaving(false);
    }
  };

  const handleEmailChangeSuccess = (newEmail: string) => {
    setProfileData(prev => ({ ...prev, email: newEmail }));
    if (user) {
      user.email = newEmail; // Update user context
    }
  };

  if (loading || !user) {
    return (
      <div className='min-h-screen bg-black flex items-center justify-center'>
        <div className='text-white'>Loading...</div>
      </div>
    );
  }

  return (
    <main className='min-h-screen bg-black text-white'>
      <div className='absolute inset-0 bg-gradient-to-br from-blue-600/10 via-purple-600/10 to-pink-600/10' />

      <div className='relative z-10'>
        <div className='border-b border-white/10 bg-black/50 backdrop-blur-xl'>
          <div className='container mx-auto px-6 py-6'>
            <h1 className='text-3xl font-bold'>Profile Settings</h1>
          </div>
        </div>

        <div className='container mx-auto px-6 py-8 max-w-4xl'>
          {message && (
            <motion.div
              initial={{ opacity: 0, y: -10 }}
              animate={{ opacity: 1, y: 0 }}
              className={`mb-6 p-4 rounded-xl flex items-center gap-3 ${
                message.type === 'success'
                  ? 'bg-green-500/10 border border-green-500/20'
                  : 'bg-red-500/10 border border-red-500/20'
              }`}
            >
              {message.type === 'success' ? (
                <CheckCircle className='w-5 h-5 text-green-400' />
              ) : (
                <AlertCircle className='w-5 h-5 text-red-400' />
              )}
              <p
                className={`text-sm ${message.type === 'success' ? 'text-green-400' : 'text-red-400'}`}
              >
                {message.text}
              </p>
            </motion.div>
          )}

          {/* Profile Information */}
          <Card className='mb-8'>
            <CardHeader>
              <div className='flex items-center justify-between'>
                <div className='flex items-center gap-3'>
                  <div className='w-12 h-12 bg-gradient-to-br from-blue-600 to-purple-600 rounded-full flex items-center justify-center'>
                    <User className='w-6 h-6' />
                  </div>
                  <div>
                    <CardTitle>Profile Information</CardTitle>
                    <p className='text-sm text-gray-400 mt-1'>Update your account details</p>
                  </div>
                </div>
                {!isEditing && (
                  <Button variant='secondary' onClick={() => setIsEditing(true)}>
                    Edit
                  </Button>
                )}
              </div>
            </CardHeader>
            <CardContent>
              <form onSubmit={handleProfileUpdate} className='space-y-6'>
                <Input
                  label='Username'
                  value={profileData.username}
                  onChange={e => setProfileData({ ...profileData, username: e.target.value })}
                  disabled={!isEditing || isSaving}
                />
                <div className='space-y-2'>
                  <div className='relative'>
                    <Input
                      label='Email'
                      type='email'
                      value={profileData.email}
                      onChange={e => setProfileData({ ...profileData, email: e.target.value })}
                      disabled={!isEditing || isSaving}
                    />
                    {!isEditing && (
                      <div className='absolute top-8 right-3'>
                        {isEmailVerified ? (
                          <div className='flex items-center gap-1 text-green-400'>
                            <CheckCircle className='w-4 h-4' />
                            <span className='text-xs'>Verified</span>
                          </div>
                        ) : (
                          <div className='flex items-center gap-1 text-yellow-400'>
                            <AlertCircle className='w-4 h-4' />
                            <span className='text-xs'>Not verified</span>
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                  {!isEditing && (
                    <div className='flex gap-2'>
                      <Button
                        type='button'
                        variant='secondary'
                        size='sm'
                        onClick={() => setShowEmailChangeModal(true)}
                        className='mt-2'
                      >
                        <Mail className='w-4 h-4 mr-2' />
                        Change Email
                      </Button>
                      {!isEmailVerified && (
                        <Button
                          type='button'
                          variant='ghost'
                          size='sm'
                          onClick={() => setShowEmailChangeModal(true)}
                          className='mt-2 text-yellow-400 hover:text-yellow-300'
                        >
                          <AlertCircle className='w-4 h-4 mr-2' />
                          Verify Now
                        </Button>
                      )}
                    </div>
                  )}
                </div>
                {isEditing && (
                  <div className='flex gap-4'>
                    <Button type='submit' isLoading={isSaving}>
                      <Save className='w-4 h-4 mr-2' />
                      Save Changes
                    </Button>
                    <Button
                      type='button'
                      variant='secondary'
                      onClick={() => {
                        setIsEditing(false);
                        setProfileData({
                          username: user.username,
                          email: user.email,
                        });
                      }}
                      disabled={isSaving}
                    >
                      Cancel
                    </Button>
                  </div>
                )}
              </form>
            </CardContent>
          </Card>

          {/* Change Password */}
          <Card>
            <CardHeader>
              <div className='flex items-center gap-3'>
                <div className='w-12 h-12 bg-purple-500/20 rounded-full flex items-center justify-center'>
                  <Lock className='w-6 h-6 text-purple-400' />
                </div>
                <div>
                  <CardTitle>Change Password</CardTitle>
                  <p className='text-sm text-gray-400 mt-1'>Update your password</p>
                </div>
              </div>
            </CardHeader>
            <CardContent>
              <form onSubmit={handlePasswordChange} className='space-y-6'>
                <Input
                  type='password'
                  label='Current Password'
                  value={passwordData.currentPassword}
                  onChange={e =>
                    setPasswordData({ ...passwordData, currentPassword: e.target.value })
                  }
                  required
                  disabled={isSaving}
                />
                <Input
                  type='password'
                  label='New Password'
                  value={passwordData.newPassword}
                  onChange={e => setPasswordData({ ...passwordData, newPassword: e.target.value })}
                  required
                  disabled={isSaving}
                  helperText='Must be at least 8 characters'
                />
                <Input
                  type='password'
                  label='Confirm New Password'
                  value={passwordData.confirmPassword}
                  onChange={e =>
                    setPasswordData({ ...passwordData, confirmPassword: e.target.value })
                  }
                  required
                  disabled={isSaving}
                />
                <Button type='submit' isLoading={isSaving}>
                  <Lock className='w-4 h-4 mr-2' />
                  Change Password
                </Button>
              </form>
            </CardContent>
          </Card>

          {/* Danger Zone */}
          <Card className='mt-8 border-red-500/20'>
            <CardHeader>
              <CardTitle className='text-red-400'>Danger Zone</CardTitle>
            </CardHeader>
            <CardContent>
              <div className='space-y-4'>
                <div className='bg-red-500/10 border border-red-500/20 rounded-lg p-4'>
                  <h4 className='font-semibold text-red-400 mb-2'>Delete Account</h4>
                  <p className='text-sm text-gray-300 mb-4'>
                    Permanently delete your account and all associated data. This action cannot be
                    undone.
                  </p>
                  <Button variant='danger' onClick={() => setShowDeleteAccountModal(true)}>
                    <Trash2 className='w-4 h-4 mr-2' />
                    Delete Account
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Account Info */}
          <Card className='mt-8'>
            <CardHeader>
              <CardTitle>Account Information</CardTitle>
            </CardHeader>
            <CardContent>
              <div className='space-y-4 text-sm'>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Account ID</span>
                  <span className='font-mono'>{user.id}</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Role</span>
                  <span className='capitalize'>{user.role}</span>
                </div>
                <div className='flex justify-between'>
                  <span className='text-gray-400'>Member Since</span>
                  <span>{new Date(user.createdAt).toLocaleDateString()}</span>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Email Change Modal */}
      <EmailChangeModal
        isOpen={showEmailChangeModal}
        onClose={() => setShowEmailChangeModal(false)}
        currentEmail={profileData.email}
        onSuccess={handleEmailChangeSuccess}
      />

      {/* Delete Account Modal */}
      <DeleteAccountModal
        isOpen={showDeleteAccountModal}
        onClose={() => setShowDeleteAccountModal(false)}
        email={profileData.email}
      />
    </main>
  );
}
