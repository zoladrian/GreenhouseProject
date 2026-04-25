import { describe, expect, it } from 'vitest';
import { chartGridBottomPl, echartsTimeXAxisPl, inferRangeMs, utcIsoToMs } from './chartTimePl';

describe('chartTimePl', () => {
  it('utcIsoToMs should treat ISO without zone as UTC', () => {
    const a = utcIsoToMs('2026-04-23T10:00:00');
    const b = utcIsoToMs('2026-04-23T10:00:00Z');
    expect(a).toBe(b);
  });

  it('inferRangeMs should return span for sorted and unsorted values', () => {
    const span = inferRangeMs([2000, 1000, 5000, 3000]);
    expect(span).toBe(4000);
  });

  it('chartGridBottomPl should reserve more space for long ranges', () => {
    expect(chartGridBottomPl(30 * 24 * 3600_000)).toBe(78);
    expect(chartGridBottomPl(25 * 3600_000)).toBe(52);
    expect(chartGridBottomPl(10 * 3600_000)).toBe(40);
  });

  it('echartsTimeXAxisPl should widen minInterval for multi-week ranges', () => {
    const thirtyDays = 30 * 24 * 3600_000;
    const axis = echartsTimeXAxisPl(thirtyDays) as { minInterval?: number; axisLabel?: { rotate?: number } };
    expect(axis.minInterval).toBe(2 * 24 * 60 * 60 * 1000);
    expect(axis.axisLabel?.rotate).toBe(40);
  });

  it('echartsTimeXAxisPl should hide hours for ranges above 48h', () => {
    const axis = echartsTimeXAxisPl(49 * 3600_000) as {
      axisLabel?: { formatter?: (v: number) => string };
    };
    const label = axis.axisLabel?.formatter?.(Date.parse('2026-04-25T11:30:00Z')) ?? '';
    expect(label).not.toContain(':');
  });
});
