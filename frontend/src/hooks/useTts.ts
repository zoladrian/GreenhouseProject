import { useCallback, useRef, useState } from 'react';

export function useTts() {
  const [enabled, setEnabled] = useState(false);
  const lastSpoke = useRef(0);
  const cooldownMs = 30_000;

  const speak = useCallback(
    (text: string) => {
      if (!enabled) return;
      if (!('speechSynthesis' in window)) return;
      const now = Date.now();
      if (now - lastSpoke.current < cooldownMs) return;
      lastSpoke.current = now;

      const utterance = new SpeechSynthesisUtterance(text);
      utterance.lang = 'pl-PL';
      utterance.rate = 0.9;
      speechSynthesis.speak(utterance);
    },
    [enabled],
  );

  return { enabled, setEnabled, speak };
}
