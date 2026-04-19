'use client';

import { LucideIcon } from 'lucide-react';
import { motion } from 'framer-motion';

interface TabItem {
  id: string;
  label: string;
  icon: LucideIcon;
  disabled?: boolean;
}

interface TabsNavigationProps {
  tabs: TabItem[];
  activeTab: string;
  onTabChange: (tabId: string) => void;
}

export function TabsNavigation({ tabs, activeTab, onTabChange }: TabsNavigationProps) {
  return (
    <div className='border-b border-white/10'>
      <nav className='-mb-px flex space-x-8 overflow-x-auto'>
        {tabs.map(tab => {
          const Icon = tab.icon;
          const isActive = activeTab === tab.id;

          return (
            <motion.button
              key={tab.id}
              onClick={() => !tab.disabled && onTabChange(tab.id)}
              disabled={tab.disabled}
              whileHover={{ scale: 1.02 }}
              whileTap={{ scale: 0.98 }}
              className={`
                group relative min-w-0 flex items-center gap-2 py-4 px-1 border-b-2 font-medium text-sm transition-colors
                ${
                  isActive
                    ? 'border-purple-500 text-purple-400'
                    : tab.disabled
                      ? 'border-transparent text-gray-500 cursor-not-allowed'
                      : 'border-transparent text-gray-400 hover:text-gray-300 hover:border-gray-600'
                }
              `}
            >
              <motion.div animate={{ rotate: isActive ? 360 : 0 }} transition={{ duration: 0.5 }}>
                <Icon className='w-4 h-4' />
              </motion.div>
              <span className='truncate'>{tab.label}</span>
              {isActive && (
                <motion.div
                  layoutId='activeTab'
                  className='absolute bottom-0 left-0 right-0 h-0.5 bg-gradient-to-r from-purple-500 to-pink-500'
                  initial={false}
                  animate={{ opacity: 1 }}
                  transition={{ duration: 0.3 }}
                />
              )}
            </motion.button>
          );
        })}
      </nav>
    </div>
  );
}

interface TabsContentProps {
  activeTab?: string;
  children: React.ReactNode;
}

export function TabsContent({ activeTab: _activeTab, children }: TabsContentProps) {
  return <div className='mt-6'>{children}</div>;
}

interface TabPanelProps {
  value: string;
  activeTab: string;
  children: React.ReactNode;
}

export function TabPanel({ value, activeTab, children }: TabPanelProps) {
  if (value !== activeTab) {
    return null;
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -10 }}
      transition={{ duration: 0.3 }}
    >
      {children}
    </motion.div>
  );
}
