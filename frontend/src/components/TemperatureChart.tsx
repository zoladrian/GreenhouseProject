import { useMemo } from 'react';
import ReactECharts from 'echarts-for-react';
import type { MoisturePoint } from '../api/client';
import { moisturePointSeriesKey, resolveSeriesLegendName, sortPointsByTime, uniqueSeriesKeys } from '../utils/chartSeries';
import { chartGridBottomPl, echartsAxisTooltipPl, echartsTimeXAxisPl, inferRangeMs, utcIsoToMs } from '../utils/chartTimePl';

interface Props {
  points: MoisturePoint[];
  sensorLegendById?: Record<string, string>;
  temperatureMin?: number | null;
  temperatureMax?: number | null;
}

export function TemperatureChart({ points, sensorLegendById, temperatureMin, temperatureMax }: Props) {
  // Hooki muszą iść przed wczesnym returnem; inaczej React #310 przy zmianie liczby renderów.
  const seriesKeys = useMemo(() => uniqueSeriesKeys(points), [points]);

  const thresholdLines = useMemo(() => {
    if (temperatureMin == null && temperatureMax == null) return [];
    const lines: Array<Record<string, unknown>> = [];
    if (temperatureMin != null) {
      lines.push({
        yAxis: temperatureMin,
        lineStyle: { color: '#0891b2', type: 'dashed' as const, width: 2 },
        label: { formatter: `Min (${temperatureMin}°C)`, position: 'insideEndTop' as const },
      });
    }
    if (temperatureMax != null) {
      lines.push({
        yAxis: temperatureMax,
        lineStyle: { color: '#dc2626', type: 'dashed' as const, width: 2 },
        label: { formatter: `Max (${temperatureMax}°C)`, position: 'insideEndBottom' as const },
      });
    }
    return lines;
  }, [temperatureMin, temperatureMax]);

  const series = useMemo(
    () =>
      seriesKeys.map((key, idx) => {
        const forSeries = sortPointsByTime(
          points.filter((p) => moisturePointSeriesKey(p) === key && p.temperature !== null),
        );
        const legendName = resolveSeriesLegendName(key, forSeries, sensorLegendById);
        return {
          name: legendName,
          type: 'line' as const,
          smooth: true,
          symbol: 'none',
          data: forSeries.map((p) => [utcIsoToMs(p.utcTime), p.temperature]),
          markLine:
            idx === 0 && thresholdLines.length > 0
              ? { symbol: 'none', data: thresholdLines, silent: true }
              : undefined,
          sampling: 'lttb' as const,
          progressive: 1000,
          progressiveThreshold: 3000,
        };
      }),
    [seriesKeys, points, thresholdLines, sensorLegendById],
  );

  const rangeMs = useMemo(
    () => inferRangeMs(points.map((p) => utcIsoToMs(p.utcTime)).filter((n) => !Number.isNaN(n))),
    [points],
  );

  const option = useMemo(
    () => ({
      title: { text: 'Temperatura', left: 'center', textStyle: { fontSize: 14 } },
      tooltip: echartsAxisTooltipPl(),
      legend: { bottom: 0, textStyle: { fontSize: 11 } },
      xAxis: echartsTimeXAxisPl(rangeMs),
      yAxis: { type: 'value' as const, name: '°C' },
      grid: { left: 50, right: 16, top: 40, bottom: chartGridBottomPl(rangeMs) },
      animation: !(rangeMs && rangeMs > 24 * 3600_000),
      series,
    }),
    [rangeMs, series],
  );

  if (points.length === 0) {
    return <p style={{ color: '#9ca3af', textAlign: 'center' }}>Brak danych temperatury</p>;
  }

  return <ReactECharts option={option} style={{ height: 240 }} />;
}
