# Fix TypeScript Types for TimeRange

Fix TypeScript errors where `string` type was assigned to specific union type for timeRange props.

## Changes
- Update TimeRangeSelectorProps to use proper union type
- Fix timeRange types in all tab components (CpuTab, MemoryTab, NetworkTab, StorageTab)
- Update MetricsTabsProps with correct timeRange types
- Add default values for optional timeRange props

## Files Changed
- TimeRangeSelector.tsx
- CpuTab.tsx  
- MemoryTab.tsx
- NetworkTab.tsx
- StorageTab.tsx
- MetricsTabs.tsx