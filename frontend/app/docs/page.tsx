'use client';

import { useState } from 'react';
import { motion } from 'framer-motion';
import { Search, Book, Code, Terminal, Zap, Shield, GitBranch } from 'lucide-react';
import Link from 'next/link';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/Card';

const docSections = [
  {
    title: 'Getting Started',
    icon: Zap,
    articles: [
      { title: 'Quick Start Guide', href: '/install' },
      { title: 'Installation', href: '/docs/installation' },
      { title: 'Configuration', href: '/docs/configuration' },
      { title: 'First Steps', href: '/docs/first-steps' },
    ],
  },
  {
    title: 'Agent Customizations',
    icon: Code,
    articles: [
      { title: 'Configuration Options', href: '/docs/agent/config' },
      { title: 'Custom Metrics', href: '/docs/agent/metrics' },
      { title: 'Alert Rules', href: '/docs/agent/alerts' },
      { title: 'Plugins', href: '/docs/agent/plugins' },
    ],
  },
  {
    title: 'Project Repositories',
    icon: GitBranch,
    articles: [
      { title: 'View All Repositories', href: '/docs/repositories' },
      { title: 'Backend API', href: 'https://github.com/godofphonk/ServerEye' },
      { title: 'Monitoring Agent', href: 'https://github.com/godofphonk/ServerEye-agent' },
      { title: 'Web Dashboard', href: 'https://github.com/godofphonk/ServerEye-web' },
    ],
  },
  {
    title: 'Security',
    icon: Shield,
    articles: [
      { title: 'Best Practices', href: '/docs/security/best-practices' },
      { title: 'Authentication', href: '/docs/security/auth' },
      { title: 'API Keys', href: '/docs/security/api-keys' },
      { title: 'Compliance', href: '/docs/security/compliance' },
    ],
  },
];

export default function DocsPage() {
  const [searchQuery, setSearchQuery] = useState('');

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

              {/* Search */}
              <div className='relative'>
                <Search className='absolute left-4 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400' />
                <input
                  type='text'
                  placeholder='Search documentation...'
                  value={searchQuery}
                  onChange={e => setSearchQuery(e.target.value)}
                  className='w-full pl-12 pr-4 py-4 bg-white/10 backdrop-blur-sm border border-white/20 rounded-2xl text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500'
                />
              </div>
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
                href: '/docs/agent',
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

          {/* Documentation Sections */}
          <div className='grid md:grid-cols-2 gap-8'>
            {docSections.map((section, i) => (
              <motion.div
                key={i}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: 0.3 + i * 0.1 }}
              >
                <Card>
                  <CardHeader>
                    <div className='flex items-center gap-3 mb-4'>
                      <div className='w-10 h-10 bg-purple-500/20 rounded-xl flex items-center justify-center'>
                        <section.icon className='w-5 h-5 text-purple-400' />
                      </div>
                      <CardTitle>{section.title}</CardTitle>
                    </div>
                  </CardHeader>
                  <CardContent>
                    <div className='space-y-3'>
                      {section.articles.map((article, j) => (
                        <Link
                          key={j}
                          href={article.href}
                          target={article.href.startsWith('http') ? '_blank' : '_self'}
                          rel={article.href.startsWith('http') ? 'noopener noreferrer' : undefined}
                          className='block p-3 rounded-xl hover:bg-white/5 transition-colors'
                        >
                          <div className='flex items-center justify-between'>
                            <span className='text-gray-300'>{article.title}</span>
                            <svg
                              className='w-4 h-4 text-gray-500'
                              fill='none'
                              viewBox='0 0 24 24'
                              stroke='currentColor'
                            >
                              <path
                                strokeLinecap='round'
                                strokeLinejoin='round'
                                strokeWidth={2}
                                d='M9 5l7 7-7 7'
                              />
                            </svg>
                          </div>
                        </Link>
                      ))}
                    </div>
                  </CardContent>
                </Card>
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
                  <Link
                    href='https://github.com/godofphonk/ServerEye'
                    className='px-6 py-3 bg-white/10 hover:bg-white/20 border border-white/20 rounded-xl font-semibold transition-colors'
                  >
                    GitHub Discussions
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
