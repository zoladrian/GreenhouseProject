import ReactECharts from 'echarts-for-react';
import type { MoisturePoint, WateringEventDto } from '../api/client';

interface Props {
  points: MoisturePoint[];
  wateringEvents?: WateringEventDto[];
  title?: string;
  /** Poniżej — strefa „podlej”; na wykresie jako linia pomarańczowa. */
  moistureMin?: number | null;
  /** Powyżej — strefa „za mokro”; niebieska linia. */
  moistureMax?: number | null;
}

export function MoistureChart({ points, wateringEvents = [], title, moistureMin, moistureMax }: Props) {
  if (points.length === 0) {
    return <p style={{ color: '#9ca3af', textAlign: 'center' }}>Brak danych do wykresu</p>;
  }

  const sensors = [...new Set(points.map((p) => p.sensorIdentifier))];

  const thresholdLines =
    moistureMin != null || moistureMax != null
      ? [
          ...(moistureMin != null
            ? [
                {
                  yAxis: moistureMin,
                  lineStyle: { color: '#d97706', type: 'dashed' as const, width: 2 },
                  label: { formatter: `Podlej (≤ ${moistureMin}%)`, position: 'insideEndTop' as const },
                },
              ]
            : []),
          ...(moistureMax != null
            ? [
                {
                  yAxis: moistureMax,
                  lineStyle: { color: '#2563eb', type: 'dashed' as const, width: 2 },
                  label: { formatter: `Za mokro (≥ ${moistureMax}%)`, position: 'insideEndBottom' as const },
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
      .filter((p) => p.sensorIdentifier === name && p.soilMoisture !== null)
      .map((p) => [p.utcTime, p.soilMoisture]),
    markLine:
      idx === 0 && thresholdLines.length > 0
        ? { symbol: 'none', data: thresholdLines, silent: true }
        : undefined,
  }));

  if (wateringEvents.length > 0) {
    series.push({
      name: 'Podlanie',
      type: 'line' as const,
      smooth: false,
      symbol: 'diamond',
      data: wateringEvents.map((e) => [e.detectedAtUtc, e.moistureAfter]),
      markLine: undefined,
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
