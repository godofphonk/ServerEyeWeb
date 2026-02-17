"use client";

import { useState } from 'react';
import { Cpu, HardDrive, Database, Wifi, Monitor } from 'lucide-react';
import { TabsNavigation, TabsContent, TabPanel } from '@/components/ui/Tabs';
import CpuTab from './CpuTab';
import MemoryTab from './MemoryTab';
import StorageTab from './StorageTab';
import NetworkTab from './NetworkTab';
import SystemTab from './SystemTab';
import { DashboardMetrics, MetricsResponse } from '@/types';

interface MetricsTabsProps {
  dashboardMetrics: DashboardMetrics | null;
  historicalMetrics: MetricsResponse | null;
  server: any;
}

const tabs = [
  { id: 'cpu', label: 'CPU', icon: Cpu },
  { id: 'memory', label: 'Memory', icon: HardDrive },
  { id: 'storage', label: 'Storage', icon: Database },
  { id: 'network', label: 'Network', icon: Wifi },
  { id: 'system', label: 'System', icon: Monitor },
];

export default function MetricsTabs({ dashboardMetrics, historicalMetrics, server }: MetricsTabsProps) {
  const [activeTab, setActiveTab] = useState('cpu');

  return (
    <div className="space-y-6">
      <TabsNavigation
        tabs={tabs}
        activeTab={activeTab}
        onTabChange={setActiveTab}
      />
      
      <TabsContent activeTab={activeTab}>
        <TabPanel value="cpu" activeTab={activeTab}>
          <CpuTab 
            dashboardMetrics={dashboardMetrics}
            historicalMetrics={historicalMetrics}
          />
        </TabPanel>
        
        <TabPanel value="memory" activeTab={activeTab}>
          <MemoryTab 
            dashboardMetrics={dashboardMetrics}
            historicalMetrics={historicalMetrics}
          />
        </TabPanel>
        
        <TabPanel value="storage" activeTab={activeTab}>
          <StorageTab 
            dashboardMetrics={dashboardMetrics}
            historicalMetrics={historicalMetrics}
          />
        </TabPanel>
        
        <TabPanel value="network" activeTab={activeTab}>
          <NetworkTab 
            dashboardMetrics={dashboardMetrics}
            historicalMetrics={historicalMetrics}
          />
        </TabPanel>
        
        <TabPanel value="system" activeTab={activeTab}>
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
