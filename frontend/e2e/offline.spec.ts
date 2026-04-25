import { expect, test } from '@playwright/test';

/**
 * Test E2E PWA offline:
 *  1. Pierwsza wizyta = SW się rejestruje, precache zapisuje shell (HTML/CSS/JS).
 *  2. Wymuszamy `context.setOffline(true)` (rzeczywiste odcięcie sieci na poziomie Chromium).
 *  3. Reload — strona MUSI się załadować z precache (offline shell).
 *  4. Banner łączności pokazuje "Brak sieci…" (z ConnectivityBanner + useOnline).
 *  5. Żądanie do /api/* MUSI zfailować (NetworkOnly) — nie może być stale data.
 *
 * Ten test wymaga buildu PROD (vite-plugin-pwa wyłącza SW w dev). Global-setup robi `npm run build`.
 */

const SW_READY_TIMEOUT = 15_000;

async function waitForSwActive(page: import('@playwright/test').Page) {
  await page.waitForFunction(
    async () => {
      if (!('serviceWorker' in navigator)) return false;
      const reg = await navigator.serviceWorker.getRegistration();
      if (!reg) return false;
      // active = po pierwszej instalacji może być w "installing" / "waiting"
      if (reg.active) return true;
      if (reg.installing) {
        await new Promise<void>((res) => {
          reg.installing?.addEventListener('statechange', function handler() {
            if (this.state === 'activated') {
              this.removeEventListener('statechange', handler);
              res();
            }
          });
        });
        return true;
      }
      return false;
    },
    null,
    { timeout: SW_READY_TIMEOUT },
  );
}

test.describe('PWA offline', () => {
  test('shell ładuje się offline po pierwszej wizycie i banner sieci jest widoczny', async ({
    context,
    page,
  }) => {
    await page.goto('/', { waitUntil: 'load' });
    await expect(page).toHaveTitle(/Kwiaty Polskie/);
    await waitForSwActive(page);

    // Daj precache chwilę na zapisanie wszystkich plików (workbox install hook).
    await page.waitForTimeout(1500);

    await context.setOffline(true);

    // Reload — z precache.
    await page.reload({ waitUntil: 'domcontentloaded' });
    await expect(page).toHaveTitle(/Kwiaty Polskie/);

    // Banner łączności (ConnectivityBanner reaguje na navigator.onLine).
    const banner = page.getByTestId('connectivity-banner');
    await expect(banner).toBeVisible({ timeout: 5_000 });
    await expect(banner).toContainText(/Brak sieci/i);

    await context.setOffline(false);
  });

  test('/api/* MUSI failować gdy sieć jest "padnięta" (NetworkOnly, brak stale data)', async ({ page }) => {
    // UWAGA: `context.setOffline(true)` w Chromium NIE odcina sieci po stronie Service Workera
    // (znany ograniczenie Playwright/Chromium). Dlatego symulujemy padłą sieć przez `page.route`
    // — abortujemy KAŻDE żądanie do /api/*. Workbox NetworkOnly wtedy MUSI rzucić błąd, bo nie
    // ma fallbacku do cache'a (po to wybraliśmy tę strategię — żeby nie pokazywać stale danych).
    await page.route('**/api/**', (route) => route.abort('failed'));
    await page.goto('/', { waitUntil: 'load' });
    await expect(page).toHaveTitle(/Kwiaty Polskie/);
    await waitForSwActive(page);

    const apiResp = await page.evaluate(async () => {
      try {
        const r = await fetch('/api/dashboard', { cache: 'no-store' });
        return r.ok ? ('ok' as const) : (`status-${r.status}` as const);
      } catch {
        return 'failed' as const;
      }
    });
    expect(apiResp).toBe('failed');
  });
});
