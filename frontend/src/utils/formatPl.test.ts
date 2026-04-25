import { describe, expect, it } from 'vitest';
import {
  formatDatePl,
  formatDateTimeFullPl,
  formatDateTimeShortPl,
  formatIntegerPl,
  formatNumberPl,
  formatTimePl,
  keyFromTimestampAndLabel,
} from './formatPl';

describe('formatPl', () => {
  // 2026-04-25T15:32:10Z → 17:32 lokalnego (Europe/Warsaw, CEST). W CI na UTC: 15:32.
  // Test napisany "tolerancyjnie": sprawdzamy strukturę, nie konkretną godzinę.
  const sample = '2026-04-25T15:32:10Z';

  it('formatDateTimeShortPl: dd.MM HH:mm', () => {
    expect(formatDateTimeShortPl(sample)).toMatch(/^\d{2}\.\d{2},?\s+\d{2}:\d{2}$/);
  });

  it('formatDateTimeShortPl: null/empty → "—"', () => {
    expect(formatDateTimeShortPl(null)).toBe('—');
    expect(formatDateTimeShortPl(undefined)).toBe('—');
    expect(formatDateTimeShortPl('not-a-date')).toBe('—');
  });

  it('formatDateTimeFullPl: yyyy z rokiem', () => {
    expect(formatDateTimeFullPl(sample)).toMatch(/2026/);
  });

  it('formatDatePl: tylko data', () => {
    expect(formatDatePl(sample)).toMatch(/^\d{2}\.\d{2}\.\d{4}$/);
    expect(formatDatePl(null)).toBe('—');
  });

  it('formatTimePl: HH:mm', () => {
    expect(formatTimePl(sample)).toMatch(/^\d{2}:\d{2}$/);
    expect(formatTimePl(null)).toBe('—');
  });

  it('formatNumberPl: max 1 miejsce po przecinku, separator polski', () => {
    expect(formatNumberPl(1.5)).toBe('1,5');
    expect(formatNumberPl(1.55)).toBe('1,6'); // zaokr.
    expect(formatNumberPl(1)).toBe('1');
    expect(formatNumberPl(null)).toBe('—');
    expect(formatNumberPl(undefined)).toBe('—');
    expect(formatNumberPl(Number.NaN)).toBe('—');
  });

  it('formatIntegerPl: zaokrąglone, polski separator tysięcy', () => {
    // pl-PL grupuje od 5 cyfr w niektórych ICU (min2). Test używa >= 5 cyfr żeby było stabilnie.
    expect(formatIntegerPl(1234567)).toMatch(/^1[\s\u00A0\u202F]234[\s\u00A0\u202F]567$/);
    expect(formatIntegerPl(0)).toBe('0');
    expect(formatIntegerPl(null)).toBe('—');
  });

  it('keyFromTimestampAndLabel: stabilny klucz po timestampie i label', () => {
    const k1 = keyFromTimestampAndLabel(sample, 'watering');
    const k2 = keyFromTimestampAndLabel(sample, 'watering');
    const k3 = keyFromTimestampAndLabel(sample, 'drying');
    expect(k1).toBe(k2);
    expect(k1).not.toBe(k3);
    expect(k1).toContain('__watering');
  });

  it('keyFromTimestampAndLabel: null timestamp → "unknown" w prefixie', () => {
    expect(keyFromTimestampAndLabel(null, 'x')).toBe('unknown__x');
  });
});
