'use client';

import { useState } from 'react';
import { Cpu, HardDrive, Database, Wifi, Monitor } from 'lucide-react';
import { TabsNavigation, TabsContent, TabPanel } from '@/components/ui/Tabs';
import CpuTab from './CpuTab';
import MemoryTab from './MemoryTab';
import StorageTab from './StorageTab';
import NetworkTab from './NetworkTab';
import SystemTab from './SystemTab';
import { DashboardMetrics, MetricsResponse, ServerStaticInfo } from '@/types';

interface MetricsTabsProps {
  dashboardMetrics: DashboardMetrics | null;
  historicalMetrics: MetricsResponse | null; // Unified metrics for all charts
  staticInfo: ServerStaticInfo | null;
  server: any;
  networkDetails?: any;
  activeTab?: string;
  onActiveTabChange?: (tab: string) => void;
  loadHistoricalMetrics?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => Promise<MetricsResponse>;
}

const tabs = [
  { id: 'cpu', label: 'CPU', icon: Cpu },
  { id: 'memory', label: 'Memory', icon: HardDrive },
  { id: 'storage', label: 'Storage', icon: Database },
  { id: 'network', label: 'Network', icon: Wifi },
  { id: 'system', label: 'System', icon: Monitor },
];

export default function MetricsTabs({
  dashboardMetrics,
  historicalMetrics, // Use unified metrics for all tabs
  staticInfo,
  server,
  networkDetails,
  activeTab = 'cpu',
  onActiveTabChange,
  loadHistoricalMetrics,
}: MetricsTabsProps) {
  return (
    <div className='space-y-6'>
      <TabsNavigation
        tabs={tabs}
        activeTab={activeTab}
        onTabChange={onActiveTabChange || (() => {})}
      />

      <TabsContent activeTab={activeTab}>
        <TabPanel value='cpu' activeTab={activeTab}>
          <CpuTab
            dashboardMetrics={dashboardMetrics}
            historicalMetrics={historicalMetrics} // Use unified metrics
            staticInfo={staticInfo}
            loadHistoricalMetrics={loadHistoricalMetrics}
          />
        </TabPanel>

        <TabPanel value='memory' activeTab={activeTab}>
          <MemoryTab
            dashboardMetrics={dashboardMetrics}
            historicalMetrics={historicalMetrics} // Use unified metrics
            staticInfo={staticInfo}
            loadHistoricalMetrics={loadHistoricalMetrics}
          />
        </TabPanel>

        <TabPanel value='storage' activeTab={activeTab}>
          <StorageTab
            dashboardMetrics={dashboardMetrics}
            historicalMetrics={historicalMetrics} // Use unified metrics
            staticInfo={staticInfo}
            loadHistoricalMetrics={loadHistoricalMetrics}
          />
        </TabPanel>

        <TabPanel value='network' activeTab={activeTab}>
          <NetworkTab
            dashboardMetrics={dashboardMetrics}
            historicalMetrics={historicalMetrics} // Use unified metrics
            networkDetails={networkDetails}
            loadHistoricalMetrics={loadHistoricalMetrics}
          />
        </TabPanel>

        <TabPanel value='system' activeTab={activeTab}>
          <SystemTab
            dashboardMetrics={dashboardMetrics}
            historicalMetrics={historicalMetrics}
            server={server}
          />
        </TabPanel>
      </TabsContent>
    </div>
  );
}
