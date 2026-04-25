import { useMemo } from 'react';
import ReactECharts from 'echarts-for-react';
import type { WeatherPoint } from '../api/client';
import { resolveSeriesLegendName } from '../utils/chartSeries';
import { chartGridBottomPl, echartsAxisTooltipPl, echartsTimeXAxisPl, inferRangeMs, utcIsoToMs } from '../utils/chartTimePl';

export type WeatherMetricKey =
  | 'rain'
  | 'rainIntensityRaw'
  | 'illuminanceRaw'
  | 'illuminanceAverage20MinRaw'
  | 'illuminanceMaximumTodayRaw'
  | 'battery';

const metricLabel: Record<WeatherMetricKey, string> = {
  rain: 'Wykrycie opadu',
  rainIntensityRaw: 'Intensywność opadu (surowa)',
  illuminanceRaw: 'Jasność surowa',
  illuminanceAverage20MinRaw: 'Jasność średnia 20 min',
  illuminanceMaximumTodayRaw: 'Maks. jasność dziś',
  battery: 'Bateria',
};

export function WeatherChart({
  points,
  selectedMetrics,
  sensorLegendById,
}: {
  points: WeatherPoint[];
  selectedMetrics: WeatherMetricKey[];
  sensorLegendById?: Record<string, string>;
}) {
  // Hooki przed wczesnym returnem — kolejność wywołań musi być stała.
  const keys = useMemo(
    () => [...new Set(points.map((p) => (p.sensorId ? p.sensorId : `topic:${p.sensorIdentifier}`)))],
    [points],
  );

  const series: object[] = useMemo(() => {
    const s: object[] = [];
    for (const key of keys) {
      const sensorPoints = [...points]
        .filter((p) => (p.sensorId ? p.sensorId : `topic:${p.sensorIdentifier}`) === key)
        .sort((a, b) => utcIsoToMs(a.utcTime) - utcIsoToMs(b.utcTime));
      const legend = resolveSeriesLegendName(
        key,
        sensorPoints.map((p) => ({ sensorIdentifier: p.sensorIdentifier })),
        sensorLegendById,
      );

      for (const metric of selectedMetrics) {
        const values = sensorPoints
          .map((p) => {
            const y =
              metric === 'rain'
                ? p.rain == null
                  ? null
                  : p.rain
                    ? 1
                    : 0
                : p[metric];
            return y == null ? null : [utcIsoToMs(p.utcTime), y];
          })
          .filter((x): x is [number, number] => x !== null);

        if (values.length === 0) continue;

        s.push({
          name: `${legend} · ${metricLabel[metric]}`,
          type: 'line' as const,
          smooth: metric === 'rain' ? false : true,
          step: metric === 'rain' ? 'end' : false,
          symbol: 'none',
          yAxisIndex: metric === 'rain' ? 1 : 0,
          data: values,
          sampling: 'lttb' as const,
          progressive: 1000,
          progressiveThreshold: 3000,
        });
      }
    }
    return s;
  }, [keys, points, selectedMetrics, sensorLegendById]);

  const rangeMs = useMemo(
    () => inferRangeMs(points.map((p) => utcIsoToMs(p.utcTime)).filter((n) => !Number.isNaN(n))),
    [points],
  );

  const option = useMemo(
    () => ({
      title: { text: 'Pogoda (RB-SRAIN01)', left: 'center', textStyle: { fontSize: 14 } },
      tooltip: echartsAxisTooltipPl(),
      legend: { bottom: 0, textStyle: { fontSize: 11 } },
      xAxis: echartsTimeXAxisPl(rangeMs),
      yAxis: [
        { type: 'value' as const, name: 'Wartość surowa' },
        { type: 'value' as const, name: 'Opad', min: 0, max: 1, interval: 1 },
      ],
      grid: { left: 52, right: 52, top: 40, bottom: Math.max(56, chartGridBottomPl(rangeMs)) },
      animation: !(rangeMs && rangeMs > 24 * 3600_000),
      series,
    }),
    [rangeMs, series],
  );

  if (points.length === 0 || selectedMetrics.length === 0) {
    return <p style={{ color: '#9ca3af', textAlign: 'center' }}>Brak danych pogodowych do wykresu</p>;
  }

  return <ReactECharts option={option} style={{ height: 300 }} />;
}
