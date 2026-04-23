import { describe, expect, it } from 'vitest';
import { moisturePointSeriesKey, resolveSeriesLegendName, uniqueSeriesKeys } from './chartSeries';

describe('chartSeries', () => {
  it('uses sensorId as stable series key', () => {
    const key = moisturePointSeriesKey({
      utcTime: '2026-04-23T10:00:00Z',
      sensorIdentifier: 'topic-name',
      sensorId: 'abc',
      soilMoisture: 20,
      temperature: 21,
      battery: 95,
      linkQuality: 120,
    });
    expect(key).toBe('abc');
  });

  it('falls back to topic key when sensorId is null', () => {
    const key = moisturePointSeriesKey({
      utcTime: '2026-04-23T10:00:00Z',
      sensorIdentifier: 'topic-name',
      sensorId: null,
      soilMoisture: 20,
      temperature: 21,
      battery: 95,
      linkQuality: 120,
    });
    expect(key).toBe('topic:topic-name');
  });

  it('resolves legend from map when sensor id exists', () => {
    const name = resolveSeriesLegendName('sensor-1', [{ sensorIdentifier: 'fallback' }], { 'sensor-1': 'Monstera' });
    expect(name).toBe('Monstera');
  });

  it('uniqueSeriesKeys should deduplicate', () => {
    const points = [
      { utcTime: '', sensorIdentifier: 'a', sensorId: '1', soilMoisture: 1, temperature: null, battery: null, linkQuality: null },
      { utcTime: '', sensorIdentifier: 'a', sensorId: '1', soilMoisture: 2, temperature: null, battery: null, linkQuality: null },
      { utcTime: '', sensorIdentifier: 'b', sensorId: null, soilMoisture: 3, temperature: null, battery: null, linkQuality: null },
    ];
    expect(uniqueSeriesKeys(points)).toEqual(['1', 'topic:b']);
  });
});
