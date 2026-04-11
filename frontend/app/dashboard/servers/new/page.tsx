'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Card, CardContent, CardHeader } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { ArrowLeft, Server as ServerIcon, AlertCircle, Download } from 'lucide-react';
import { motion } from 'framer-motion';
import { apiClient } from '@/lib/api';
import { clearServersCache, clearMetricsCache } from '@/lib/serverApi';
import { useToast } from '@/hooks/useToast';
import { AxiosApiError } from '@/types';

export default function AddServerPage() {
  const router = useRouter();
  const toast = useToast();
  const [serverName, setServerName] = useState('');
  const [apiKey, setApiKey] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  const handleAddServer = async () => {
    if (!serverName.trim()) {
      setError('Please enter a server name');
      return;
    }

    if (!apiKey.trim()) {
      setError('Please enter your API key');
      return;
    }

    if (!apiKey.startsWith('srv_') && !apiKey.startsWith('key_')) {
      setError("Invalid API key format. Key must start with 'srv_' or 'key_'");
      return;
    }

    setIsLoading(true);
    setError('');

    try {
      await apiClient.post('/monitoredservers/add', {
        serverKey: apiKey,
        serverName: serverName.trim(),
      });

      // Clear caches so dashboard shows the new server
      clearServersCache();
      clearMetricsCache();

      toast.success('Server Added', `Server "${serverName.trim()}" has been successfully added`);

      router.push('/dashboard');
    } catch (error: unknown) {

      const errorMessage =
        (error as AxiosApiError)?.response?.data?.message ||
        (error as AxiosApiError)?.message ||
        'Failed to add server. Please check your API key.';

      setError(errorMessage);

      toast.error('Add Failed', `Failed to add server: ${errorMessage}`);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main className='min-h-screen bg-black text-white'>
      <div className='absolute inset-0 bg-gradient-to-br from-blue-600/10 via-purple-600/10 to-pink-600/10' />

      <div className='relative z-10'>
        {/* Header */}
        <div className='border-b border-white/10 bg-black/50 backdrop-blur-sm'>
          <div className='container mx-auto px-6 py-6'>
            <div className='flex items-center gap-4'>
              <Button variant='secondary' onClick={() => router.back()}>
                <ArrowLeft className='w-5 h-5' />
              </Button>
              <div>
                <h1 className='text-3xl font-bold'>Add Server</h1>
                <p className='text-gray-400'>Connect your monitored server</p>
              </div>
            </div>
          </div>
        </div>

        {/* Content */}
        <div className='container mx-auto px-6 py-8'>
          <div className='max-w-3xl mx-auto space-y-6'>
            {/* Installation Instructions */}
            <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }}>
              <Card className='border-blue-500/20'>
                <CardHeader>
                  <div className='flex items-center gap-3 mb-2'>
                    <div className='w-12 h-12 rounded-lg bg-blue-600/20 flex items-center justify-center'>
                      <Download className='w-6 h-6 text-blue-400' />
                    </div>
                    <div>
                      <h2 className='text-xl font-bold'>Step 1: Install Agent</h2>
                      <p className='text-sm text-gray-400'>Run this command on your server</p>
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className='p-4 bg-black/50 rounded-lg border border-white/10'>
                    <code className='text-sm text-green-400'>
                      wget -qO-
                      https://raw.githubusercontent.com/godofphonk/ServerEye/master/scripts/install-agent.sh
                      | sudo bash
                    </code>
                  </div>
                  <p className='text-sm text-gray-400 mt-3'>
                    💡 After installation, you'll receive an API key in the format:{' '}
                    <code className='text-blue-400'>srv_xxxxx...</code>
                  </p>
                </CardContent>
              </Card>
            </motion.div>

            {/* Add Server Form */}
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: 0.1 }}
            >
              <Card>
                <CardHeader>
                  <div className='flex items-center gap-3 mb-2'>
                    <div className='w-12 h-12 rounded-lg bg-purple-600/20 flex items-center justify-center'>
                      <ServerIcon className='w-6 h-6 text-purple-400' />
                    </div>
                    <div>
                      <h2 className='text-xl font-bold'>Step 2: Add to Dashboard</h2>
                      <p className='text-sm text-gray-400'>Enter your server details</p>
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className='space-y-6'>
                    {error && (
                      <div className='p-4 bg-red-500/10 border border-red-500/20 rounded-lg flex items-center gap-3'>
                        <AlertCircle className='w-5 h-5 text-red-400 flex-shrink-0' />
                        <p className='text-sm text-red-400'>{error}</p>
                      </div>
                    )}

                    <div>
                      <label className='block text-sm font-medium mb-2'>
                        Server Name <span className='text-red-400'>*</span>
                      </label>
                      <Input
                        value={serverName}
                        onChange={e => setServerName(e.target.value)}
                        placeholder='e.g., Production Server'
                        className='w-full'
                      />
                    </div>

                    <div>
                      <label className='block text-sm font-medium mb-2'>
                        API Key <span className='text-red-400'>*</span>
                      </label>
                      <Input
                        value={apiKey}
                        onChange={e => setApiKey(e.target.value)}
                        placeholder='srv_684eab33c7d2f1e9a8b5c4d3e2f1a0b9'
                        className='w-full font-mono'
                      />
                      <p className='text-xs text-gray-400 mt-1'>
                        The key you received after installing the agent
                      </p>
                    </div>

                    <Button
                      onClick={handleAddServer}
                      disabled={isLoading || !serverName.trim() || !apiKey.trim()}
                      className='w-full'
                    >
                      {isLoading ? 'Adding Server...' : 'Add Server'}
                    </Button>
                  </div>
                </CardContent>
              </Card>
            </motion.div>
          </div>
        </div>
      </div>
    </main>
  );
}
