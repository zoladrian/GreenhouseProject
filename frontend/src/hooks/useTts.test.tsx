import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { act, renderHook } from '@testing-library/react';
import { useTts } from './useTts';

class FakeUtter {
  text: string;
  lang: string = '';
  rate: number = 1;
  onend: (() => void) | null = null;
  constructor(text: string) {
    this.text = text;
  }
}

const speakSpy = vi.fn();
const cancelSpy = vi.fn();

beforeEach(() => {
  speakSpy.mockClear();
  cancelSpy.mockClear();
  // jsdom nie ma speechSynthesis — podstawiamy stub.
  Object.defineProperty(globalThis, 'speechSynthesis', {
    configurable: true,
    value: { speak: speakSpy, cancel: cancelSpy },
  });
  Object.defineProperty(globalThis, 'SpeechSynthesisUtterance', {
    configurable: true,
    value: FakeUtter,
  });
  Object.defineProperty(window, 'speechSynthesis', {
    configurable: true,
    value: { speak: speakSpy, cancel: cancelSpy },
  });
});

afterEach(() => {
  vi.useRealTimers();
});

describe('useTts', () => {
  it('zwraca stabilną referencję `speak` i `speakImmediate` między renderami', () => {
    const { result, rerender } = renderHook(() => useTts());
    const first = { speak: result.current.speak, speakImmediate: result.current.speakImmediate };
    rerender();
    rerender();
    expect(result.current.speak).toBe(first.speak);
    expect(result.current.speakImmediate).toBe(first.speakImmediate);
  });

  it('speak respektuje cooldown i flag enabled', () => {
    const { result } = renderHook(() => useTts());
    act(() => {
      result.current.setEnabled(false);
    });
    act(() => {
      result.current.speak('A');
    });
    expect(speakSpy).not.toHaveBeenCalled();

    act(() => {
      result.current.setEnabled(true);
    });
    act(() => {
      result.current.speak('B');
    });
    expect(speakSpy).toHaveBeenCalledTimes(1);

    // Drugie wywołanie w tej samej chwili — cooldown blokuje.
    act(() => {
      result.current.speak('C');
    });
    expect(speakSpy).toHaveBeenCalledTimes(1);
  });

  it('speakImmediate ignoruje enabled', () => {
    const { result } = renderHook(() => useTts());
    act(() => {
      result.current.speakImmediate('teraz');
    });
    expect(speakSpy).toHaveBeenCalledTimes(1);
  });
});
