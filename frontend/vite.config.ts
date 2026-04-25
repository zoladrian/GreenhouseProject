import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      // 'autoUpdate' wymusza aktualizację SW przy każdym wejściu (idealne na malinkę bez sklepu).
      registerType: 'autoUpdate',
      // Wyłączamy SW w dev (uniknij efektu "stary build" przy hot-reload).
      devOptions: { enabled: false },
      includeAssets: [
        'favicon.svg',
        'icons.svg',
        'images/kwiaty-polskie-logo.png',
        'images/kwiaty-polskie-logo.svg',
        'images/kwiaty-polskie-greenhouse.svg',
        'images/kwiaty-polskie-hero.png',
        'images/kwiaty-polskie-petunias-banner.png',
        'images/nawy-aisle-bg.png',
        'images/nawy-beds-bg.png',
      ],
      manifest: {
        id: '/',
        name: 'Kwiaty Polskie — Szklarnia',
        short_name: 'Szklarnia',
        description:
          'Monitoring wilgotności gleby i naw w szklarni (offline / sieć lokalna).',
        lang: 'pl',
        dir: 'ltr',
        start_url: '/',
        scope: '/',
        display: 'standalone',
        display_override: ['standalone', 'minimal-ui', 'browser'],
        orientation: 'portrait-primary',
        background_color: '#f4faf5',
        theme_color: '#2d6a32',
        categories: ['utilities', 'lifestyle'],
        icons: [
          {
            src: '/images/kwiaty-polskie-logo.png',
            sizes: '192x192',
            type: 'image/png',
            purpose: 'any',
          },
          {
            src: '/images/kwiaty-polskie-logo.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'any',
          },
          // Ten sam asset zadeklarowany jako "maskable" (Android adaptive icons).
          // Logo PK ma duży padding wewnętrzny (~20%), więc bezpiecznie spełnia safe-zone.
          {
            src: '/images/kwiaty-polskie-logo.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'maskable',
          },
        ],
      },
      workbox: {
        // Precache całego shella (HTML/CSS/JS/ikony) — pozwala uruchomić aplikację offline.
        globPatterns: ['**/*.{js,css,html,svg,png,ico,webp,woff2}'],
        // Pojedynczy chunk z echarts to ~1 MB; podnieś limit, żeby się dostał do precache.
        maximumFileSizeToCacheInBytes: 5 * 1024 * 1024,
        // SPA fallback — przy braku sieci offline zawsze ładuje index.html (z precache).
        navigateFallback: '/index.html',
        // Wyłącz fallback dla /api/* — endpointy mają zwracać twardy network error,
        // a UI sam wyświetla banner offline.
        navigateFallbackDenylist: [/^\/api\//],
        runtimeCaching: [
          {
            // Surowy NetworkOnly dla danych z API. NIE pokazujemy stale danych —
            // to ważne dla decyzji "podlej / nie podlej" (sygnalizuj brak świeżych danych).
            urlPattern: ({ url }) => url.pathname.startsWith('/api/'),
            handler: 'NetworkOnly',
            options: { cacheName: 'api-no-cache' },
          },
          {
            // Obrazy i fonty — cache-first (bez wpływu na świeżość danych aplikacji).
            urlPattern: ({ request }) =>
              request.destination === 'image' || request.destination === 'font',
            handler: 'CacheFirst',
            options: {
              cacheName: 'static-assets',
              expiration: { maxEntries: 80, maxAgeSeconds: 60 * 60 * 24 * 30 },
            },
          },
        ],
        // Czyść stare buildy z cache, żeby nie rosło bez końca.
        cleanupOutdatedCaches: true,
        clientsClaim: true,
        skipWaiting: true,
      },
    }),
  ],
  test: {
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    include: ['src/**/*.test.ts', 'src/**/*.test.tsx'],
  },
  server: {
    proxy: {
      '/api': 'http://localhost:5000',
    },
  },
});
