import ReactECharts from 'echarts-for-react';
import type { MoisturePoint, WateringEventDto } from '../api/client';

interface Props {
  points: MoisturePoint[];
  wateringEvents?: WateringEventDto[];
  title?: string;
}

export function MoistureChart({ points, wateringEvents = [], title }: Props) {
  if (points.length === 0) {
    return <p style={{ color: '#9ca3af', textAlign: 'center' }}>Brak danych do wykresu</p>;
  }

  const sensors = [...new Set(points.map((p) => p.sensorIdentifier))];

  const series = sensors.map((name) => ({
    name,
    type: 'line' as const,
    smooth: true,
    symbol: 'none',
    data: points
      .filter((p) => p.sensorIdentifier === name && p.soilMoisture !== null)
      .map((p) => [p.utcTime, p.soilMoisture]),
  }));

  if (wateringEvents.length > 0) {
    series.push({
      name: 'Podlanie',
      type: 'line' as const,
      smooth: false,
      symbol: 'diamond',
      data: wateringEvents.map((e) => [e.detectedAtUtc, e.moistureAfter]),
    });
  }

  const option = {
    title: title ? { text: title, left: 'center', textStyle: { fontSize: 14 } } : undefined,
    tooltip: { trigger: 'axis' as const },
    legend: { bottom: 0, textStyle: { fontSize: 11 } },
    xAxis: { type: 'time' as const },
    yAxis: { type: 'value' as const, name: 'Wilgotność (%)' },
    grid: { left: 50, right: 16, top: title ? 40 : 16, bottom: 40 },
    series,
  };

  return <ReactECharts option={option} style={{ height: 280 }} />;
}
