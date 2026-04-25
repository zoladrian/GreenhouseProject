import { act, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { useOnline } from './useOnline';

function Probe() {
  const online = useOnline();
  return <span data-testid="online">{online ? 'on' : 'off'}</span>;
}

function setNavigatorOnline(value: boolean) {
  Object.defineProperty(window.navigator, 'onLine', {
    configurable: true,
    get: () => value,
  });
}

describe('useOnline', () => {
  it('zwraca true gdy navigator.onLine === true', () => {
    setNavigatorOnline(true);
    render(<Probe />);
    expect(screen.getByTestId('online').textContent).toBe('on');
  });

  it('aktualizuje się przy evencie offline / online', () => {
    setNavigatorOnline(true);
    render(<Probe />);
    expect(screen.getByTestId('online').textContent).toBe('on');

    act(() => {
      setNavigatorOnline(false);
      window.dispatchEvent(new Event('offline'));
    });
    expect(screen.getByTestId('online').textContent).toBe('off');

    act(() => {
      setNavigatorOnline(true);
      window.dispatchEvent(new Event('online'));
    });
    expect(screen.getByTestId('online').textContent).toBe('on');
  });
});
