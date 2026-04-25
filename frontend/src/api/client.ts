const BASE = '/api';

/**
 * Wyjątek z dodatkowymi metadanymi: status HTTP i ciało odpowiedzi (jeśli dało się przeczytać).
 * Pozwala UI pokazać użytkownikowi treść błędu zamiast surowego "HTTP 500".
 */
export class ApiError extends Error {
  readonly status: number;
  readonly bodySnippet: string | null;

  constructor(status: number, message: string, bodySnippet: string | null) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.bodySnippet = bodySnippet;
  }
}

async function buildErrorMessage(resp: Response): Promise<{ msg: string; snippet: string | null }> {
  const fallback = `HTTP ${resp.status}`;
  // Body można odczytać tylko raz — najpierw spróbuj jako JSON z polem `error` lub `title` (RFC7807),
  // potem jako tekst (max 240 znaków, żeby nie zalać UI).
  try {
    const txt = await resp.text();
    if (!txt) return { msg: fallback, snippet: null };
    try {
      const j = JSON.parse(txt) as { error?: string; title?: string; detail?: string; message?: string };
      const fromJson = j.error ?? j.title ?? j.detail ?? j.message;
      if (typeof fromJson === 'string' && fromJson.trim().length > 0) {
        return { msg: `${fallback}: ${fromJson}`, snippet: txt.length > 240 ? txt.slice(0, 240) + '…' : txt };
      }
    } catch {
      /* nie JSON — zwracamy tekst */
    }
    const snippet = txt.length > 240 ? txt.slice(0, 240) + '…' : txt;
    return { msg: `${fallback}: ${snippet}`, snippet };
  } catch {
    return { msg: fallback, snippet: null };
  }
}

async function fetchJson<T>(url: string, init?: RequestInit): Promise<T> {
  const resp = await fetch(`${BASE}${url}`, init);
  if (!resp.ok) {
    const { msg, snippet } = await buildErrorMessage(resp);
    throw new ApiError(resp.status, msg, snippet);
  }
  return resp.json();
}

async function putJson<T>(url: string, body: unknown, init?: RequestInit): Promise<T> {
  const resp = await fetch(`${BASE}${url}`, {
    ...init,
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...(init?.headers ?? {}) },
    body: JSON.stringify(body),
  });
  if (!resp.ok) {
    const { msg, snippet } = await buildErrorMessage(resp);
    throw new ApiError(resp.status, msg, snippet);
  }
  return resp.json();
}

async function postJson<T>(url: string, body: unknown, init?: RequestInit): Promise<T> {
  const resp = await fetch(`${BASE}${url}`, {
    ...init,
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...(init?.headers ?? {}) },
    body: JSON.stringify(body),
  });
  if (!resp.ok) {
    const { msg, snippet } = await buildErrorMessage(resp);
    throw new ApiError(resp.status, msg, snippet);
  }
  return resp.json();
}

export interface NawaSnapshot {
  nawaId: string;
  nawaName: string;
  plantNote: string | null;
  /** 0 Ok, 1 Za mokro, 2 Sucho, 3 Brak danych, 4 Sprzeczne czujniki, 5 Rozstrzał, 6 Po podlaniu (krótkie przemoczenie) */
  status: number;
  sensorCount: number;
  moistureReadingCount: number;
  avgMoisture: number | null;
  minMoisture: number | null;
  maxMoisture: number | null;
  moistureSpread: number | null;
  avgTemperature: number | null;
  lowestBattery: number | null;
  oldestReadingUtc: string | null;
  generatedAtUtc: string;
  /** Progi z ustawień nawy (do legendy na pulpicie) */
  moistureMin: number | null;
  moistureMax: number | null;
  temperatureMin: number | null;
  temperatureMax: number | null;
  /** Krótka wypowiedź TTS o ostatnim podlaniu (sucho / konflikt / po podlaniu). */
  wateringSpeechNote?: string | null;
}

export interface NawaDto {
  id: string;
  name: string;
  description: string | null;
  plantNote: string | null;
  isActive: boolean;
  moistureMin: number | null;
  moistureMax: number | null;
  temperatureMin: number | null;
  temperatureMax: number | null;
  createdAtUtc: string;
}

export interface NawaDetailDto extends NawaDto {
  sensors: SensorListItem[];
}

export interface SensorListItem {
  id: string;
  externalId: string;
  displayName: string | null;
  kind: string;
  nawaId: string | null;
  createdAtUtc: string;
}

