'use client';

import { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { 
  Link as LinkIcon, 
  Unlink, 
  CheckCircle, 
  AlertCircle, 
  Loader2,
  ExternalLink
} from 'lucide-react';
import { useAuth } from '@/context/AuthContext';
import { Button } from '@/components/ui/Button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';
import { ExternalLogin } from '@/types';
import { useToast } from '@/hooks/useToast';

interface OAuthSettingsProps {
  className?: string;
}

export function OAuthSettings({ className }: OAuthSettingsProps) {
  const { getExternalLogins, linkExternalAccount, unlinkExternalAccount, getOAuthChallenge } = useAuth();
  const [externalLogins, setExternalLogins] = useState<ExternalLogin[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isLinking, setIsLinking] = useState<string | null>(null);
  const [isUnlinking, setIsUnlinking] = useState<string | null>(null);
  const toast = useToast();

  const loadExternalLogins = async () => {
    try {
      const response = await getExternalLogins();
      setExternalLogins(response.externalLogins || []);
    } catch (error: any) {
      console.error('Failed to load external logins:', error);
      toast.error('Error', 'Failed to load connected accounts');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadExternalLogins();
  }, []);

  const handleLinkAccount = async (provider: string) => {
    setIsLinking(provider);
    
    try {
      // Get OAuth challenge URL with special callback for linking
      const challenge = await getOAuthChallenge(provider, '/api/auth/oauth/link-callback');
      
      // Store return URL in sessionStorage
      if (typeof window !== 'undefined') {
        sessionStorage.setItem('oauth_return_url', '/profile');
        sessionStorage.setItem('oauth_action', 'link');
      }
      
      // Redirect to OAuth provider
      window.location.href = challenge.challengeUrl;
    } catch (error: any) {
      console.error('Failed to link account:', error);
      toast.error('Error', `Failed to link ${provider} account`);
      setIsLinking(null);
    }
  };

  const handleUnlinkAccount = async (provider: string) => {
    if (!confirm(`Are you sure you want to unlink your ${provider} account?`)) {
      return;
    }

    setIsUnlinking(provider);
    
    try {
      await unlinkExternalAccount(provider);
      toast.success('Success', `${provider} account unlinked successfully`);
      await loadExternalLogins(); // Reload the list
    } catch (error: any) {
      console.error('Failed to unlink account:', error);
      toast.error('Error', `Failed to unlink ${provider} account`);
    } finally {
      setIsUnlinking(null);
    }
  };

  const getProviderIcon = (provider: string) => {
    switch (provider.toLowerCase()) {
      case 'google':
        return (
          <svg className='w-5 h-5' viewBox='0 0 24 24'>
            <path
              fill='currentColor'
              d='M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z'
            />
            <path
              fill='currentColor'
              d='M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z'
            />
            <path
              fill='currentColor'
              d='M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z'
            />
            <path
              fill='currentColor'
              d='M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z'
            />
          </svg>
        );
      case 'github':
        return (
          <svg className='w-5 h-5' viewBox='0 0 24 24' fill='currentColor'>
            <path d='M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z'/>
          </svg>
        );
      case 'telegram':
        return (
          <svg className='w-5 h-5' viewBox='0 0 24 24' fill='currentColor'>
            <path d='M12 0C5.373 0 0 5.373 0 12s5.373 12 12 12 12-5.373 12-12S18.627 0 12 0zm5.894 8.221l-1.97 9.28c-.145.658-.537.818-1.084.508l-3-2.21-1.446 1.394c-.14.18-.357.295-.6.295-.002 0-.003 0-.005 0l.213-3.054 5.56-5.022c.24-.213-.054-.334-.373-.121l-6.869 4.326-2.96-.924c-.64-.203-.658-.64.135-.954l11.566-4.458c.538-.196 1.006.128.832.941z'/>
          </svg>
        );
      default:
        return <ExternalLink className='w-5 h-5' />;
    }
  };

  const availableProviders = [
    { name: 'Google', key: 'google', available: true },
    { name: 'GitHub', key: 'github', available: false, reason: 'Coming Soon' },
    { name: 'Telegram', key: 'telegram', available: false, reason: 'Coming Soon' },
  ];

  if (isLoading) {
    return (
      <Card className={className}>
        <CardContent className='p-6'>
          <div className='flex items-center justify-center py-8'>
            <Loader2 className='w-6 h-6 animate-spin text-blue-400' />
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className={className}>
      <CardHeader>
        <div className='flex items-center gap-3'>
          <div className='w-12 h-12 bg-blue-500/20 rounded-full flex items-center justify-center'>
            <LinkIcon className='w-6 h-6 text-blue-400' />
          </div>
          <div>
            <CardTitle>Connected Accounts</CardTitle>
            <p className='text-sm text-gray-400 mt-1'>
              Link your social accounts for easier login
            </p>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <div className='space-y-4'>
          {availableProviders.map((provider) => {
            const linkedAccount = externalLogins.find(
              (login) => login.provider.toLowerCase() === provider.key.toLowerCase()
            );

            return (
              <motion.div
                key={provider.key}
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.1 * availableProviders.indexOf(provider) }}
                className='flex items-center justify-between p-4 rounded-lg border border-white/10 bg-white/5'
              >
                <div className='flex items-center gap-3'>
                  <div className='w-10 h-10 bg-gray-800 rounded-full flex items-center justify-center'>
                    {getProviderIcon(provider.name)}
                  </div>
                  <div>
                    <p className='font-medium'>{provider.name}</p>
                    {linkedAccount ? (
                      <p className='text-sm text-gray-400 flex items-center gap-1'>
                        <CheckCircle className='w-3 h-3 text-green-400' />
                        {linkedAccount.providerDisplayName}
                      </p>
                    ) : provider.available ? (
                      <p className='text-sm text-gray-400'>Not connected</p>
                    ) : (
                      <p className='text-sm text-yellow-400'>{provider.reason}</p>
                    )}
                  </div>
                </div>

                <div>
                  {linkedAccount ? (
                    <Button
                      variant='ghost'
                      size='sm'
                      onClick={() => handleUnlinkAccount(provider.key)}
                      disabled={isUnlinking === provider.key}
                      className='text-red-400 hover:text-red-300 hover:bg-red-500/10'
                    >
                      {isUnlinking === provider.key ? (
                        <Loader2 className='w-4 h-4 animate-spin' />
                      ) : (
                        <Unlink className='w-4 h-4' />
                      )}
                    </Button>
                  ) : provider.available ? (
                    <Button
                      variant='secondary'
                      size='sm'
                      onClick={() => handleLinkAccount(provider.key)}
                      disabled={isLinking === provider.key}
                    >
                      {isLinking === provider.key ? (
                        <Loader2 className='w-4 h-4 animate-spin' />
                      ) : (
                        <LinkIcon className='w-4 h-4' />
                      )}
                    </Button>
                  ) : (
                    <Button variant='ghost' size='sm' disabled className='opacity-50'>
                      <AlertCircle className='w-4 h-4' />
                    </Button>
                  )}
                </div>
              </motion.div>
            );
          })}
        </div>

        <div className='mt-6 p-4 bg-blue-500/10 border border-blue-500/20 rounded-lg'>
          <p className='text-sm text-blue-400'>
            <strong>Note:</strong> Connecting an account allows you to sign in with that provider. 
            Your existing login method will continue to work.
          </p>
        </div>
      </CardContent>
    </Card>
  );
}
