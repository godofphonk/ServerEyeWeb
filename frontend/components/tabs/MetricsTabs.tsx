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
  cpuHistoricalMetrics: MetricsResponse | null;
  cpuUsageHistoricalMetrics: MetricsResponse | null;
  cpuLoadHistoricalMetrics: MetricsResponse | null;
  memoryHistoricalMetrics: MetricsResponse | null;
  networkHistoricalMetrics: MetricsResponse | null;
  diskHistoricalMetrics: MetricsResponse | null;
  server: any;
  timeRange?: string;
  cpuTimeRange?: string;
  cpuUsageTimeRange?: string;
  cpuLoadTimeRange?: string;
  memoryTimeRange?: string;
  networkTimeRange?: string;
  diskTimeRange?: string;
  onTimeRangeChange?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
  onCpuTimeRangeChange?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
  onCpuUsageTimeRangeChange?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
  onCpuLoadTimeRangeChange?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
  onMemoryTimeRangeChange?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
  onNetworkTimeRangeChange?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
  onDiskTimeRangeChange?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
  networkDetails?: any;
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
  historicalMetrics, 
  cpuHistoricalMetrics,
  cpuUsageHistoricalMetrics,
  cpuLoadHistoricalMetrics,
  memoryHistoricalMetrics,
  networkHistoricalMetrics,
  diskHistoricalMetrics,
  server, 
  timeRange, 
  cpuTimeRange,
  cpuUsageTimeRange,
  cpuLoadTimeRange,
  memoryTimeRange,
  networkTimeRange,
  diskTimeRange,
  onTimeRangeChange, 
  onCpuTimeRangeChange,
  onCpuUsageTimeRangeChange,
  onCpuLoadTimeRangeChange,
  onMemoryTimeRangeChange,
  onNetworkTimeRangeChange,
  onDiskTimeRangeChange,
  networkDetails 
}: MetricsTabsProps) {
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
            historicalMetrics={cpuHistoricalMetrics}
            cpuUsageHistoricalMetrics={cpuUsageHistoricalMetrics}
            cpuLoadHistoricalMetrics={cpuLoadHistoricalMetrics}
            cpuUsageTimeRange={cpuUsageTimeRange}
            cpuLoadTimeRange={cpuLoadTimeRange}
            onCpuUsageTimeRangeChange={onCpuUsageTimeRangeChange}
            onCpuLoadTimeRangeChange={onCpuLoadTimeRangeChange}
          />
        </TabPanel>
        
        <TabPanel value="memory" activeTab={activeTab}>
          <MemoryTab 
            dashboardMetrics={dashboardMetrics}
            historicalMetrics={memoryHistoricalMetrics}
            timeRange={memoryTimeRange}
            onTimeRangeChange={onMemoryTimeRangeChange}
          />
        </TabPanel>
        
        <TabPanel value="storage" activeTab={activeTab}>
          <StorageTab 
            dashboardMetrics={dashboardMetrics}
            historicalMetrics={diskHistoricalMetrics}
            timeRange={diskTimeRange}
            onTimeRangeChange={onDiskTimeRangeChange}
          />
        </TabPanel>
        
        <TabPanel value="network" activeTab={activeTab}>
          <NetworkTab 
            dashboardMetrics={dashboardMetrics}
            historicalMetrics={networkHistoricalMetrics}
            networkDetails={networkDetails}
            timeRange={networkTimeRange}
            onTimeRangeChange={onNetworkTimeRangeChange}
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
