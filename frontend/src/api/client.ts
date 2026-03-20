const BASE = '/api';

async function fetchJson<T>(url: string): Promise<T> {
  const resp = await fetch(`${BASE}${url}`);
  if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
  return resp.json();
}

async function putJson<T>(url: string, body: unknown): Promise<T> {
  const resp = await fetch(`${BASE}${url}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
  return resp.json();
}

async function postJson<T>(url: string, body: unknown): Promise<T> {
  const resp = await fetch(`${BASE}${url}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
  return resp.json();
}

export interface NawaSnapshot {
  nawaId: string;
  nawaName: string;
  plantNote: string | null;
  status: number;
  sensorCount: number;
  avgMoisture: number | null;
  minMoisture: number | null;
  maxMoisture: number | null;
  avgTemperature: number | null;
  lowestBattery: number | null;
  oldestReadingUtc: string | null;
  generatedAtUtc: string;
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
  nawaId: string | null;
  createdAtUtc: string;
}

export interface SensorHealthDto {
  sensorId: string;
  externalId: string;
  displayName: string | null;
  nawaId: string | null;
  battery: number | null;
  linkQuality: number | null;
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

export interface WateringEventDto {
  detectedAtUtc: string;
  moistureBefore: number;
  moistureAfter: number;
  deltaMoisture: number;
  windowDuration: string;
}

export interface DryingRateDto {
  sensorIdentifier: string;
  sensorId: string | null;
  windowStart: string;
  windowEnd: string;
  percentPerHour: number;
}

export const api = {
  getDashboard: () => fetchJson<NawaSnapshot[]>('/dashboard'),
  getNawy: () => fetchJson<NawaDto[]>('/nawa'),
  getNawaDetail: (id: string) => fetchJson<NawaDetailDto>(`/nawa/${id}/detail`),
  createNawa: (body: { name: string; description?: string }) => postJson<NawaDto>('/nawa', body),
  updateNawa: (id: string, body: Record<string, unknown>) => putJson<NawaDto>(`/nawa/${id}`, body),
  getSensors: () => fetchJson<SensorListItem[]>('/sensor'),
  getSensorHealth: () => fetchJson<SensorHealthDto[]>('/sensor/health'),
  assignSensor: async (sensorId: string, nawaId: string | null) => {
    const resp = await fetch(`${BASE}/sensor/${sensorId}/nawa`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ nawaId }),
    });
    if (!resp.ok) {
      let msg = `HTTP ${resp.status}`;
      try {
        const j = (await resp.json()) as { error?: string };
        if (j?.error) msg = j.error;
      } catch {
        /* ignore */
      }
      throw new Error(msg);
    }
  },
  updateSensorDisplayName: (sensorId: string, displayName: string | null) =>
    putJson<SensorListItem>(`/sensor/${sensorId}/display-name`, { displayName }),
  getMoistureSeries: (params: string) => fetchJson<MoisturePoint[]>(`/chart/moisture?${params}`),
  getWateringEvents: (nawaId: string, from?: string, to?: string) => {
    let qs = `nawaId=${nawaId}`;
    if (from) qs += `&from=${from}`;
    if (to) qs += `&to=${to}`;
    return fetchJson<WateringEventDto[]>(`/chart/watering-events?${qs}`);
  },
  getDryingRates: (nawaId: string, from?: string, to?: string) => {
    let qs = `nawaId=${nawaId}`;
    if (from) qs += `&from=${from}`;
    if (to) qs += `&to=${to}`;
    return fetchJson<DryingRateDto[]>(`/chart/drying-rate?${qs}`);
  },
};
