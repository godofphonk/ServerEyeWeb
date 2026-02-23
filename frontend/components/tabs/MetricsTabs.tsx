"use client";

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
  historicalMetrics: MetricsResponse | null;
  cpuHistoricalMetrics: MetricsResponse | null;
  cpuUsageHistoricalMetrics: MetricsResponse | null;
  cpuLoadHistoricalMetrics: MetricsResponse | null;
  cpuTemperatureHistoricalMetrics: MetricsResponse | null;
  memoryHistoricalMetrics: MetricsResponse | null;
  networkHistoricalMetrics: MetricsResponse | null;
  diskHistoricalMetrics: MetricsResponse | null;
  staticInfo: ServerStaticInfo | null;
  server: any;
  timeRange?: '1h' | '6h' | '24h' | '7d' | '30d';
  cpuTimeRange?: '1h' | '6h' | '24h' | '7d' | '30d';
  cpuUsageTimeRange?: '1h' | '6h' | '24h' | '7d' | '30d';
  cpuLoadTimeRange?: '1h' | '6h' | '24h' | '7d' | '30d';
  cpuTemperatureTimeRange?: '1h' | '6h' | '24h' | '7d' | '30d';
  memoryTimeRange?: '1h' | '6h' | '24h' | '7d' | '30d';
  networkTimeRange?: '1h' | '6h' | '24h' | '7d' | '30d';
  diskTimeRange?: '1h' | '6h' | '24h' | '7d' | '30d';
  onTimeRangeChange?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
  onCpuTimeRangeChange?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
  onCpuUsageTimeRangeChange?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
  onCpuLoadTimeRangeChange?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
  onCpuTemperatureTimeRangeChange?: (range: '1h' | '6h' | '24h' | '7d' | '30d') => void;
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
  cpuTemperatureHistoricalMetrics,
  memoryHistoricalMetrics,
  networkHistoricalMetrics,
  diskHistoricalMetrics,
  staticInfo,
  server, 
  timeRange, 
  cpuTimeRange,
  cpuUsageTimeRange,
  cpuLoadTimeRange,
  cpuTemperatureTimeRange,
  memoryTimeRange,
  networkTimeRange,
  diskTimeRange,
  onTimeRangeChange, 
  onCpuTimeRangeChange,
  onCpuUsageTimeRangeChange,
  onCpuLoadTimeRangeChange,
  onCpuTemperatureTimeRangeChange,
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
            cpuTemperatureHistoricalMetrics={cpuTemperatureHistoricalMetrics}
            staticInfo={staticInfo}
            cpuUsageTimeRange={cpuUsageTimeRange}
            cpuLoadTimeRange={cpuLoadTimeRange}
            cpuTemperatureTimeRange={cpuTemperatureTimeRange}
            onCpuUsageTimeRangeChange={onCpuUsageTimeRangeChange}
            onCpuLoadTimeRangeChange={onCpuLoadTimeRangeChange}
            onCpuTemperatureTimeRangeChange={onCpuTemperatureTimeRangeChange}
          />
        </TabPanel>
        
        <TabPanel value="memory" activeTab={activeTab}>
          <MemoryTab 
            dashboardMetrics={dashboardMetrics}
            historicalMetrics={memoryHistoricalMetrics}
            staticInfo={staticInfo}
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
