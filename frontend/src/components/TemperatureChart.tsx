import ReactECharts from 'echarts-for-react';
import type { MoisturePoint } from '../api/client';

export function TemperatureChart({ points }: { points: MoisturePoint[] }) {
  if (points.length === 0) {
    return <p style={{ color: '#9ca3af', textAlign: 'center' }}>Brak danych temperatury</p>;
  }

  const sensors = [...new Set(points.map((p) => p.sensorIdentifier))];

  const series = sensors.map((name) => ({
    name,
    type: 'line' as const,
    smooth: true,
    symbol: 'none',
    data: points
      .filter((p) => p.sensorIdentifier === name && p.temperature !== null)
      .map((p) => [p.utcTime, p.temperature]),
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
