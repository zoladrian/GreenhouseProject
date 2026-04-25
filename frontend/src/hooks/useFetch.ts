import { useEffect, useRef, useState, useCallback } from 'react';

/**
 * Stan pobierania używany przez ekrany. Zawsze zwraca świeże dane lub błąd
 * dla aktualnej kombinacji `deps`. Anuluje wcześniejsze, w lot, fetche i ignoruje
 * ich wyniki — odporne na szybkie przełączanie zakresu wykresu / nawy.
 */
export interface UseFetchResult<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
  refetch: () => void;
}

export interface UseFetchOptions {
  /**
   * Gdy `true`, błędy `AbortError` po anulowaniu fetcha NIE trafiają do UI.
   * Domyślnie włączone — `setError(null)` przy odmontowaniu/re-runie.
   */
  silentAbort?: boolean;
}

/**
 * Fetcher dostaje `AbortSignal`. Wewnętrzny `fetch` powinien go przekazać dalej,
 * dzięki czemu anulowanie naprawdę przerywa transmisję, a nie tylko ignoruje wynik.
 */
export type Fetcher<T> = (signal: AbortSignal) => Promise<T>;

export function useFetch<T>(
  fetcher: Fetcher<T> | (() => Promise<T>),
  deps: unknown[] = [],
  options: UseFetchOptions = {},
): UseFetchResult<T> {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const aliveRef = useRef(true);
  const controllerRef = useRef<AbortController | null>(null);

  const { silentAbort = true } = options;

  const refetch = useCallback(() => {
    controllerRef.current?.abort();
    const controller = new AbortController();
    controllerRef.current = controller;

    setLoading(true);
    setError(null);

    Promise.resolve()
      .then(() => (fetcher as Fetcher<T>)(controller.signal))
      .then((value) => {
        if (!aliveRef.current || controller.signal.aborted) return;
        setData(value);
      })
      .catch((e: unknown) => {
        if (!aliveRef.current || controller.signal.aborted) return;
        if (silentAbort && isAbortError(e)) return;
        setError(toMessage(e));
      })
      .finally(() => {
        if (!aliveRef.current || controller.signal.aborted) return;
        setLoading(false);
      });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  useEffect(() => {
    aliveRef.current = true;
    refetch();
    return () => {
      aliveRef.current = false;
      controllerRef.current?.abort();
    };
  }, [refetch]);

  return { data, loading, error, refetch };
}

function isAbortError(e: unknown): boolean {
  if (e instanceof DOMException && e.name === 'AbortError') return true;
  if (typeof e === 'object' && e !== null && (e as { name?: string }).name === 'AbortError') return true;
  return false;
}

function toMessage(e: unknown): string {
  if (e instanceof Error) return e.message || 'Nieznany błąd';
  if (typeof e === 'string') return e;
  return 'Nieznany błąd';
}
