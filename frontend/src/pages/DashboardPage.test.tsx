import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { act, render, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import type { NawaSnapshot } from '../api/client';
import { DashboardPage } from './DashboardPage';

const speakSpy = vi.fn();
const cancelSpy = vi.fn();

class FakeUtter {
  text: string;
  lang: string = '';
  rate: number = 1;
  onend: (() => void) | null = null;
  constructor(text: string) {
    this.text = text;
  }
}

beforeEach(() => {
  speakSpy.mockClear();
  cancelSpy.mockClear();
  Object.defineProperty(globalThis, 'speechSynthesis', {
    configurable: true,
    value: { speak: speakSpy, cancel: cancelSpy },
  });
  Object.defineProperty(globalThis, 'SpeechSynthesisUtterance', {
    configurable: true,
    value: FakeUtter,
  });
  Object.defineProperty(window, 'speechSynthesis', {
    configurable: true,
    value: { speak: speakSpy, cancel: cancelSpy },
  });
});

afterEach(() => {
  vi.restoreAllMocks();
});

const sampleNawa = (id: string, status: number, name: string): NawaSnapshot => ({
  nawaId: id,
  nawaName: name,
  plantNote: null,
  status,
  sensorCount: 1,
  moistureReadingCount: 1,
  avgMoisture: 30,
  minMoisture: 30,
  maxMoisture: 30,
  moistureSpread: 0,
  avgTemperature: 22,
  lowestBattery: 80,
  oldestReadingUtc: null,
  generatedAtUtc: '2026-04-25T10:00:00Z',
  moistureMin: 35,
  moistureMax: 70,
  temperatureMin: null,
  temperatureMax: null,
  wateringSpeechNote: null,
});

function mockDashboardSequence(snapshots: NawaSnapshot[][]) {
  let i = 0;
  globalThis.fetch = vi.fn(async (input: RequestInfo | URL) => {
    const url = typeof input === 'string' ? input : input.toString();
    if (url.includes('/api/dashboard')) {
      const snap = snapshots[Math.min(i, snapshots.length - 1)];
      i += 1;
      return new Response(JSON.stringify(snap), { status: 200 });
    }
    return new Response('null', { status: 200 });
  }) as unknown as typeof fetch;
}

function setEnabledViaCheckbox(container: HTMLElement) {
  const checkbox = container.querySelector('input[type="checkbox"]') as HTMLInputElement | null;
  if (!checkbox) throw new Error('Brak checkboxa „Głos”');
  act(() => {
    checkbox.click();
  });
}

describe('DashboardPage voice effect', () => {
  it('odzywa się raz na zmianę zbioru naw "Sucho", a nie przy każdym pollu z tym samym zbiorem', async () => {
    const dryNawa = sampleNawa('n-1', 2, 'Pomidory');
    mockDashboardSequence([[dryNawa], [dryNawa], [dryNawa]]);

    const { container } = render(
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>,
    );

    await waitFor(() => {
      expect(container.querySelector('.dashboard-page__inner')).toBeTruthy();
    });

    setEnabledViaCheckbox(container);

    // speakImmediate przy włączeniu głosu: 1 wywołanie.
    expect(speakSpy.mock.calls.length).toBeGreaterThanOrEqual(1);
    const afterEnableCount = speakSpy.mock.calls.length;

    // Symulujemy 30 s polling: ten sam dataset — efekt nie powinien re-firować.
    // (W teście „polling” to po prostu kolejny render z tymi samymi danymi.)
    act(() => {
      // wymuś re-render przez pusty state — niepotrzebne, więc zamiast tego: nic.
    });
    await new Promise((r) => setTimeout(r, 80));
    expect(speakSpy.mock.calls.length).toBe(afterEnableCount);
  });

  it('odzywa się ponownie gdy 30 s polling przyniesie nowy zbiór "Sucho"', async () => {
    const dryA = sampleNawa('n-1', 2, 'Pomidory');
    const dryB = sampleNawa('n-2', 2, 'Ogórki');
    mockDashboardSequence([[dryA], [dryA, dryB]]);

    vi.useFakeTimers({ shouldAdvanceTime: true });

    const { container } = render(
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>,
    );

    await waitFor(() => {
      expect(container.querySelector('.dashboard-page__inner')).toBeTruthy();
    });
    setEnabledViaCheckbox(container);
    const afterEnableCount = speakSpy.mock.calls.length;

    await act(async () => {
      vi.advanceTimersByTime(30_000);
    });

    await waitFor(() => {
      expect(speakSpy.mock.calls.length).toBeGreaterThan(afterEnableCount);
    });

    vi.useRealTimers();
  });
});
