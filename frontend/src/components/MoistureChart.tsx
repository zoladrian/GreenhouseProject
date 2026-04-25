import { useMemo } from 'react';
import ReactECharts from 'echarts-for-react';
import type { MoisturePoint, WateringEventDto, WateringInferredKind } from '../api/client';
import { moisturePointSeriesKey, resolveSeriesLegendName, sortPointsByTime, uniqueSeriesKeys } from '../utils/chartSeries';
import { chartGridBottomPl, echartsAxisTooltipPl, echartsTimeXAxisPl, inferRangeMs, utcIsoToMs } from '../utils/chartTimePl';

interface Props {
  points: MoisturePoint[];
  /** Mapa sensorId → etykieta (np. displayName z nawy); scala serie po zmianie nazwy w Z2M. */
  sensorLegendById?: Record<string, string>;
  /** Opcjonalny zakres osi czasu (ms) wyliczony z filtra widoku; nadpisuje inferencję z punktów. */
  rangeMs?: number | null;
  /** Twarde granice osi czasu z filtra (od-do). */
  timeBounds?: { minMs: number; maxMs: number } | null;
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

export function MoistureChart({
  points,
  sensorLegendById,
  rangeMs: rangeMsOverride,
  timeBounds,
  wateringEvents = [],
  title,
  moistureMin,
  moistureMax,
}: Props) {
  // Wszystkie hooki MUSZĄ być wywołane przed jakimkolwiek warunkowym `return` —
  // inaczej React naliczy różne liczby hooków między renderami i wybuchnie (#310).
  const seriesKeys = useMemo(() => uniqueSeriesKeys(points), [points]);

  const thresholdLines = useMemo(() => {
    if (moistureMin == null && moistureMax == null) return [];
    const lines: Array<Record<string, unknown>> = [];
    if (moistureMin != null) {
      lines.push({
        yAxis: moistureMin,
        lineStyle: { color: '#d97706', type: 'dashed' as const, width: 2 },
        label: { formatter: `Podlej (≤ ${moistureMin}%)`, position: 'insideEndTop' as const },
      });
    }
    if (moistureMax != null) {
      lines.push({
        yAxis: moistureMax,
        lineStyle: { color: '#2563eb', type: 'dashed' as const, width: 2 },
        label: { formatter: `Za mokro (≥ ${moistureMax}%)`, position: 'insideEndBottom' as const },
      });
    }
    return lines;
  }, [moistureMin, moistureMax]);

  const wateringVertLines = useMemo(() => {
    if (wateringEvents.length === 0) return [];
    return wateringEvents.map((e) => {
      const v = kindVisual(e.inferredKind);
      return {
        xAxis: utcIsoToMs(e.detectedAtUtc),
        name: v.shortLabel,
        lineStyle: { color: v.color, type: 'dashed' as const, width: 2 },
        label: {
          formatter: `${v.shortLabel} +${e.deltaMoisture}%`,
          color: v.color,
          fontSize: 10,
        },
      };
    });
  }, [wateringEvents]);

  const series = useMemo(
    () =>
      seriesKeys.map((key, idx) => {
        const forSeries = sortPointsByTime(
          points.filter((p) => moisturePointSeriesKey(p) === key && p.soilMoisture !== null),
        );
        const legendName = resolveSeriesLegendName(key, forSeries, sensorLegendById);
        return {
          name: legendName,
          type: 'line' as const,
          smooth: true,
          symbol: 'none',
          data: forSeries.map((p) => [utcIsoToMs(p.utcTime), p.soilMoisture]),
          markLine:
            idx === 0 && (thresholdLines.length > 0 || wateringVertLines.length > 0)
              ? {
                  symbol: 'none',
                  data: [...thresholdLines, ...wateringVertLines],
                  silent: false,
                }
              : undefined,
          sampling: 'lttb' as const,
          progressive: 1000,
          progressiveThreshold: 3000,
        };
      }),
    [seriesKeys, points, thresholdLines, wateringVertLines, sensorLegendById],
  );

  const rangeMs = useMemo(() => {
    if (rangeMsOverride != null && rangeMsOverride >= 0) return rangeMsOverride;
    return inferRangeMs(points.map((p) => utcIsoToMs(p.utcTime)).filter((n) => !Number.isNaN(n)));
  }, [points, rangeMsOverride]);

  const option = useMemo(
    () => ({
      title: title ? { text: title, left: 'center', textStyle: { fontSize: 14 } } : undefined,
      tooltip: echartsAxisTooltipPl(),
      legend: { bottom: 0, textStyle: { fontSize: 11 } },
      xAxis: echartsTimeXAxisPl(rangeMs, timeBounds),
      yAxis: { type: 'value' as const, name: 'Wilgotność (%)' },
      grid: { left: 50, right: 16, top: title ? 40 : 16, bottom: chartGridBottomPl(rangeMs) },
      animation: !(rangeMs && rangeMs > 24 * 3600_000),
      series,
    }),
    [title, rangeMs, series, timeBounds],
  );

  if (points.length === 0) {
    return <p style={{ color: '#9ca3af', textAlign: 'center' }}>Brak danych do wykresu</p>;
  }

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
