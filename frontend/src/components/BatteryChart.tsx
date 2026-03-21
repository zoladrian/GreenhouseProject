import ReactECharts from 'echarts-for-react';
import type { MoisturePoint } from '../api/client';
import { moisturePointSeriesKey, resolveSeriesLegendName, sortPointsByTime, uniqueSeriesKeys } from '../utils/chartSeries';
import { echartsAxisTooltipPl, echartsTimeXAxisPl, utcIsoToMs } from '../utils/chartTimePl';

export function BatteryChart({
  points,
  sensorLegendById,
}: {
  points: MoisturePoint[];
  sensorLegendById?: Record<string, string>;
}) {
  if (points.length === 0) return null;

  const seriesKeys = uniqueSeriesKeys(points);

  const series = seriesKeys.map((key) => {
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
    };
  });

  const option = {
    title: { text: 'Bateria', left: 'center', textStyle: { fontSize: 14 } },
    tooltip: echartsAxisTooltipPl(),
    legend: { bottom: 0, textStyle: { fontSize: 11 } },
    xAxis: echartsTimeXAxisPl(),
    yAxis: { type: 'value' as const, name: '%', min: 0, max: 100 },
    grid: { left: 50, right: 16, top: 40, bottom: 40 },
    series,
  };

  return <ReactECharts option={option} style={{ height: 200 }} />;
}
