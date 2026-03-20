/** Odpowiedź GET /api/meta/deploy */
export type DeployMetaResponse = { deployId: string };

const POLL_MS = 45_000;

/**
 * W produkcji: co POLL_MS sprawdza deployId z serwera; po zmianie (np. nowy obraz Docker) — pełne przeładowanie strony.
 */
export function startDeployVersionWatch(): void {
  if (!import.meta.env.PROD) return;

  let current: string | null = null;

  const tick = async () => {
    try {
      const r = await fetch('/api/meta/deploy', { cache: 'no-store' });
      if (!r.ok) return;
      const j = (await r.json()) as DeployMetaResponse;
      const id = j.deployId?.trim();
      if (!id) return;

      if (current === null) {
        current = id;
        return;
      }
      if (id !== current) {
        window.location.reload();
      }
    } catch {
      /* offline / tymczasowy błąd — następna próba */
    }
  };

  void tick();
  window.setInterval(() => void tick(), POLL_MS);
}

export async function fetchDeployMeta(): Promise<DeployMetaResponse | null> {
  try {
    const r = await fetch('/api/meta/deploy', { cache: 'no-store' });
    if (!r.ok) return null;
    return (await r.json()) as DeployMetaResponse;
  } catch {
    return null;
  }
}
