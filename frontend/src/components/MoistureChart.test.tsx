import { describe, expect, it, vi } from 'vitest';
import { render } from '@testing-library/react';
import type { MoisturePoint } from '../api/client';
import { MoistureChart } from './MoistureChart';

vi.mock('echarts-for-react', () => ({
  default: (props: { option: { series: unknown[] } }) => (
    <div data-testid="echarts" data-series-count={props.option.series.length} />
  ),
}));

const point = (sensorId: string, ts: string, m: number): MoisturePoint => ({
  utcTime: ts,
  sensorIdentifier: `friendly-${sensorId.slice(0, 4)}`,
  sensorId,
  soilMoisture: m,
  temperature: 21,
  battery: 90,
  linkQuality: 200,
});

describe('MoistureChart', () => {
  it('renderuje paragraf gdy brak punktów i nie wybucha hookami przy zmianie stanu na dane', () => {
    const { container, rerender, getByTestId } = render(<MoistureChart points={[]} />);
    expect(container.querySelector('p')?.textContent).toContain('Brak danych');

    rerender(
      <MoistureChart
        points={[point('s-1', '2026-04-25T10:00:00Z', 50), point('s-1', '2026-04-25T11:00:00Z', 55)]}
      />,
    );
    expect(getByTestId('echarts')).toBeTruthy();
  });

  it('liczba serii odpowiada liczbie unikalnych sensor_id', () => {
    const points: MoisturePoint[] = [
      point('s-1', '2026-04-25T10:00:00Z', 30),
      point('s-2', '2026-04-25T10:00:00Z', 40),
      point('s-3', '2026-04-25T10:00:00Z', 50),
    ];
    const { getByTestId } = render(<MoistureChart points={points} />);
    expect(getByTestId('echarts').getAttribute('data-series-count')).toBe('3');
  });
});
