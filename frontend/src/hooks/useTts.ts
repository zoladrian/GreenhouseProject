import { useCallback, useMemo, useRef, useState } from 'react';

/** Dzieli tekst na krótsze wypowiedzi — lepsze pauzy niż jedno długie zdanie (Web Speech bez SSML). */
function speakPolishChunks(fullText: string) {
  const chunks = fullText
    .split(/(?<=[.!?])\s+/)
    .map((s) => s.trim())
    .filter(Boolean);
  if (chunks.length === 0) return;

  let i = 0;
  const next = () => {
    if (i >= chunks.length) return;
    const utterance = new SpeechSynthesisUtterance(chunks[i]);
    utterance.lang = 'pl-PL';
    utterance.rate = 0.9;
    i += 1;
    utterance.onend = () => {
      window.setTimeout(next, 120);
    };
    speechSynthesis.speak(utterance);
  };
  next();
}

/** Synteza mowy PL — wywołuj z gestu użytkownika (klik), zwłaszcza na iOS. */
export function speakPolish(text: string) {
  if (!('speechSynthesis' in window)) return;
  try {
    speechSynthesis.cancel();
    if (text.length > 120 && /[.!?]\s/.test(text)) {
      speakPolishChunks(text);
      return;
    }
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = 'pl-PL';
    utterance.rate = 0.9;
    speechSynthesis.speak(utterance);
  } catch {
    /* starsze WebView / blokady przeglądarki */
  }
}

export interface TtsHandle {
  enabled: boolean;
  setEnabled: (v: boolean) => void;
  /** Wypowiedź ograniczona cooldownem; nie zadziała przy `enabled=false`. */
  speak: (text: string) => void;
  /** Wymuszona wypowiedź (klik użytkownika) — bez cooldownu, ignoruje `enabled`. */
  speakImmediate: (text: string) => void;
}

export function useTts(): TtsHandle {
  const [enabled, setEnabled] = useState(false);
  const lastSpoke = useRef(0);
  const cooldownMs = 30_000;
  // Trzymamy enabled w refie, żeby `speak` mogło być stabilne (deps tylko `[]`).
  // Inaczej każdy konsument, który włoży obiekt `tts` do deps, re-firowałby effect
  // przy każdym renderze rodzica.
  const enabledRef = useRef(enabled);
  enabledRef.current = enabled;

  const speakImmediate = useCallback((text: string) => {
    speakPolish(text);
  }, []);

  const speak = useCallback((text: string) => {
    if (!enabledRef.current) return;
    if (!('speechSynthesis' in window)) return;
    const now = Date.now();
    if (now - lastSpoke.current < cooldownMs) return;
    lastSpoke.current = now;
    speakPolish(text);
  }, []);

  // Stabilna referencja: zmiania tylko gdy `enabled` się zmieni (co w UI dzieje się rzadko).
  return useMemo<TtsHandle>(
    () => ({ enabled, setEnabled, speak, speakImmediate }),
    [enabled, speak, speakImmediate],
  );
}
