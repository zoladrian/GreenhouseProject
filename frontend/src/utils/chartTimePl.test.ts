import { describe, expect, it } from 'vitest';
import { inferRangeMs, utcIsoToMs } from './chartTimePl';

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
});
