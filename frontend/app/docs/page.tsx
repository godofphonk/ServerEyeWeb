'use client';

import { motion } from 'framer-motion';
import { Book, Code, Terminal, GitBranch } from 'lucide-react';
import Link from 'next/link';
import { Card, CardContent } from '@/components/ui/Card';

export default function DocsPage() {
  return (
    <main className='min-h-screen bg-black text-white'>
      <div className='absolute inset-0 bg-gradient-to-br from-blue-600/10 via-purple-600/10 to-pink-600/10' />

      <div className='relative z-10'>
        {/* Header */}
        <div className='border-b border-white/10 bg-black/50 backdrop-blur-xl'>
          <div className='container mx-auto px-6 py-12'>
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              className='text-center max-w-3xl mx-auto'
            >
              <div className='inline-flex items-center gap-2 px-4 py-2 bg-blue-500/10 border border-blue-500/20 rounded-full mb-6'>
                <Book className='w-4 h-4 text-blue-400' />
                <span className='text-sm text-blue-400'>Documentation</span>
              </div>
              <h1 className='text-5xl md:text-6xl font-bold mb-6'>How can we help?</h1>
              <p className='text-xl text-gray-400 mb-8'>
                Everything you need to know about ServerEye
              </p>
            </motion.div>
          </div>
        </div>

        {/* Content */}
        <div className='container mx-auto px-6 py-16'>
          {/* Quick Links */}
          <div className='grid md:grid-cols-3 gap-6 mb-16'>
            {[
              {
                icon: Terminal,
                title: 'Quick Start',
                desc: 'Get up and running in 60 seconds',
                href: '/install',
              },
              {
                icon: Code,
                title: 'Agent Customizations',
                desc: 'Configure and customize monitoring agent',
                href: 'https://github.com/godofphonk/ServerEye/blob/master/docs/configuration.md',
              },
              {
                icon: GitBranch,
                title: 'Repositories',
                desc: 'Explore project source code and examples',
                href: '/docs/repositories',
              },
            ].map((link, i) => (
              <motion.div
                key={i}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.1 }}
              >
                <Link
                  href={link.href}
                  target={link.href.startsWith('http') ? '_blank' : '_self'}
                  rel={link.href.startsWith('http') ? 'noopener noreferrer' : undefined}
                >
                  <Card hover className='h-full'>
                    <CardContent className='pt-6'>
                      <div className='w-12 h-12 bg-blue-500/20 rounded-xl flex items-center justify-center mb-4'>
                        <link.icon className='w-6 h-6 text-blue-400' />
                      </div>
                      <h3 className='text-xl font-bold mb-2'>{link.title}</h3>
                      <p className='text-gray-400'>{link.desc}</p>
                    </CardContent>
                  </Card>
                </Link>
              </motion.div>
            ))}
          </div>

          {/* Help Section */}
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.8 }}
            className='mt-16'
          >
            <Card>
              <CardContent className='text-center py-12'>
                <h3 className='text-2xl font-bold mb-4'>Still need help?</h3>
                <p className='text-gray-400 mb-6'>
                  Can't find what you're looking for? Our support team is here to help.
                </p>
                <div className='flex flex-col sm:flex-row gap-4 justify-center'>
                  <Link
                    href='/support'
                    className='px-6 py-3 bg-blue-600 hover:bg-blue-700 rounded-xl font-semibold transition-colors'
                  >
                    Contact Support
                  </Link>
                </div>
              </CardContent>
            </Card>
          </motion.div>
        </div>
      </div>
    </main>
  );
}
