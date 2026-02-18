# Fix Chart Tooltip Runtime Errors

Fix "Cannot read properties of undefined (reading 'value')" errors in chart tooltips.

## Changes
- Add null checks for payload elements in CustomTooltip
- Use optional chaining (?.) for safe property access
- Add conditional rendering for Max/Min values in MetricsLineChart
- Fix MetricsAreaChart tooltip with proper null checks
- Provide fallback values ('0') when data is missing

## Files Changed
- components/charts/MetricsLineChart.tsx
- components/charts/MetricsAreaChart.tsx

## Result
- Charts no longer crash when hovering over incomplete data
- Tooltips gracefully handle missing Max/Min values
- Safe fallback to '0' when values are undefined