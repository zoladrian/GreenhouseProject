import { expect, test } from '@playwright/test';

test.describe('Smoke (API + SPA)', () => {
  test('health live zwraca JSON ze znacznikiem czasu', async ({ request }) => {
    const res = await request.get('/health/live');
    expect(res.ok()).toBeTruthy();
    const body = (await res.json()) as { status?: string; utc?: string };
    expect(body.status).toBe('live');
    expect(body.utc).toBeTruthy();
  });

  test('strona główna zwraca shell SPA (tytuł z index.html)', async ({ page }) => {
    await page.goto('/', { waitUntil: 'domcontentloaded' });
    await expect(page).toHaveTitle(/Kwiaty Polskie/, { timeout: 60_000 });
  });
});
