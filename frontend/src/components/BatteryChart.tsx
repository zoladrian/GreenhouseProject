import { useMemo } from 'react';
import ReactECharts from 'echarts-for-react';
import type { MoisturePoint } from '../api/client';
import { moisturePointSeriesKey, resolveSeriesLegendName, sortPointsByTime, uniqueSeriesKeys } from '../utils/chartSeries';
import { chartGridBottomPl, echartsAxisTooltipPl, echartsTimeXAxisPl, inferRangeMs, utcIsoToMs } from '../utils/chartTimePl';

export function BatteryChart({
  points,
  sensorLegendById,
  rangeMs: rangeMsOverride,
}: {
  points: MoisturePoint[];
  sensorLegendById?: Record<string, string>;
  /** Opcjonalny zakres osi czasu (ms) wyliczony z filtra widoku; nadpisuje inferencję z punktów. */
  rangeMs?: number | null;
}) {
  // Hooki przed wczesnym returnem — kolejność wywołań musi być stała.
  const seriesKeys = useMemo(() => uniqueSeriesKeys(points), [points]);

  const series = useMemo(
    () =>
      seriesKeys.map((key) => {
        const forSeries = sortPointsByTime(
          points.filter((p) => moisturePointSeriesKey(p) === key && p.battery !== null),
        );
        const legendName = resolveSeriesLegendName(key, forSeries, sensorLegendById);
        return {
          name: legendName,
          type: 'line' as const,
          smooth: true,
          symbol: 'none',
          data: forSeries.map((p) => [utcIsoToMs(p.utcTime), p.battery]),
          sampling: 'lttb' as const,
          progressive: 1000,
          progressiveThreshold: 3000,
        };
      }),
    [seriesKeys, points, sensorLegendById],
  );

  const rangeMs = useMemo(() => {
    if (rangeMsOverride != null && rangeMsOverride >= 0) return rangeMsOverride;
    return inferRangeMs(points.map((p) => utcIsoToMs(p.utcTime)).filter((n) => !Number.isNaN(n)));
  }, [points, rangeMsOverride]);

  const option = useMemo(
    () => ({
      title: { text: 'Bateria', left: 'center', textStyle: { fontSize: 14 } },
      tooltip: echartsAxisTooltipPl(),
      legend: { bottom: 0, textStyle: { fontSize: 11 } },
      xAxis: echartsTimeXAxisPl(rangeMs),
      yAxis: { type: 'value' as const, name: '%', min: 0, max: 100 },
      grid: { left: 50, right: 16, top: 40, bottom: chartGridBottomPl(rangeMs) },
      animation: !(rangeMs && rangeMs > 24 * 3600_000),
      series,
    }),
    [rangeMs, series],
  );

  if (points.length === 0) return null;

  return <ReactECharts option={option} style={{ height: 200 }} />;
}
