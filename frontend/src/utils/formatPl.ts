/**
 * Centralne formatowanie dat/godzin/liczb dla całego frontendu.
 * Wszystko używa locale 'pl-PL' i jest reentrant (tworzymy formatter raz, reużywamy).
 *
 * Zasada: w UI nie wołamy `new Date(x).toLocaleString(...)` z parametrami w-miejscu
 * (różne strony pokazywałyby różne formaty tej samej daty). Zamiast tego — funkcje stąd.
 */

const fmtDateTimeShort = new Intl.DateTimeFormat('pl-PL', {
  day: '2-digit',
  month: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
});

const fmtDateTimeFull = new Intl.DateTimeFormat('pl-PL', {
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
});

const fmtDateOnly = new Intl.DateTimeFormat('pl-PL', {
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
});

const fmtTimeOnly = new Intl.DateTimeFormat('pl-PL', {
  hour: '2-digit',
  minute: '2-digit',
});

const fmtNumberOneDecimal = new Intl.NumberFormat('pl-PL', {
  maximumFractionDigits: 1,
  minimumFractionDigits: 0,
});

const fmtNumberInteger = new Intl.NumberFormat('pl-PL', {
  maximumFractionDigits: 0,
});

function toDate(value: string | number | Date | null | undefined): Date | null {
  if (value === null || value === undefined) return null;
  const d = value instanceof Date ? value : new Date(value);
  return Number.isNaN(d.getTime()) ? null : d;
}

/** „24.04 17:32” — krótki format używany w listach pomocniczych. */
export function formatDateTimeShortPl(value: string | number | Date | null | undefined): string {
  const d = toDate(value);
  return d ? fmtDateTimeShort.format(d) : '—';
}

/** „24.04.2026, 17:32” — pełny format, np. detale wykresów. */
export function formatDateTimeFullPl(value: string | number | Date | null | undefined): string {
  const d = toDate(value);
  return d ? fmtDateTimeFull.format(d) : '—';
}

/** „24.04.2026” — sama data. */
export function formatDatePl(value: string | number | Date | null | undefined): string {
  const d = toDate(value);
  return d ? fmtDateOnly.format(d) : '—';
}

/** „17:32” — sama godzina. */
export function formatTimePl(value: string | number | Date | null | undefined): string {
  const d = toDate(value);
  return d ? fmtTimeOnly.format(d) : '—';
}

/** Liczba z 1 miejscem po przecinku, polski separator dziesiętny. */
export function formatNumberPl(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '—';
  return fmtNumberOneDecimal.format(value);
}

/** Liczba całkowita w formacie pl-PL. */
export function formatIntegerPl(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '—';
  return fmtNumberInteger.format(value);
}

/**
 * Stabilny klucz dla list React: ISO timestamp + nazwa/identyfikator,
 * żeby nie używać `key={i}` (które łamie reconciliation przy reorderingu).
 *
 * Przykład: `keyFromTimestampAndLabel(e.detectedAtUtc, 'watering')`.
 */
export function keyFromTimestampAndLabel(
  timestamp: string | number | Date | null | undefined,
  label: string,
): string {
  const d = toDate(timestamp);
  const ts = d ? d.toISOString() : 'unknown';
  return `${ts}__${label}`;
}