export interface SensorHealthDto {
  sensorId: string;
  externalId: string;
  displayName: string | null;
  kind: string;
  nawaId: string | null;
  battery: number | null;
  linkQuality: number | null;
  cleaningReminder: boolean | null;
  rain: boolean | null;
  rainIntensityRaw: number | null;
  illuminanceRaw: number | null;
  lastReadingUtc: string | null;
  totalReadings24h: number;
}

export interface MoisturePoint {
  utcTime: string;
  sensorIdentifier: string;
  sensorId: string | null;
  soilMoisture: number | null;
  temperature: number | null;
  battery: number | null;
  linkQuality: number | null;
}

export interface WeatherPoint {
  utcTime: string;
  sensorIdentifier: string;
  sensorId: string | null;
  rain: boolean | null;
  rainIntensityRaw: number | null;
  illuminanceRaw: number | null;
  illuminanceAverage20MinRaw: number | null;
  illuminanceMaximumTodayRaw: number | null;
  battery: number | null;
  linkQuality: number | null;
  cleaningReminder: boolean | null;
  rainLevel: number | null;
  lightLevel: number | null;
  isNightBySchedule: boolean;
  currentRainStatus: 'auto' | 'raining' | 'no-rain' | 'high-humidity' | 'unknown';
  currentLightStatus: 'auto' | 'sunny' | 'cloudy' | 'night' | 'unknown';
}

/** Z API (camelCase enum z .NET). */
export type WateringInferredKind = 'unknown' | 'likelyManual' | 'likelyRain';

export interface WateringEventDto {
  detectedAtUtc: string;
  moistureBefore: number;
  moistureAfter: number;
  deltaMoisture: number;
  windowDuration: string;
  inferredKind: WateringInferredKind;
  contributingSensorCount: number;
}

export interface DryingRateDto {
  sensorIdentifier: string;
  sensorId: string | null;
  windowStart: string;
  windowEnd: string;
  percentPerHour: number;
}

export interface NawaVoiceLineDto {
  order: number;
  nawaName: string;
  avgTemperature: number | null;
  avgSoilMoisture: number | null;
  readingCount: number;
  assignedSensorCount: number;
  moistureAssessment: string;
  temperatureAssessment: string;
}

export interface NawaVoiceBriefDto {
  nawaName: string;
  spokenText: string;
}

export interface VoiceDailyReportDto {
  greetingLeadin: string;
  localTime: string;
  localDateLong: string;
  nawy: NawaVoiceLineDto[];
}

export interface VoiceWeatherReportDto {
  greetingLeadin: string;
  localTime: string;
  localDateLong: string;
  rainStatus: string;
  lightStatus: string;
  isNightBySchedule: boolean;
  rainIntensityRaw: number | null;
  illuminanceRaw: number | null;
  sourceUtcTime: string | null;
}

export interface WeatherControlConfigDto {
  rainDetectedMinRaw: number;
  highHumidityMinRaw: number;
  sunnyMinRaw: number;
  cloudyMaxRaw: number;
  sunriseLocal: string;
  sunsetLocal: string;
  manualRainStatus: 'auto' | 'raining' | 'no-rain' | 'high-humidity';
  manualLightStatus: 'auto' | 'sunny' | 'cloudy' | 'night';
  updatedAtUtc: string;
}

export interface WeatherCurrentStatusDto {
  rainStatus: string;
  lightStatus: string;
  isNightBySchedule: boolean;
  rainIntensityRaw: number | null;
  illuminanceRaw: number | null;
  sourceUtcTime: string | null;
}

export interface SunScheduleEntryDto {
  date: string;
  sunriseLocal: string;
  sunsetLocal: string;
}

export interface SunScheduleImportResultDto {
  importedRows: number;
  ignoredRows: number;
}

const sig = (signal?: AbortSignal): RequestInit | undefined => (signal ? { signal } : undefined);

