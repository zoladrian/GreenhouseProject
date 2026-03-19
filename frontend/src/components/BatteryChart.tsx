import ReactECharts from 'echarts-for-react';
import type { MoisturePoint } from '../api/client';

export function BatteryChart({ points }: { points: MoisturePoint[] }) {
  if (points.length === 0) return null;

  const sensors = [...new Set(points.map((p) => p.sensorIdentifier))];

  const series = sensors.map((name) => ({
    name,
    type: 'line' as const,
    smooth: true,
    symbol: 'none',
    data: points
      .filter((p) => p.sensorIdentifier === name && p.battery !== null)
      .map((p) => [p.utcTime, p.battery]),
  }));

  const option = {
    title: { text: 'Bateria', left: 'center', textStyle: { fontSize: 14 } },
    tooltip: { trigger: 'axis' as const },
    legend: { bottom: 0, textStyle: { fontSize: 11 } },
    xAxis: { type: 'time' as const },
    yAxis: { type: 'value' as const, name: '%', min: 0, max: 100 },
    grid: { left: 50, right: 16, top: 40, bottom: 40 },
    series,
  };

  return <ReactECharts option={option} style={{ height: 200 }} />;
}
