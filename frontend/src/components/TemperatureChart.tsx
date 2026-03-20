import ReactECharts from 'echarts-for-react';
import type { MoisturePoint } from '../api/client';

interface Props {
  points: MoisturePoint[];
  temperatureMin?: number | null;
  temperatureMax?: number | null;
}

export function TemperatureChart({ points, temperatureMin, temperatureMax }: Props) {
  if (points.length === 0) {
    return <p style={{ color: '#9ca3af', textAlign: 'center' }}>Brak danych temperatury</p>;
  }

  const sensors = [...new Set(points.map((p) => p.sensorIdentifier))];

  const thresholdLines =
    temperatureMin != null || temperatureMax != null
      ? [
          ...(temperatureMin != null
            ? [
                {
                  yAxis: temperatureMin,
                  lineStyle: { color: '#0891b2', type: 'dashed' as const, width: 2 },
                  label: { formatter: `Min (${temperatureMin}°C)`, position: 'insideEndTop' as const },
                },
              ]
            : []),
          ...(temperatureMax != null
            ? [
                {
                  yAxis: temperatureMax,
                  lineStyle: { color: '#dc2626', type: 'dashed' as const, width: 2 },
                  label: { formatter: `Max (${temperatureMax}°C)`, position: 'insideEndBottom' as const },
                },
              ]
            : []),
        ]
      : [];

  const series = sensors.map((name, idx) => ({
    name,
    type: 'line' as const,
    smooth: true,
    symbol: 'none',
    data: points
      .filter((p) => p.sensorIdentifier === name && p.temperature !== null)
      .map((p) => [p.utcTime, p.temperature]),
    markLine:
      idx === 0 && thresholdLines.length > 0
        ? { symbol: 'none', data: thresholdLines, silent: true }
        : undefined,
  }));

  const option = {
    title: { text: 'Temperatura', left: 'center', textStyle: { fontSize: 14 } },
    tooltip: { trigger: 'axis' as const },
    legend: { bottom: 0, textStyle: { fontSize: 11 } },
    xAxis: { type: 'time' as const },
    yAxis: { type: 'value' as const, name: '°C' },
    grid: { left: 50, right: 16, top: 40, bottom: 40 },
    series,
  };

  return <ReactECharts option={option} style={{ height: 240 }} />;
}
