'use client';

import { motion } from 'framer-motion';
import { Github, Download, ExternalLink, Code, Terminal, Globe } from 'lucide-react';
import Link from 'next/link';
import { Card } from '@/components/ui/Card';

const repositories = [
  {
    name: 'ServerEye API Backend',
    description:
      'High-performance Go REST API for server metrics collection with WebSocket support, multi-tier authentication and PostgreSQL storage',
    href: 'https://github.com/godofphonk/ServerEyeAPI',
    icon: Code,
    language: 'Go',
    languageColor: 'bg-green-500',
    tooltip:
      'Core API server that collects metrics from agents, manages servers, and provides real-time data to web dashboard via WebSocket connections',
    features: [
      'REST API with Gorilla Mux Router',
      'WebSocket Real-time Communication',
      'Multi-tier Authentication (API Key, JWT, Server Key)',
      'PostgreSQL Database with PGX Driver',
      'Metrics Collection & Storage',
      'Server Management Endpoints',
      'Docker Containerization',
      'Structured Logging with Logrus',
      'Dependency Injection with Wire',
      'Comprehensive Test Coverage',
    ],
  },
  {
    name: 'ServerEye Agent',
    description:
      'High-performance Go monitoring agent for real-time system metrics collection with WebSocket communication and Telegram integration',
    href: 'https://github.com/godofphonk/ServerEye',
    icon: Terminal,
    language: 'Go',
    languageColor: 'bg-cyan-500',
    tooltip:
      'Lightweight agent that runs on monitored servers, collects system metrics (CPU, memory, disk, network, temperature) and sends them to API via WebSocket',
    features: [
      'Real-time System Metrics Collection',
      'CPU, Memory, Disk, Network Monitoring',
      'Temperature & Hardware Sensors',
      'WebSocket Communication with TLS',
      'Telegram Bot Integration',
      'YAML Configuration with Hot Reload',
      'Systemd Service Support',
      'Cross-platform (Linux x86_64/ARM64)',
      'Minimal Resource Footprint',
    ],
  },
  {
    name: 'ServerEye Web Platform',
    description:
      'Complete monitoring platform with Next.js frontend and .NET backend for server metrics visualization and management',
    href: 'https://github.com/godofphonk/ServerEyeWeb',
    icon: Globe,
    language: 'TypeScript/C#',
    languageColor: 'bg-gradient-to-r from-blue-500 to-purple-500',
    tooltip:
      'Web dashboard that displays server metrics in real-time, manages servers and users, provides authentication and beautiful data visualization',
    features: [
      'Next.js 14 App Router Frontend',
      '.NET 8 Web API Backend',
      'Real-time Dashboard with Charts',
      'JWT Authentication & Authorization',
      'PostgreSQL Database & Redis Cache',
      'Server Management & Metrics',
      'Docker Containerization',
      'Enterprise Architecture Patterns',
    ],
  },
  {
    name: 'ServerEye Telegram Bot',
    description:
      'Telegram bot for server monitoring notifications and management with real-time alerts and interactive commands',
    href: 'https://github.com/godofphonk/ServerEyeBot',
    icon: Terminal,
    language: 'Go',
    languageColor: 'bg-blue-400',
    tooltip:
      'Telegram bot that sends real-time alerts about server issues, allows server management through chat commands and provides quick status updates',
    features: [
      'Telegram Bot API Integration',
      'Real-time Server Notifications',
      'Interactive Commands & Menu',
      'Server Metrics Display',
      'Alert Management',
      'PostgreSQL Database Storage',
      'Docker Containerization',
      'Structured Logging',
      'Health Check Endpoints',
      'Admin User Management',
    ],
  },
];

export default function RepositoriesPage() {
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
              className='text-center max-w-4xl mx-auto'
            >
              <div className='inline-flex items-center gap-2 px-4 py-2 bg-blue-500/10 border border-blue-500/20 rounded-full mb-6'>
                <Github className='w-4 h-4 text-blue-400' />
                <span className='text-sm text-blue-400'>Open Source</span>
              </div>
              <h1 className='text-5xl md:text-6xl font-bold mb-6'>Project Repositories</h1>
              <p className='text-xl text-gray-400 mb-8'>
                Explore the complete ServerEye ecosystem. All components are open-source and ready
                for contributions.
              </p>
            </motion.div>
          </div>
        </div>

        {/* Content */}
        <div className='container mx-auto px-6 py-16'>
          {/* Repository Cards */}
          <div className='grid md:grid-cols-2 gap-8 mb-16'>
            {repositories.map((repo, i) => (
              <motion.div
                key={i}
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.1 }}
              >
                <Card hover className='h-full relative group'>
                  {/* Custom Tooltip */}
                  <div className='absolute bottom-full left-1/2 transform -translate-x-1/2 mb-2 px-3 py-2 bg-gray-900 text-white text-sm rounded-lg opacity-0 group-hover:opacity-100 transition-opacity duration-200 pointer-events-none whitespace-nowrap z-50 shadow-lg border border-gray-700'>
                    {repo.tooltip}
                    <div className='absolute top-full left-1/2 transform -translate-x-1/2 -mt-1 w-2 h-2 bg-gray-900 border-r border-b border-gray-700 transform rotate-45'></div>
                  </div>
                  <div className='mb-4'>
                    <div className='flex items-start justify-between mb-4'>
                      <div className='flex items-center gap-3'>
                        <div
                          className={`w-12 h-12 ${repo.languageColor}/20 rounded-xl flex items-center justify-center`}
                        >
                          <repo.icon className='w-6 h-6 text-white' />
                        </div>
                        <div>
                          <h3 className='text-xl font-bold'>{repo.name}</h3>
                          <div className='flex items-center gap-2 mt-1'>
                            <span
                              className={`px-2 py-1 ${repo.languageColor} text-white text-xs rounded-full`}
                            >
                              {repo.language}
                            </span>
                          </div>
                        </div>
                      </div>
                      <Link
                        href={repo.href}
                        target='_blank'
                        rel='noopener noreferrer'
                        className='p-2 bg-white/10 rounded-lg hover:bg-white/20 transition-colors'
                      >
                        <ExternalLink className='w-4 h-4' />
                      </Link>
                    </div>
                  </div>
                  <div>
                    <p className='text-gray-400 mb-6'>{repo.description}</p>

                    {/* Features */}
                    <div className='space-y-2'>
                      {repo.features.map((feature, j) => (
                        <div key={j} className='flex items-center gap-2 text-sm text-gray-300'>
                          <div className='w-1.5 h-1.5 bg-blue-400 rounded-full' />
                          {feature}
                        </div>
                      ))}
                    </div>

                    {/* Action Buttons */}
                    <div className='mt-6'>
                      <Link
                        href={repo.href}
                        target='_blank'
                        rel='noopener noreferrer'
                        className='block w-full px-4 py-2 bg-blue-600 hover:bg-blue-700 rounded-lg font-medium text-center transition-colors'
                      >
                        View Repository
                      </Link>
                    </div>
                  </div>
                </Card>
              </motion.div>
            ))}
          </div>
        </div>
      </div>
    </main>
  );
}
