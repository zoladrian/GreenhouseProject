import type { MoisturePoint } from '../api/client';
import { utcIsoToMs } from './chartTimePl';

/**
 * Zigbee2MQTT zmienia fragment tematu po zmianie „friendly name” — w bazie zostają różne
 * `sensorIdentifier` dla tego samego czujnika. Grupujemy po `sensorId` (FK), żeby na wykresie
 * była jedna linia i jedna legenda.
 */
export function moisturePointSeriesKey(p: MoisturePoint): string {
  if (p.sensorId) return p.sensorId;
  return `topic:${p.sensorIdentifier}`;
}

export function uniqueSeriesKeys(points: MoisturePoint[]): string[] {
  return [...new Set(points.map(moisturePointSeriesKey))];
}

function looksLikeRawIdentifier(value: string): boolean {
  const v = value.trim();
  // UUID v4-ish lub standardowy UUID.
  if (/^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(v)) return true;
  // Adresy/klucze Zigbee (0x...).
  if (/^0x[0-9a-f]{8,}$/i.test(v)) return true;
  return false;
}

function friendlyFallbackFromSeriesKey(seriesKey: string): string {
  const raw = seriesKey.startsWith('topic:') ? seriesKey.slice('topic:'.length) : seriesKey;
  const alnum = raw.replace(/[^a-z0-9]/gi, '');
  if (alnum.length === 0) return 'Czujnik';
  return `Czujnik ${alnum.slice(-6)}`;
}

/** Etykieta legendy: nazwa z panelu czujników albo ostatni znany identyfikator z MQTT. */
export function resolveSeriesLegendName(
  seriesKey: string,
  seriesPoints: Array<{ sensorIdentifier: string }>,
  sensorLegendById?: Record<string, string>,
): string {
  const fromDetail = sensorLegendById?.[seriesKey];
  if (fromDetail && fromDetail.trim().length > 0) return fromDetail;

  const sample = seriesPoints[0]?.sensorIdentifier?.trim() ?? '';
  if (sample.length > 0 && !looksLikeRawIdentifier(sample)) return sample;

  const fromTopicKey = seriesKey.startsWith('topic:') ? seriesKey.slice('topic:'.length).trim() : '';
  if (fromTopicKey.length > 0 && !looksLikeRawIdentifier(fromTopicKey)) return fromTopicKey;

  return friendlyFallbackFromSeriesKey(seriesKey);
}

export function sortPointsByTime(points: MoisturePoint[]): MoisturePoint[] {
  return [...points].sort((a, b) => utcIsoToMs(a.utcTime) - utcIsoToMs(b.utcTime));
}
