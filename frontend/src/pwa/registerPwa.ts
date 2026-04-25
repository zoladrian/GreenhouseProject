/**
 * Rejestruje service worker generowany przez vite-plugin-pwa.
 *
 * Zachowanie:
 *  - W trybie PROD: rejestracja przez `virtual:pwa-register` (auto-update).
 *  - W DEV i w testach: NO-OP (brak SW), żeby nie zaśmiecać hot-reloadu.
 *  - Wszelkie błędy rejestracji są wyciszone — brak SW NIE może zablokować startu UI
 *    (offline jest "nice-to-have", a nie warunek koniecznym).
 */
export function registerPwa(): void {
  if (!import.meta.env.PROD) return;
  if (typeof window === 'undefined') return;
  if (!('serviceWorker' in navigator)) return;

  void import('virtual:pwa-register')
    .then(({ registerSW }) => {
      registerSW({ immediate: true });
    })
    .catch(() => {
      /* brak SW nie blokuje aplikacji */
    });
}
