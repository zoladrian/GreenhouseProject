import { expect, test } from '@playwright/test';

/**
 * E2E:
 *  - voice: GET /api/voice/daily-report zwraca poprawny szkielet (nawet bez nawy/danych).
 *  - sensor assign: PUT /api/sensor/<obcy>/nawa zwraca 404 z JSON-em (nie traktujemy jako 500).
 *  - 30d preset: tworzymy nawę przez API, otwieramy /nawy/<id>, klikamy "30 dni"
 *    i sprawdzamy że zapytania o serie używają zakresu ~720 godzin (±5 min na drift),
 *    bez padów i bez błędów wykresu.
 *
 * Wszystko biegnie przeciwko realnemu API (Production env) z czystym SQLite-em z global-setup.
 */

const TWO_HOURS_MS = 2 * 60 * 60 * 1000;
const THIRTY_DAYS_MS = 30 * 24 * 60 * 60 * 1000;
const FIVE_MIN_MS = 5 * 60 * 1000;

test.describe('Nawy (voice + assign + 30d preset)', () => {
  test('GET /api/voice/daily-report odpowiada poprawnym szkieletem', async ({ request }) => {
    const res = await request.get('/api/voice/daily-report');
    expect(res.ok()).toBeTruthy();
    const body = (await res.json()) as {
      greetingLeadin?: string;
      localTime?: string;
      localDateLong?: string;
      nawy?: unknown[];
    };
    expect(typeof body.greetingLeadin).toBe('string');
    expect(typeof body.localTime).toBe('string');
    expect(typeof body.localDateLong).toBe('string');
    expect(Array.isArray(body.nawy)).toBe(true);
  });

  test('PUT /api/sensor/<nieistniejący>/nawa zwraca 404 JSON, nie 500', async ({ request }) => {
    const res = await request.put('/api/sensor/sensor-does-not-exist-zzz/nawa', {
      data: { nawaId: null },
      headers: { 'Content-Type': 'application/json' },
    });
    expect(res.status()).toBe(404);
    // Body MUSI być JSON-em (frontend buduje komunikat z `error`/`title`/`detail`/`message`).
    const text = await res.text();
    expect(text.length).toBeGreaterThan(0);
    expect(() => JSON.parse(text)).not.toThrow();
  });

  test('preset "30 dni" wywołuje serie z from=teraz-720h, UI nie wybucha', async ({ page, request }) => {
    // Tworzymy nawę przez API, żeby mieć trwały ID i nie zależeć od stanu UI.
    const createRes = await request.post('/api/nawa', {
      data: { name: `E2E Nawa ${Date.now()}` },
      headers: { 'Content-Type': 'application/json' },
    });
    expect(createRes.ok()).toBeTruthy();
    const created = (await createRes.json()) as { id: string };
    expect(created.id).toBeTruthy();

    // Łapiemy WSZYSTKIE wywołania chart/* żeby później zweryfikować zakres.
    const chartCalls: { url: string; from: number; to: number }[] = [];
    page.on('request', (req) => {
      const u = req.url();
      if (!u.includes('/api/chart/')) return;
      const parsed = new URL(u);
      const from = parsed.searchParams.get('from');
      const to = parsed.searchParams.get('to');
      if (!from || !to) return;
      chartCalls.push({ url: u, from: Date.parse(from), to: Date.parse(to) });
    });

    await page.goto(`/nawy/${created.id}`, { waitUntil: 'domcontentloaded' });
    // Sekcja zakresu czasu z guzikami presetów.
    await expect(page.getByRole('heading', { name: /Zakres czasu wykresów/i })).toBeVisible({ timeout: 10_000 });

    // Pierwsze ładowanie: defaultem 24h. Wywołania powinny dotrzeć — czekamy aż się pojawią.
    await expect.poll(() => chartCalls.length, { timeout: 10_000 }).toBeGreaterThan(0);
    const initialCallCount = chartCalls.length;

    // Klikamy "30 dni".
    await page.getByRole('button', { name: '30 dni', exact: true }).click();

    // Po kliknięciu UI uruchomi nowe fetche — czekamy aż przyjdą.
    await expect.poll(() => chartCalls.length, { timeout: 10_000 }).toBeGreaterThan(initialCallCount);

    // Bierzemy wywołania zarejestrowane PO kliknięciu i sprawdzamy że spread to ~720h (±5 min).
    const after = chartCalls.slice(initialCallCount);
    expect(after.length).toBeGreaterThan(0);
    const nowMs = Date.now();
    for (const call of after) {
      const span = call.to - call.from;
      expect(Math.abs(span - THIRTY_DAYS_MS)).toBeLessThan(FIVE_MIN_MS);
      // 'to' powinno być blisko teraz (±2h żeby pokryć drift CI).
      expect(Math.abs(call.to - nowMs)).toBeLessThan(TWO_HOURS_MS);
    }

    // Nie pojawia się alert "wybuchu" wykresów (przykładowy guard).
    const errorAlert = page.locator('text=/Część danych nie wczytała się/i');
    // Może się pojawić jeśli serwer akurat zwróci błąd — w czystej bazie nie powinien dla pustych serii.
    expect(await errorAlert.count()).toBe(0);
  });
});
