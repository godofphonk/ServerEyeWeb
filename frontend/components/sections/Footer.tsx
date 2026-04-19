'use client';

import { motion } from 'framer-motion';
import { GitBranch, Globe, MessageCircle, Mail, Sparkles } from 'lucide-react';

export default function Footer() {
  const links = {
    product: [
      { name: 'Features', href: '#' },
      { name: 'Pricing', href: '#' },
      { name: 'Documentation', href: '#' },
      { name: 'API', href: '#' },
    ],
    company: [
      { name: 'About', href: '#' },
      { name: 'Blog', href: '#' },
      { name: 'Careers', href: '#' },
      { name: 'Contact', href: '#' },
    ],
    legal: [
      { name: 'Privacy', href: '#' },
      { name: 'Terms', href: '#' },
      { name: 'Security', href: '#' },
      { name: 'Status', href: '#' },
    ],
  };

  const socials = [
    { icon: GitBranch, href: 'https://github.com/godofphonk/ServerEye', label: 'GitHub' },
    { icon: Globe, href: '#', label: 'Twitter' },
    { icon: MessageCircle, href: '#', label: 'Discord' },
    { icon: Mail, href: '#', label: 'Email' },
  ];

  return (
    <footer className='relative border-t border-white/10 bg-black overflow-hidden'>
      <div className='absolute inset-0 bg-gradient-to-b from-purple-900/5 to-transparent' />
      <div className='container mx-auto px-6 py-20 relative z-10'>
        <div className='grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-12 mb-12'>
          {/* Brand */}
          <div className='lg:col-span-2'>
            <motion.div whileHover={{ scale: 1.05 }} className='flex items-center gap-2 mb-4'>
              <motion.div
                animate={{ rotate: [0, 360] }}
                transition={{ duration: 20, repeat: Infinity, ease: 'linear' }}
                className='w-10 h-10 bg-gradient-to-br from-blue-600 to-purple-600 rounded-lg flex items-center justify-center shadow-lg shadow-purple-500/30'
              >
                <Sparkles className='w-6 h-6 text-white' />
              </motion.div>
              <span className='text-2xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-blue-400 to-purple-400'>
                ServerEye
              </span>
            </motion.div>
            <p className='text-gray-400 mb-6 max-w-sm'>
              Modern server monitoring made simple. Open source, secure, and built for developers.
            </p>
            <div className='flex gap-4'>
              {socials.map((social, i) => (
                <motion.a
                  key={i}
                  href={social.href}
                  target='_blank'
                  rel='noopener noreferrer'
                  whileHover={{ scale: 1.1, y: -5 }}
                  whileTap={{ scale: 0.95 }}
                  className='w-10 h-10 bg-white/5 hover:bg-white/10 border border-white/10 rounded-lg flex items-center justify-center transition-all duration-300 shadow-lg hover:shadow-purple-500/20'
                  aria-label={social.label}
                >
                  <social.icon className='w-5 h-5' />
                </motion.a>
              ))}
            </div>
          </div>

          {/* Links */}
          <div>
            <h3 className='font-semibold mb-4'>Product</h3>
            <ul className='space-y-3'>
              {links.product.map((link, i) => (
                <li key={i}>
                  <motion.a
                    whileHover={{ x: 5 }}
                    href={link.href}
                    className='text-gray-400 hover:text-white transition-colors inline-block'
                  >
                    {link.name}
                  </motion.a>
                </li>
              ))}
            </ul>
          </div>

          <div>
            <h3 className='font-semibold mb-4'>Company</h3>
            <ul className='space-y-3'>
              {links.company.map((link, i) => (
                <li key={i}>
                  <motion.a
                    whileHover={{ x: 5 }}
                    href={link.href}
                    className='text-gray-400 hover:text-white transition-colors inline-block'
                  >
                    {link.name}
                  </motion.a>
                </li>
              ))}
            </ul>
          </div>

          <div>
            <h3 className='font-semibold mb-4'>Legal</h3>
            <ul className='space-y-3'>
              {links.legal.map((link, i) => (
                <li key={i}>
                  <motion.a
                    whileHover={{ x: 5 }}
                    href={link.href}
                    className='text-gray-400 hover:text-white transition-colors inline-block'
                  >
                    {link.name}
                  </motion.a>
                </li>
              ))}
            </ul>
          </div>
        </div>

        {/* Bottom */}
        <div className='pt-8 border-t border-white/10 flex flex-col md:flex-row justify-between items-center gap-4'>
          <p className='text-gray-500 text-sm'>© 2024 ServerEye. All rights reserved.</p>
          <div className='flex items-center gap-6 text-sm text-gray-500'>
            <span>Made with ❤️ by developers</span>
            <span>•</span>
            <motion.a
              whileHover={{ scale: 1.05 }}
              href='#'
              className='hover:text-white transition-colors'
            >
              Open Source
            </motion.a>
          </div>
        </div>
      </div>
    </footer>
  );
}
