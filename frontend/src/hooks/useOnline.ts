import { useEffect, useState } from 'react';

/**
 * Subskrybuje `navigator.onLine` + zdarzenia `online` / `offline`.
 *
 * Uwagi praktyczne:
 *  - `navigator.onLine === true` NIE gwarantuje, że API odpowiada (np. malina padła).
 *    Dla "świeżości danych" osobny licznik jest w widoku (banner pokazuje ostatnią aktualizację).
 *  - W SSR / vitest jsdom: `navigator` istnieje, ale nie ma listenera `online`. Wartość początkowa = true.
 */
export function useOnline(): boolean {
  const [online, setOnline] = useState<boolean>(() => {
    if (typeof navigator === 'undefined') return true;
    return navigator.onLine !== false;
  });

  useEffect(() => {
    if (typeof window === 'undefined') return;

    const handleOnline = () => setOnline(true);
    const handleOffline = () => setOnline(false);

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);
    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);

  return online;
}
