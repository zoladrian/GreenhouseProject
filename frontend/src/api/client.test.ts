import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { api, ApiError } from './client';

const originalFetch = globalThis.fetch;

function mockFetch(impl: (input: RequestInfo | URL, init?: RequestInit) => Promise<Response>) {
  globalThis.fetch = vi.fn(impl) as unknown as typeof fetch;
}

beforeEach(() => {
  globalThis.fetch = vi.fn() as unknown as typeof fetch;
});

afterEach(() => {
  globalThis.fetch = originalFetch;
});

describe('fetchJson error propagation', () => {
  it('rzuca ApiError z status i ciałem JSON {error}', async () => {
    mockFetch(async () => new Response(JSON.stringify({ error: 'Nawa nie istnieje.' }), { status: 404 }));
    const err = await api.getNawaDetail('abc').catch((e: unknown) => e);
    expect(err).toBeInstanceOf(ApiError);
    const ae = err as ApiError;
    expect(ae.status).toBe(404);
    expect(ae.message).toContain('HTTP 404');
    expect(ae.message).toContain('Nawa nie istnieje.');
  });

  it('rzuca ApiError ze snippetem tekstowym dla nie-JSON', async () => {
    mockFetch(async () => new Response('Internal failure: payload too big', { status: 500 }));
    const err = await api.getDashboard().catch((e: unknown) => e);
    expect(err).toBeInstanceOf(ApiError);
    expect((err as ApiError).status).toBe(500);
    expect((err as ApiError).message).toContain('Internal failure: payload too big');
  });

  it('przekazuje signal do fetch', async () => {
    const seenSignals: (AbortSignal | undefined)[] = [];
    mockFetch(async (_input, init) => {
      seenSignals.push(init?.signal ?? undefined);
      return new Response(JSON.stringify([]), { status: 200 });
    });
    const ctrl = new AbortController();
    await api.getDashboard(ctrl.signal);
    expect(seenSignals[0]).toBe(ctrl.signal);
  });

  it('happy path zwraca JSON', async () => {
    mockFetch(async () => new Response(JSON.stringify([{ id: '1', name: 'A' }]), { status: 200 }));
    const data = await api.getNawy();
    expect(data).toEqual([{ id: '1', name: 'A' }]);
  });

  it('assignSensor zwraca message z body przy 409', async () => {
    mockFetch(async () => new Response(JSON.stringify({ error: 'Konflikt: czujnik już przypisany' }), { status: 409 }));
    const err = await api.assignSensor('s1', 'n1').catch((e: unknown) => e);
    expect(err).toBeInstanceOf(ApiError);
    expect((err as ApiError).message).toContain('Konflikt: czujnik już przypisany');
    expect((err as ApiError).status).toBe(409);
  });
});
