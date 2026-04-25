import { act, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ConnectivityBanner } from './ConnectivityBanner';

function setNavigatorOnline(value: boolean) {
  Object.defineProperty(window.navigator, 'onLine', {
    configurable: true,
    get: () => value,
  });
}

describe('ConnectivityBanner', () => {
  it('nie renderuje nic gdy online', () => {
    setNavigatorOnline(true);
    render(<ConnectivityBanner />);
    expect(screen.queryByTestId('connectivity-banner')).toBeNull();
  });

  it('pokazuje banner po przejściu na offline i chowa po online', () => {
    setNavigatorOnline(true);
    render(<ConnectivityBanner />);
    expect(screen.queryByTestId('connectivity-banner')).toBeNull();

    act(() => {
      setNavigatorOnline(false);
      window.dispatchEvent(new Event('offline'));
    });
    const banner = screen.getByTestId('connectivity-banner');
    expect(banner).toBeTruthy();
    expect(banner.textContent).toMatch(/Brak sieci/i);

    act(() => {
      setNavigatorOnline(true);
      window.dispatchEvent(new Event('online'));
    });
    expect(screen.queryByTestId('connectivity-banner')).toBeNull();
  });
});