export const api = {
  getVoiceDailyReport: (signal?: AbortSignal) => fetchJson<VoiceDailyReportDto>('/voice/daily-report', sig(signal)),
  getVoiceClimateReport: (signal?: AbortSignal) => fetchJson<VoiceDailyReportDto>('/voice/daily-report-climate', sig(signal)),
  getVoiceWeatherReport: (signal?: AbortSignal) => fetchJson<VoiceWeatherReportDto>('/voice/daily-report-weather', sig(signal)),
  getNawaVoiceBrief: (id: string, signal?: AbortSignal) =>
    fetchJson<NawaVoiceBriefDto>(`/voice/nawa/${id}/brief`, sig(signal)),
  getDashboard: (signal?: AbortSignal) => fetchJson<NawaSnapshot[]>('/dashboard', sig(signal)),
  getNawy: (signal?: AbortSignal) => fetchJson<NawaDto[]>('/nawa', sig(signal)),
  getNawaDetail: (id: string, signal?: AbortSignal) => fetchJson<NawaDetailDto>(`/nawa/${id}/detail`, sig(signal)),
  createNawa: (body: { name: string; description?: string }) => postJson<NawaDto>('/nawa', body),
  updateNawa: (id: string, body: Record<string, unknown>) => putJson<NawaDto>(`/nawa/${id}`, body),
  getSensors: (signal?: AbortSignal) => fetchJson<SensorListItem[]>('/sensor', sig(signal)),
  getSensorHealth: (signal?: AbortSignal) => fetchJson<SensorHealthDto[]>('/sensor/health', sig(signal)),
  assignSensor: async (sensorId: string, nawaId: string | null) => {
    const resp = await fetch(`${BASE}/sensor/${sensorId}/nawa`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ nawaId }),
    });
    if (!resp.ok) {
      const { msg, snippet } = await buildErrorMessage(resp);
      throw new ApiError(resp.status, msg, snippet);
    }
  },
  updateSensorDisplayName: (sensorId: string, displayName: string | null) =>
    putJson<SensorListItem>(`/sensor/${sensorId}/display-name`, { displayName }),
  deleteSensor: async (sensorId: string) => {
    const resp = await fetch(`${BASE}/sensor/${sensorId}`, { method: 'DELETE' });
    if (resp.status === 404) {
      throw new ApiError(404, 'Czujnik nie istnieje.', null);
    }
    if (!resp.ok) {
      const { msg, snippet } = await buildErrorMessage(resp);
      throw new ApiError(resp.status, msg, snippet);
    }
  },
  getMoistureSeries: (params: string, signal?: AbortSignal) =>
    fetchJson<MoisturePoint[]>(`/chart/moisture?${params}`, sig(signal)),
  getWeatherSeries: (params: string, signal?: AbortSignal) =>
    fetchJson<WeatherPoint[]>(`/chart/weather?${params}`, sig(signal)),
  getWateringEvents: (nawaId: string, from?: string, to?: string, signal?: AbortSignal) => {
    let qs = `nawaId=${nawaId}`;
    if (from) qs += `&from=${from}`;
    if (to) qs += `&to=${to}`;
    return fetchJson<WateringEventDto[]>(`/chart/watering-events?${qs}`, sig(signal));
  },
  getDryingRates: (nawaId: string, from?: string, to?: string, signal?: AbortSignal) => {
    let qs = `nawaId=${nawaId}`;
    if (from) qs += `&from=${from}`;
    if (to) qs += `&to=${to}`;
    return fetchJson<DryingRateDto[]>(`/chart/drying-rate?${qs}`, sig(signal));
  },
  getWeatherConfig: (signal?: AbortSignal) => fetchJson<WeatherControlConfigDto>('/weather/config', sig(signal)),
  updateWeatherConfig: (body: WeatherControlConfigDto) => putJson<WeatherControlConfigDto>('/weather/config', body),
  getWeatherCurrentStatus: (signal?: AbortSignal) => fetchJson<WeatherCurrentStatusDto>('/weather/current-status', sig(signal)),
  getSunSchedule: (from?: string, to?: string, signal?: AbortSignal) => {
    let qs = '';
    if (from) qs += `from=${encodeURIComponent(from)}`;
    if (to) qs += `${qs ? '&' : ''}to=${encodeURIComponent(to)}`;
    return fetchJson<SunScheduleEntryDto[]>(`/weather/sun-schedule${qs ? `?${qs}` : ''}`, sig(signal));
  },
  importSunScheduleCsv: async (csvContent: string) => {
    const resp = await fetch(`${BASE}/weather/sun-schedule/import`, {
      method: 'POST',
      headers: { 'Content-Type': 'text/csv' },
      body: csvContent,
    });
    if (!resp.ok) {
      const { msg, snippet } = await buildErrorMessage(resp);
      throw new ApiError(resp.status, msg, snippet);
    }
    return (await resp.json()) as SunScheduleImportResultDto;
  },
};
