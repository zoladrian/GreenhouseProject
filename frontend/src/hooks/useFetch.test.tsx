import { describe, expect, it, vi } from 'vitest';
import { act, render, waitFor } from '@testing-library/react';
import { useState } from 'react';
import { useFetch, type Fetcher } from './useFetch';

function flush(ms = 0) {
  return new Promise<void>((resolve) => setTimeout(resolve, ms));
}

function Probe<T>({ fetcher, deps }: { fetcher: Fetcher<T>; deps: unknown[] }) {
  const { data, loading, error } = useFetch(fetcher, deps);
  return (
    <div>
      <span data-testid="loading">{loading ? 'L' : 'I'}</span>
      <span data-testid="error">{error ?? '∅'}</span>
      <span data-testid="data">{JSON.stringify(data)}</span>
    </div>
  );
}

describe('useFetch', () => {
  it('przekazuje AbortSignal do fetchera i abortuje przy odmontowaniu', async () => {
    const abortObserved: boolean[] = [];
    const fetcher = vi.fn(async (signal: AbortSignal) => {
      const wasAborted: boolean[] = [];
      signal.addEventListener('abort', () => wasAborted.push(true));
      await flush(20);
      abortObserved.push(signal.aborted);
      if (signal.aborted) {
        const err = new DOMException('Aborted', 'AbortError');
        throw err;
      }
      return 'OK';
    });

    const view = render(<Probe fetcher={fetcher} deps={[]} />);
    await flush(0);
    view.unmount();
    await flush(40);
    expect(abortObserved[0]).toBe(true);
    expect(fetcher).toHaveBeenCalledTimes(1);
  });

  it('zmiana deps anuluje poprzedni fetch i pokazuje tylko najnowsze dane', async () => {
    let callCount = 0;
    const fetcher = vi.fn(async (signal: AbortSignal) => {
      const id = ++callCount;
      await flush(id === 1 ? 50 : 5);
      if (signal.aborted) throw new DOMException('Aborted', 'AbortError');
      return `payload-${id}`;
    });

    function Wrapper() {
      const [dep, setDep] = useState(0);
      return (
        <>
          <button data-testid="bump" onClick={() => setDep((d) => d + 1)} />
          <Probe fetcher={fetcher} deps={[dep]} />
        </>
      );
    }

    const { getByTestId } = render(<Wrapper />);
    await flush(0);
    act(() => {
      getByTestId('bump').click();
    });

    await waitFor(() => {
      expect(getByTestId('data').textContent).toBe('"payload-2"');
    });
    expect(getByTestId('error').textContent).toBe('∅');
  });

  it('błąd inny niż AbortError trafia do `error`', async () => {
    const fetcher: Fetcher<string> = async () => {
      throw new Error('boom');
    };
    const view = render(<Probe fetcher={fetcher} deps={[]} />);
    await waitFor(() => {
      expect(view.getByTestId('error').textContent).toBe('boom');
    });
  });

  it('AbortError jest cichy gdy silentAbort=true (domyślnie)', async () => {
    const fetcher: Fetcher<string> = async () => {
      throw new DOMException('Aborted', 'AbortError');
    };
    const view = render(<Probe fetcher={fetcher} deps={[]} />);
    await flush(20);
    expect(view.getByTestId('error').textContent).toBe('∅');
  });
});
