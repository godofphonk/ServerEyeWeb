'use client';

import { LucideIcon } from 'lucide-react';

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
    <div className='border-b border-gray-700'>
      <nav className='-mb-px flex space-x-8 overflow-x-auto'>
        {tabs.map(tab => {
          const Icon = tab.icon;
          const isActive = activeTab === tab.id;

          return (
            <button
              key={tab.id}
              onClick={() => !tab.disabled && onTabChange(tab.id)}
              disabled={tab.disabled}
              className={`
                group relative min-w-0 flex items-center gap-2 py-4 px-1 border-b-2 font-medium text-sm transition-colors
                ${
                  isActive
                    ? 'border-blue-500 text-blue-400'
                    : tab.disabled
                      ? 'border-transparent text-gray-500 cursor-not-allowed'
                      : 'border-transparent text-gray-400 hover:text-gray-300 hover:border-gray-600'
                }
              `}
            >
              <Icon className='w-4 h-4' />
              <span className='truncate'>{tab.label}</span>
              {isActive && <div className='absolute bottom-0 left-0 right-0 h-0.5 bg-blue-500' />}
            </button>
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

  return <div className='animate-in fade-in-0 duration-200'>{children}</div>;
}
