import { useEffect, useState } from 'react';
import { fetchDeployMeta } from '../deployWatch';

/**
 * Cienki pasek z identyfikatorem wdrożenia (z API). Przydatny na ekranie Malinki / diagnostyka.
 */
export function DeployVersionBar() {
  const [deployId, setDeployId] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    void (async () => {
      const meta = await fetchDeployMeta();
      if (!cancelled && meta?.deployId) setDeployId(meta.deployId);
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  if (!deployId) return null;

  return (
    <div className="app-deploy-bar" title="Identyfikator wdrożenia serwera; po aktualizacji Dockera zmieni się i strona odświeży się automatycznie.">
      <span className="app-deploy-bar__label">Wersja serwera</span>
      <code className="app-deploy-bar__id">{deployId}</code>
    </div>
  );
}
