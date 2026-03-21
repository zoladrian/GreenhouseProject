import { useCallback, useRef, useState } from 'react';

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

export function useTts() {
  const [enabled, setEnabled] = useState(false);
  const lastSpoke = useRef(0);
  const cooldownMs = 30_000;

  /** Wywołaj z obsługi zdarzenia użytkownika (np. klik) — wymagane na części mobilnych Safari. */
  const speakImmediate = useCallback((text: string) => {
    speakPolish(text);
  }, []);

  const speak = useCallback(
    (text: string) => {
      if (!enabled) return;
      if (!('speechSynthesis' in window)) return;
      const now = Date.now();
      if (now - lastSpoke.current < cooldownMs) return;
      lastSpoke.current = now;
      speakPolish(text);
    },
    [enabled],
  );

  return { enabled, setEnabled, speak, speakImmediate };
}
