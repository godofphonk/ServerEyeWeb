import {
  formatTimeByRange,
  getChartDateFormat,
  getTickCountByRange,
} from '@/utils/timeFormat';

describe('getChartDateFormat', () => {
  it('returns HH:mm for 1h range', () => {
    expect(getChartDateFormat('1h')).toBe('HH:mm');
  });

  it('returns HH:mm for 6h range', () => {
    expect(getChartDateFormat('6h')).toBe('HH:mm');
  });

  it('returns HH:mm for 24h range', () => {
    expect(getChartDateFormat('24h')).toBe('HH:mm');
  });

  it('returns dd MMM HH:mm for 7d range', () => {
    expect(getChartDateFormat('7d')).toBe('dd MMM HH:mm');
  });

  it('returns dd MMM for 30d range', () => {
    expect(getChartDateFormat('30d')).toBe('dd MMM');
  });

  it('returns dd MMM HH:mm for unknown range', () => {
    expect(getChartDateFormat('unknown')).toBe('dd MMM HH:mm');
  });

  it('returns dd MMM HH:mm for empty string', () => {
    expect(getChartDateFormat('')).toBe('dd MMM HH:mm');
  });
});

describe('getTickCountByRange', () => {
  it('returns 6 for 1h range', () => {
    expect(getTickCountByRange('1h')).toBe(6);
  });

  it('returns 6 for 6h range', () => {
    expect(getTickCountByRange('6h')).toBe(6);
  });

  it('returns 8 for 24h range', () => {
    expect(getTickCountByRange('24h')).toBe(8);
  });

  it('returns 7 for 7d range', () => {
    expect(getTickCountByRange('7d')).toBe(7);
  });

  it('returns 10 for 30d range', () => {
    expect(getTickCountByRange('30d')).toBe(10);
  });

  it('returns 6 for unknown range', () => {
    expect(getTickCountByRange('unknown')).toBe(6);
  });

  it('returns 6 for empty string', () => {
    expect(getTickCountByRange('')).toBe(6);
  });
});

describe('formatTimeByRange', () => {
  const testTimestamp = '2024-06-15T14:30:00.000Z';

  it('returns a non-empty string for 1h range', () => {
    const result = formatTimeByRange(testTimestamp, '1h');
    expect(typeof result).toBe('string');
    expect(result.length).toBeGreaterThan(0);
  });

  it('returns a non-empty string for 6h range', () => {
    const result = formatTimeByRange(testTimestamp, '6h');
    expect(typeof result).toBe('string');
    expect(result.length).toBeGreaterThan(0);
  });

  it('returns a non-empty string for 24h range', () => {
    const result = formatTimeByRange(testTimestamp, '24h');
    expect(typeof result).toBe('string');
    expect(result.length).toBeGreaterThan(0);
  });

  it('returns a non-empty string for 7d range', () => {
    const result = formatTimeByRange(testTimestamp, '7d');
    expect(typeof result).toBe('string');
    expect(result.length).toBeGreaterThan(0);
  });

  it('returns a non-empty string for 30d range', () => {
    const result = formatTimeByRange(testTimestamp, '30d');
    expect(typeof result).toBe('string');
    expect(result.length).toBeGreaterThan(0);
  });

  it('returns a non-empty string for unknown range using default format', () => {
    const result = formatTimeByRange(testTimestamp, 'unknown');
    expect(typeof result).toBe('string');
    expect(result.length).toBeGreaterThan(0);
  });

  it('1h, 6h, 24h ranges produce the same format (time only)', () => {
    const result1h = formatTimeByRange(testTimestamp, '1h');
    const result6h = formatTimeByRange(testTimestamp, '6h');
    const result24h = formatTimeByRange(testTimestamp, '24h');
    expect(result1h).toBe(result6h);
    expect(result6h).toBe(result24h);
  });

  it('7d range produces longer output than 30d range', () => {
    const result7d = formatTimeByRange(testTimestamp, '7d');
    const result30d = formatTimeByRange(testTimestamp, '30d');
    expect(result7d.length).toBeGreaterThanOrEqual(result30d.length);
  });
});
