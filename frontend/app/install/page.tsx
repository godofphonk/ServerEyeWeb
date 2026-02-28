'use client';

import { useState } from 'react';
import { motion } from 'framer-motion';
import {
  Download,
  Terminal,
  Package,
  CheckCircle,
  Copy,
  Server,
  Database,
  Activity,
  Lock,
} from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';

const installCommands = {
  linux: `curl -fsSL https://get.servereye.io/install.sh | sudo bash`,
  macos: `curl -fsSL https://get.servereye.io/install.sh | bash`,
  windows: `powershell -c "irm https://get.servereye.io/install.ps1 | iex"`,
};

const systemRequirements = [
  { icon: Server, title: 'CPU', description: 'x86_64 or ARM64 architecture' },
  { icon: Database, title: 'Memory', description: 'Minimum 512MB RAM' },
  { icon: Package, title: 'Storage', description: '100MB free disk space' },
  { icon: Activity, title: 'Network', description: 'Internet connection for monitoring' },
];

export default function InstallPage() {
  const [selectedOS, setSelectedOS] = useState<'linux' | 'macos' | 'windows'>('linux');
  const [copied, setCopied] = useState(false);

  const handleCopy = async (command: string) => {
    await navigator.clipboard.writeText(command);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className='min-h-screen bg-gradient-to-br from-gray-900 via-black to-gray-900'>
      {/* Hero Section */}
      <div className='relative overflow-hidden'>
        <div className='absolute inset-0 bg-gradient-to-r from-blue-600/20 to-purple-600/20' />
        <div className='relative container mx-auto px-6 py-24'>
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6 }}
            className='text-center max-w-4xl mx-auto'
          >
            <div className='flex justify-center mb-6'>
              <div className='p-4 bg-gradient-to-br from-blue-600 to-purple-600 rounded-2xl'>
                <Download className='w-12 h-12 text-white' />
              </div>
            </div>
            <h1 className='text-5xl font-bold text-white mb-6'>Install ServerEye</h1>
            <p className='text-xl text-gray-300 mb-8'>
              Get started with ServerEye in seconds. Monitor your servers with our lightweight
              agent.
            </p>
          </motion.div>
        </div>
      </div>

      {/* Installation Steps */}
      <div className='container mx-auto px-6 py-16'>
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6, delay: 0.2 }}
          className='max-w-4xl mx-auto'
        >
          <h2 className='text-3xl font-bold text-white mb-12 text-center'>Quick Installation</h2>

          {/* OS Selection */}
          <div className='flex justify-center mb-8'>
            <div className='bg-white/10 backdrop-blur-lg rounded-xl p-1 flex gap-1'>
              {(['linux', 'macos', 'windows'] as const).map(os => (
                <button
                  key={os}
                  onClick={() => os === 'linux' && setSelectedOS(os)}
                  disabled={os !== 'linux'}
                  className={`px-6 py-3 rounded-lg font-medium transition-all flex items-center gap-2 ${
                    selectedOS === os
                      ? 'bg-gradient-to-r from-blue-600 to-purple-600 text-white'
                      : os === 'linux'
                        ? 'text-gray-300 hover:text-white hover:bg-white/10'
                        : 'text-gray-500 cursor-not-allowed opacity-60'
                  }`}
                >
                  {os.charAt(0).toUpperCase() + os.slice(1)}
                  {os !== 'linux' && <Lock className='w-4 h-4' />}
                </button>
              ))}
            </div>
          </div>

          {/* Installation Command */}
          <Card className='mb-16'>
            <CardHeader>
              <CardTitle className='flex items-center gap-3'>
                <Terminal className='w-6 h-6 text-blue-400' />
                Installation Command
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className='bg-black/50 rounded-lg p-4 font-mono text-sm'>
                <div className='flex items-center justify-between'>
                  <code className='text-green-400'>{installCommands[selectedOS]}</code>
                  <Button
                    variant='ghost'
                    size='sm'
                    onClick={() => handleCopy(installCommands[selectedOS])}
                    className='ml-4'
                  >
                    {copied ? (
                      <CheckCircle className='w-4 h-4 text-green-400' />
                    ) : (
                      <Copy className='w-4 h-4' />
                    )}
                  </Button>
                </div>
              </div>
              <p className='text-gray-400 mt-4 text-sm'>
                Run this command in your terminal to install ServerEye agent.
              </p>
            </CardContent>
          </Card>

          {/* System Requirements */}
          <h2 className='text-3xl font-bold text-white mb-12 text-center'>System Requirements</h2>

          <div className='grid md:grid-cols-2 lg:grid-cols-4 gap-6 mb-16'>
            {systemRequirements.map((req, index) => {
              const Icon = req.icon;
              return (
                <motion.div
                  key={req.title}
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.6, delay: 0.3 + index * 0.1 }}
                >
                  <Card className='text-center h-full'>
                    <CardContent className='p-6'>
                      <div className='flex justify-center mb-4'>
                        <div className='p-3 bg-gradient-to-br from-blue-600 to-purple-600 rounded-xl'>
                          <Icon className='w-6 h-6 text-white' />
                        </div>
                      </div>
                      <h3 className='text-lg font-semibold text-white mb-2'>{req.title}</h3>
                      <p className='text-gray-400 text-sm'>{req.description}</p>
                    </CardContent>
                  </Card>
                </motion.div>
              );
            })}
          </div>

          {/* Manual Installation */}
          <Card>
            <CardHeader>
              <CardTitle className='flex items-center gap-3'>
                <Package className='w-6 h-6 text-purple-400' />
                Manual Installation
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className='space-y-4'>
                <div className='flex items-start gap-3'>
                  <div className='w-8 h-8 bg-blue-600 text-white rounded-full flex items-center justify-center text-sm font-semibold'>
                    1
                  </div>
                  <div>
                    <h4 className='text-white font-semibold mb-1'>Download the agent</h4>
                    <p className='text-gray-400 text-sm'>
                      Download the ServerEye agent binary for your platform from our releases page.
                    </p>
                  </div>
                </div>
                <div className='flex items-start gap-3'>
                  <div className='w-8 h-8 bg-blue-600 text-white rounded-full flex items-center justify-center text-sm font-semibold'>
                    2
                  </div>
                  <div>
                    <h4 className='text-white font-semibold mb-1'>Configure the agent</h4>
                    <p className='text-gray-400 text-sm'>
                      Create a configuration file with your API key and server settings.
                    </p>
                  </div>
                </div>
                <div className='flex items-start gap-3'>
                  <div className='w-8 h-8 bg-blue-600 text-white rounded-full flex items-center justify-center text-sm font-semibold'>
                    3
                  </div>
                  <div>
                    <h4 className='text-white font-semibold mb-1'>Start monitoring</h4>
                    <p className='text-gray-400 text-sm'>
                      Run the agent and start monitoring your server metrics in real-time.
                    </p>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </motion.div>
      </div>
    </div>
  );
}
