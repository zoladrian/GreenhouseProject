import ReactECharts from 'echarts-for-react';
import type { MoisturePoint, WateringEventDto, WateringInferredKind } from '../api/client';
import { moisturePointSeriesKey, resolveSeriesLegendName, sortPointsByTime, uniqueSeriesKeys } from '../utils/chartSeries';

interface Props {
  points: MoisturePoint[];
  /** Mapa sensorId → etykieta (np. displayName z nawy); scala serie po zmianie nazwy w Z2M. */
  sensorLegendById?: Record<string, string>;
  wateringEvents?: WateringEventDto[];
  title?: string;
  /** Poniżej — strefa „podlej”; na wykresie jako linia pomarańczowa. */
  moistureMin?: number | null;
  /** Powyżej — strefa „za mokro”; niebieska linia. */
  moistureMax?: number | null;
}

function kindVisual(kind: WateringInferredKind | string | undefined) {
  switch (kind) {
    case 'likelyRain':
      return { color: '#0369a1', shortLabel: 'Deszcz?' };
    case 'likelyManual':
      return { color: '#15803d', shortLabel: 'Podlanie' };
    default:
      return { color: '#b45309', shortLabel: 'Skok wilg.' };
  }
}

export function MoistureChart({ points, sensorLegendById, wateringEvents = [], title, moistureMin, moistureMax }: Props) {
  if (points.length === 0) {
    return <p style={{ color: '#9ca3af', textAlign: 'center' }}>Brak danych do wykresu</p>;
  }

  const seriesKeys = uniqueSeriesKeys(points);

  const thresholdLines =
    moistureMin != null || moistureMax != null
      ? [
          ...(moistureMin != null
            ? [
                {
                  yAxis: moistureMin,
                  lineStyle: { color: '#d97706', type: 'dashed' as const, width: 2 },
                  label: { formatter: `Podlej (≤ ${moistureMin}%)`, position: 'insideEndTop' as const },
                },
              ]
            : []),
          ...(moistureMax != null
            ? [
                {
                  yAxis: moistureMax,
                  lineStyle: { color: '#2563eb', type: 'dashed' as const, width: 2 },
                  label: { formatter: `Za mokro (≥ ${moistureMax}%)`, position: 'insideEndBottom' as const },
                },
              ]
            : []),
        ]
      : [];

  const wateringVertLines =
    wateringEvents.length > 0
      ? wateringEvents.map((e) => {
          const v = kindVisual(e.inferredKind);
          return {
            xAxis: e.detectedAtUtc,
            name: v.shortLabel,
            lineStyle: { color: v.color, type: 'dashed' as const, width: 2 },
            label: {
              formatter: `${v.shortLabel} +${e.deltaMoisture}%`,
              color: v.color,
              fontSize: 10,
            },
          };
        })
      : [];

  const series = seriesKeys.map((key, idx) => {
    const forSeries = sortPointsByTime(
      points.filter((p) => moisturePointSeriesKey(p) === key && p.soilMoisture !== null),
    );
    const legendName = resolveSeriesLegendName(key, forSeries, sensorLegendById);
    return {
      name: legendName,
      type: 'line' as const,
      smooth: true,
      symbol: 'none',
      data: forSeries.map((p) => [p.utcTime, p.soilMoisture]),
      markLine:
        idx === 0 && (thresholdLines.length > 0 || wateringVertLines.length > 0)
          ? {
              symbol: 'none',
              data: [...thresholdLines, ...wateringVertLines],
              silent: false,
            }
          : undefined,
    };
  });

  const option = {
    title: title ? { text: title, left: 'center', textStyle: { fontSize: 14 } } : undefined,
    tooltip: { trigger: 'axis' as const },
    legend: { bottom: 0, textStyle: { fontSize: 11 } },
    xAxis: { type: 'time' as const },
    yAxis: { type: 'value' as const, name: 'Wilgotność (%)' },
    grid: { left: 50, right: 16, top: title ? 40 : 16, bottom: 40 },
    series,
  };

  return (
    <div>
      <ReactECharts option={option} style={{ height: 280 }} />
      {wateringEvents.length > 0 && (
        <p style={{ fontSize: 10, color: '#64748b', marginTop: 4, textAlign: 'center' }}>
          Pionowe linie: <span style={{ color: '#15803d' }}>podlanie</span> (1 czujnik) /{' '}
          <span style={{ color: '#0369a1' }}>deszcz?</span> (≥2 czujniki w krótkim czasie) — heurystyka bez
          danych pogodowych.
        </p>
      )}
    </div>
  );
}
