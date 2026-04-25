import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { act, fireEvent, render, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { SensorsPage } from './SensorsPage';
import type { NawaDto, SensorHealthDto } from '../api/client';

function makeHealth(overrides: Partial<SensorHealthDto> = {}): SensorHealthDto {
  return {
    sensorId: 's-1',
    externalId: 'salon_glebowy',
    displayName: 'Salon (gleba)',
    kind: 'Soil',
    nawaId: null,
    battery: 92,
    linkQuality: 220,
    cleaningReminder: null,
    rain: null,
    rainIntensityRaw: null,
    illuminanceRaw: null,
    lastReadingUtc: '2026-04-25T08:30:00Z',
    totalReadings24h: 144,
    ...overrides,
  };
}

function makeNawa(id: string, name: string, isActive = true): NawaDto {
  return {
    id,
    name,
    description: null,
    plantNote: null,
    isActive,
    moistureMin: 30,
    moistureMax: 70,
    temperatureMin: null,
    temperatureMax: null,
    createdAtUtc: '2026-04-01T00:00:00Z',
  };
}

interface FetchScenario {
  health: SensorHealthDto[][];
  nawy: NawaDto[][];
  assignResponses?: (Response | (() => Response))[];
}

function installFetchMock(scenario: FetchScenario) {
  let healthIdx = 0;
  let nawyIdx = 0;
  let assignIdx = 0;
  const assignCalls: { url: string; body: unknown }[] = [];

  globalThis.fetch = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = typeof input === 'string' ? input : input.toString();
    if (url.includes('/api/sensor/health')) {
      const data = scenario.health[Math.min(healthIdx, scenario.health.length - 1)];
      healthIdx += 1;
      return new Response(JSON.stringify(data), { status: 200 });
    }
    if (url.includes('/api/nawa') && (!init || init.method == null)) {
      const data = scenario.nawy[Math.min(nawyIdx, scenario.nawy.length - 1)];
      nawyIdx += 1;
      return new Response(JSON.stringify(data), { status: 200 });
    }
    if (url.match(/\/api\/sensor\/.*\/nawa$/) && init?.method === 'PUT') {
      assignCalls.push({ url, body: init.body ? JSON.parse(String(init.body)) : null });
      const next = scenario.assignResponses?.[Math.min(assignIdx, (scenario.assignResponses?.length ?? 1) - 1)];
      assignIdx += 1;
      if (typeof next === 'function') return next();
      if (next) return next;
      return new Response(null, { status: 204 });
    }
    return new Response('null', { status: 200 });
  }) as unknown as typeof fetch;

  return { assignCalls: () => assignCalls };
}

beforeEach(() => {
  vi.spyOn(window, 'confirm').mockReturnValue(true);
});

afterEach(() => {
  vi.restoreAllMocks();
});

describe('SensorsPage', () => {
  it('renderuje listę czujników i zezwala na przypisanie do nawy', async () => {
    const nawa = makeNawa('n-1', 'Salon');
    const sensorBefore = makeHealth();
    const sensorAfter = makeHealth({ nawaId: nawa.id });
    const { assignCalls } = installFetchMock({
      health: [[sensorBefore], [sensorAfter]],
      nawy: [[nawa]],
    });

    const { container, getByText } = render(
      <MemoryRouter>
        <SensorsPage />
      </MemoryRouter>,
    );

    await waitFor(() => {
      expect(getByText('Salon (gleba)')).toBeTruthy();
    });

    const select = container.querySelector('select');
    expect(select).toBeTruthy();
    await act(async () => {
      fireEvent.change(select!, { target: { value: 'n-1' } });
    });

    await waitFor(() => {
      expect(assignCalls()).toHaveLength(1);
      expect(assignCalls()[0].body).toEqual({ nawaId: 'n-1' });
    });
  });

  it('pokazuje błąd ApiError jako alert role gdy serwer zwróci 400', async () => {
    const nawa = makeNawa('n-1', 'Pomidory');
    const sensor = makeHealth();
    installFetchMock({
      health: [[sensor]],
      nawy: [[nawa]],
      assignResponses: [
        new Response(JSON.stringify({ error: 'Czujnik pogodowy jest globalny.' }), {
          status: 400,
          headers: { 'Content-Type': 'application/json' },
        }),
      ],
    });

    const { container, findByRole } = render(
      <MemoryRouter>
        <SensorsPage />
      </MemoryRouter>,
    );

    await waitFor(() => {
      expect(container.querySelector('select')).toBeTruthy();
    });

    const select = container.querySelector('select');
    await act(async () => {
      fireEvent.change(select!, { target: { value: 'n-1' } });
    });

    const alert = await findByRole('alert');
    expect(alert.textContent).toMatch(/globalny/);
  });

  it('NIE pokazuje selekt-przypisania dla czujnika Weather (jest globalny)', async () => {
    const sensor = makeHealth({
      sensorId: 'weather-1',
      kind: 'Weather',
      externalId: 'rain_outdoor',
      displayName: 'Deszczomierz',
      rain: false,
      illuminanceRaw: 320,
    });
    installFetchMock({
      health: [[sensor]],
      nawy: [[]],
    });

    const { container, findByText } = render(
      <MemoryRouter>
        <SensorsPage />
      </MemoryRouter>,
    );

    await findByText('Deszczomierz');
    expect(container.querySelectorAll('select').length).toBe(0);
    expect(container.textContent).toMatch(/Czujnik globalny/);
  });

  it('przycisk "Usuń z listy aplikacji" wywołuje DELETE /api/sensor/{id}', async () => {
    const sensor = makeHealth();
    let deleteCalled = false;

    globalThis.fetch = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = typeof input === 'string' ? input : input.toString();
      if (url.includes('/api/sensor/health')) {
        return new Response(JSON.stringify(deleteCalled ? [] : [sensor]), { status: 200 });
      }
      if (url.includes('/api/nawa') && (!init || init.method == null)) {
        return new Response(JSON.stringify([]), { status: 200 });
      }
      if (url === '/api/sensor/s-1' && init?.method === 'DELETE') {
        deleteCalled = true;
        return new Response(null, { status: 204 });
      }
      return new Response('null', { status: 200 });
    }) as unknown as typeof fetch;

    const { container, findByText, queryByText } = render(
      <MemoryRouter>
        <SensorsPage />
      </MemoryRouter>,
    );

    await findByText('Salon (gleba)');
    const btn = Array.from(container.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Usuń z listy'),
    );
    expect(btn).toBeTruthy();

    await act(async () => {
      btn!.click();
    });

    await waitFor(() => {
      expect(deleteCalled).toBe(true);
      expect(queryByText('Salon (gleba)')).toBeNull();
    });
  });
});
