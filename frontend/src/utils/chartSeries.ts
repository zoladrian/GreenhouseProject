import type { MoisturePoint } from '../api/client';

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

/** Etykieta legendy: nazwa z panelu czujników albo ostatni znany identyfikator z MQTT. */
export function resolveSeriesLegendName(
  seriesKey: string,
  seriesPoints: MoisturePoint[],
  sensorLegendById?: Record<string, string>,
): string {
  if (seriesKey.startsWith('topic:')) {
    return seriesKey.slice('topic:'.length);
  }
  const fromDetail = sensorLegendById?.[seriesKey];
  if (fromDetail) return fromDetail;
  const sample = seriesPoints[0];
  return sample?.sensorIdentifier ?? seriesKey;
}

export function sortPointsByTime(points: MoisturePoint[]): MoisturePoint[] {
  return [...points].sort((a, b) => new Date(a.utcTime).getTime() - new Date(b.utcTime).getTime());
}
