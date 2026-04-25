/** Wyświetlanie czasu na wykresach — zawsze jak w Polsce, niezależnie od strefy przeglądarki. */
export const CHART_TIME_ZONE = 'Europe/Warsaw';

const axisFmt = new Intl.DateTimeFormat('pl-PL', {
  timeZone: CHART_TIME_ZONE,
  day: '2-digit',
  month: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
});

const tooltipFmt = new Intl.DateTimeFormat('pl-PL', {
  timeZone: CHART_TIME_ZONE,
  weekday: 'short',
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  second: '2-digit',
});

const axisShortTimeFmt = new Intl.DateTimeFormat('pl-PL', {
  timeZone: CHART_TIME_ZONE,
  hour: '2-digit',
  minute: '2-digit',
});

const axisDayTimeFmt = new Intl.DateTimeFormat('pl-PL', {
  timeZone: CHART_TIME_ZONE,
  day: '2-digit',
  month: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
});

const axisDayFmt = new Intl.DateTimeFormat('pl-PL', {
  timeZone: CHART_TIME_ZONE,
  day: '2-digit',
  month: '2-digit',
});

const axisMonthFmt = new Intl.DateTimeFormat('pl-PL', {
  timeZone: CHART_TIME_ZONE,
  month: '2-digit',
  year: 'numeric',
});

/**
 * API zwraca czas w UTC; jeśli brak „Z”/offsetu w ISO, dopisujemy Z (zgodnie z konwerterem JSON po stronie serwera).
 */
export function utcIsoToMs(iso: string): number {
  const s = iso.trim();
  if (!s) return NaN;
  if (/Z$/i.test(s) || /[+-]\d{2}:?\d{2}$/.test(s)) return Date.parse(s);
  return Date.parse(`${s}Z`);
}

export function formatPlAxisTime(value: number | string): string {
  const n = typeof value === 'number' ? value : Date.parse(value);
  if (Number.isNaN(n)) return '';
  return axisFmt.format(n);
}

export function inferRangeMs(timesMs: number[]): number | null {
  if (!timesMs.length) return null;
  const sorted = [...timesMs].sort((a, b) => a - b);
  return Math.max(0, sorted[sorted.length - 1] - sorted[0]);
}

/** Dolny margines siatki (legenda + etykiety osi czasu, w tym po rotacji dla długich zakresów). */
export function chartGridBottomPl(rangeMs: number | null): number {
  if (rangeMs != null && rangeMs > 48 * 3600_000) return 78;
  if (rangeMs != null && rangeMs > 24 * 3600_000) return 52;
  return 40;
}

function formatAdaptiveAxisTime(value: number, rangeMs: number | null): string {
  if (rangeMs == null) return formatPlAxisTime(value);
  if (rangeMs <= 24 * 3600_000) return axisShortTimeFmt.format(value);
  if (rangeMs <= 48 * 3600_000) return axisDayTimeFmt.format(value);
  if (rangeMs <= 45 * 24 * 3600_000) return axisDayFmt.format(value);
  return axisMonthFmt.format(value);
}

export function formatPlTooltipTime(value: number | string): string {
  const n = typeof value === 'number' ? value : Date.parse(value);
  if (Number.isNaN(n)) return '';
  return tooltipFmt.format(n);
}

export function echartsTimeXAxisPl(rangeMs?: number | null, bounds?: { minMs: number; maxMs: number } | null) {
  const r = rangeMs ?? null;
  const minInterval =
    r == null
      ? undefined
      : r <= 6 * 3600_000
        ? 15 * 60_000
        : r <= 48 * 3600_000
          ? 60 * 60_000
          : r <= 7 * 24 * 3600_000
            ? 12 * 60 * 60_000
            : r <= 45 * 24 * 3600_000
              ? 2 * 24 * 60 * 60_000
              : 7 * 24 * 60 * 60_000;
  const longRange = r != null && r > 48 * 3600_000;
  return {
    type: 'time' as const,
    min: bounds?.minMs,
    max: bounds?.maxMs,
    minInterval,
    axisLabel: {
      formatter: (v: number) => formatAdaptiveAxisTime(v, r),
      hideOverlap: true,
      rotate: longRange ? 40 : 0,
      margin: longRange ? 14 : 8,
      overflow: 'truncate' as const,
      width: longRange ? 72 : undefined,
    },
  };
}

export function echartsAxisTooltipPl() {
  return {
    trigger: 'axis' as const,
    axisPointer: {
      label: {
        formatter: (p: { value?: number | string }) => formatPlTooltipTime(p.value ?? 0),
      },
    },
    formatter: (params: unknown) => {
      const ps = params as {
        axisValue: number;
        marker?: string;
        seriesName: string;
        value: [number, number] | number;
      }[];
      if (!Array.isArray(ps) || ps.length === 0) return '';
      const head = formatPlTooltipTime(ps[0].axisValue);
      const lines = ps.map((p) => {
        const raw = p.value;
        const y = Array.isArray(raw) ? raw[1] : raw;
        const mark = p.marker ?? '';
        return `${mark}${p.seriesName}: ${y}`;
      });
      return `${head}<br/>${lines.join('<br/>')}`;
    },
  };
}
