import { useMemo } from 'react';
import ReactECharts from 'echarts-for-react';
import type { MoisturePoint } from '../api/client';
import { moisturePointSeriesKey, resolveSeriesLegendName, sortPointsByTime, uniqueSeriesKeys } from '../utils/chartSeries';
import { echartsAxisTooltipPl, echartsTimeXAxisPl, inferRangeMs, utcIsoToMs } from '../utils/chartTimePl';

export function BatteryChart({
  points,
  sensorLegendById,
}: {
  points: MoisturePoint[];
  sensorLegendById?: Record<string, string>;
}) {
  if (points.length === 0) return null;

  const seriesKeys = uniqueSeriesKeys(points);

  const series = useMemo(() => seriesKeys.map((key) => {
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
  }), [seriesKeys, points, sensorLegendById]);
  const rangeMs = inferRangeMs(points.map((p) => utcIsoToMs(p.utcTime)).filter((n) => !Number.isNaN(n)));

  const option = {
    title: { text: 'Bateria', left: 'center', textStyle: { fontSize: 14 } },
    tooltip: echartsAxisTooltipPl(),
    legend: { bottom: 0, textStyle: { fontSize: 11 } },
    xAxis: echartsTimeXAxisPl(rangeMs),
    yAxis: { type: 'value' as const, name: '%', min: 0, max: 100 },
    grid: { left: 50, right: 16, top: 40, bottom: rangeMs && rangeMs > 48 * 3600_000 ? 56 : 40 },
    animation: !(rangeMs && rangeMs > 24 * 3600_000),
    series,
  };

  return <ReactECharts option={option} style={{ height: 200 }} />;
}
