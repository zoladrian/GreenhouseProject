import { useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/client';
import { useFetch } from '../hooks/useFetch';
import { speakPolish } from '../hooks/useTts';
import { MoistureChart } from '../components/MoistureChart';
import { TemperatureChart } from '../components/TemperatureChart';
import { BatteryChart } from '../components/BatteryChart';
import { WeatherChart, type WeatherMetricKey } from '../components/WeatherChart';
import { useCallback, useEffect, useMemo, useState, type CSSProperties } from 'react';
import { QRCodeSVG } from 'qrcode.react';
import { NawyPageBackdrop } from '../components/NawyPageBackdrop';
import { formatDateTimeFullPl, keyFromTimestampAndLabel } from '../utils/formatPl';

type RangePreset = '1h' | '6h' | '24h' | '7d' | '30d' | 'custom';

const PRESET_HOURS: Record<Exclude<RangePreset, 'custom'>, number> = {
  '1h': 1,
  '6h': 6,
  '24h': 24,
  '7d': 168,
  '30d': 720,
};

const RAIN_METRICS: WeatherMetricKey[] = ['rain', 'rainIntensityRaw'];
const LIGHT_METRICS: WeatherMetricKey[] = ['illuminanceRaw', 'illuminanceAverage20MinRaw', 'illuminanceMaximumTodayRaw'];

type ManualRainStatus = 'auto' | 'raining' | 'no-rain' | 'high-humidity';
type ManualLightStatus = 'auto' | 'sunny' | 'cloudy' | 'night';

function toDatetimeLocalValue(d: Date) {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export function NawaDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: detail, loading, error: detailError, refetch: refetchDetail } = useFetch(
    (signal) => api.getNawaDetail(id!, signal),
    [id],
  );
  const { data: allSensors } = useFetch((signal) => api.getSensors(signal), []);
  const { data: weatherConfig, error: weatherConfigError, refetch: refetchWeatherConfig } = useFetch(
    (signal) => api.getWeatherConfig(signal),
    [],
  );
  const { data: weatherCurrentStatus, error: weatherStatusError, refetch: refetchWeatherStatus } = useFetch(
    (signal) => api.getWeatherCurrentStatus(signal),
    [],
  );
  const { data: sunSchedule, error: sunScheduleError, refetch: refetchSunSchedule } = useFetch(
    (signal) => api.getSunSchedule(undefined, undefined, signal),
    [],
  );

  const [rangePreset, setRangePreset] = useState<RangePreset>('24h');
  const [customFrom, setCustomFrom] = useState('');
  const [customTo, setCustomTo] = useState('');

  const { from, to } = useMemo(() => {
    const now = Date.now();
    if (rangePreset === 'custom' && customFrom && customTo) {
      const f = new Date(customFrom);
      const t = new Date(customTo);
      if (!Number.isNaN(f.getTime()) && !Number.isNaN(t.getTime()) && f < t) {
        return { from: f.toISOString(), to: t.toISOString() };
      }
    }
    const h = rangePreset === 'custom' ? 24 : PRESET_HOURS[rangePreset as Exclude<RangePreset, 'custom'>] ?? 24;
    return { from: new Date(now - h * 3600_000).toISOString(), to: new Date(now).toISOString() };
  }, [rangePreset, customFrom, customTo]);
  const selectedRangeMs = useMemo(() => {
    const fromMs = Date.parse(from);
    const toMs = Date.parse(to);
    if (Number.isNaN(fromMs) || Number.isNaN(toMs) || toMs <= fromMs) return null;
    return toMs - fromMs;
  }, [from, to]);
  const selectedTimeBounds = useMemo(() => {
    const minMs = Date.parse(from);
    const maxMs = Date.parse(to);
    if (Number.isNaN(minMs) || Number.isNaN(maxMs) || maxMs <= minMs) return null;
    return { minMs, maxMs };
  }, [from, to]);

  const { data: points, error: pointsError, refetch: refetchPoints } = useFetch(
    (signal) => api.getMoistureSeries(`nawaId=${id}&from=${from}&to=${to}`, signal),
    [id, from, to],
  );
  const { data: weatherPoints, error: weatherError, refetch: refetchWeather } = useFetch(
    (signal) => api.getWeatherSeries(`nawaId=${id}&from=${from}&to=${to}`, signal),
    [id, from, to],
  );
  const { data: wateringEvents, error: wateringError, refetch: refetchWatering } = useFetch(
    (signal) => api.getWateringEvents(id!, from, to, signal),
    [id, from, to],
  );
  const { data: dryingRates, error: dryingError, refetch: refetchDrying } = useFetch(
    (signal) => api.getDryingRates(id!, from, to, signal),
    [id, from, to],
  );

  const refetchAll = useCallback(() => {
    refetchDetail();
    refetchPoints();
    refetchWeather();
    refetchWeatherConfig();
    refetchWeatherStatus();
    refetchSunSchedule();
    refetchWatering();
    refetchDrying();
  }, [refetchDetail, refetchPoints, refetchWeather, refetchWeatherConfig, refetchWeatherStatus, refetchSunSchedule, refetchWatering, refetchDrying]);

  const fetchErrors = useMemo(
    () =>
      [
        detailError ? `Szczegóły nawy: ${detailError}` : null,
        pointsError ? `Wilgotność/temperatura/bateria: ${pointsError}` : null,
        weatherError ? `Pogoda: ${weatherError}` : null,
        weatherConfigError ? `Konfiguracja pogody: ${weatherConfigError}` : null,
        weatherStatusError ? `Status pogody: ${weatherStatusError}` : null,
        sunScheduleError ? `Harmonogram słońca: ${sunScheduleError}` : null,
        wateringError ? `Skoki wilgotności: ${wateringError}` : null,
        dryingError ? `Tempo wysychania: ${dryingError}` : null,
      ].filter((x): x is string => x !== null),
    [detailError, pointsError, weatherError, weatherConfigError, weatherStatusError, sunScheduleError, wateringError, dryingError],
  );

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [plantNote, setPlantNote] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [moistureMin, setMoistureMin] = useState('');
  const [moistureMax, setMoistureMax] = useState('');
  const [temperatureMin, setTemperatureMin] = useState('');
  const [temperatureMax, setTemperatureMax] = useState('');
  const [saving, setSaving] = useState(false);
  const [saveMsg, setSaveMsg] = useState<string | null>(null);
  const [voiceBriefLoading, setVoiceBriefLoading] = useState(false);
  const [voiceBriefError, setVoiceBriefError] = useState<string | null>(null);
  const [weatherSaving, setWeatherSaving] = useState(false);
  const [weatherSaveMsg, setWeatherSaveMsg] = useState<string | null>(null);
  const [rainDetectedMinRaw, setRainDetectedMinRaw] = useState('');
  const [highHumidityMinRaw, setHighHumidityMinRaw] = useState('');
  const [sunnyMinRaw, setSunnyMinRaw] = useState('');
  const [cloudyMaxRaw, setCloudyMaxRaw] = useState('');
  const [sunriseLocal, setSunriseLocal] = useState('06:00');
  const [sunsetLocal, setSunsetLocal] = useState('20:00');
  const [manualRainStatus, setManualRainStatus] = useState<ManualRainStatus>('auto');
  const [manualLightStatus, setManualLightStatus] = useState<ManualLightStatus>('auto');
  const [sunCsvImportMsg, setSunCsvImportMsg] = useState<string | null>(null);
  const batteryPointsAllSensors = useMemo(() => {
    const soil = points ?? [];
    const weatherAsBatterySeries = (weatherPoints ?? []).map((p) => ({
      utcTime: p.utcTime,
      sensorIdentifier: p.sensorIdentifier,
      sensorId: p.sensorId,
      soilMoisture: null,
      temperature: null,
      battery: p.battery,
      linkQuality: p.linkQuality,
    }));

    const merged = [...soil, ...weatherAsBatterySeries];
    const seen = new Set<string>();
    const deduped: typeof merged = [];
    for (const row of merged) {
      const k = `${row.sensorId ?? row.sensorIdentifier}|${row.utcTime}|${row.battery ?? 'null'}`;
      if (seen.has(k)) continue;
      seen.add(k);
      deduped.push(row);
    }
    return deduped;
  }, [points, weatherPoints]);

  useEffect(() => {
    if (!detail) return;
    setName(detail.name);
    setDescription(detail.description ?? '');
    setPlantNote(detail.plantNote ?? '');
    setIsActive(detail.isActive);
    setMoistureMin(detail.moistureMin != null ? String(detail.moistureMin) : '');
    setMoistureMax(detail.moistureMax != null ? String(detail.moistureMax) : '');
    setTemperatureMin(detail.temperatureMin != null ? String(detail.temperatureMin) : '');
    setTemperatureMax(detail.temperatureMax != null ? String(detail.temperatureMax) : '');
  }, [detail]);

  useEffect(() => {
    if (!weatherConfig) return;
    setRainDetectedMinRaw(String(weatherConfig.rainDetectedMinRaw));
    setHighHumidityMinRaw(String(weatherConfig.highHumidityMinRaw));
    setSunnyMinRaw(String(weatherConfig.sunnyMinRaw));
    setCloudyMaxRaw(String(weatherConfig.cloudyMaxRaw));
    setSunriseLocal(weatherConfig.sunriseLocal);
    setSunsetLocal(weatherConfig.sunsetLocal);
    setManualRainStatus(weatherConfig.manualRainStatus);
    setManualLightStatus(weatherConfig.manualLightStatus);
  }, [weatherConfig]);

  /** Jedna seria na wykresie na czujnik — scala stare i nowe `sensorIdentifier` z MQTT po zmianie nazwy w Z2M. */
  const sensorLegendById = useMemo(() => {
    const fromAll = allSensors ?? [];
    if (!detail?.sensors?.length && fromAll.length === 0) return undefined;
    const rows = [...fromAll, ...(detail?.sensors ?? [])];
    return Object.fromEntries(rows.map((s) => [s.id, s.displayName?.trim() || s.externalId || s.id]));
  }, [allSensors, detail?.sensors]);

  const startCustomRange = () => {
    const end = new Date();
    const start = new Date(end.getTime() - 24 * 3600_000);
    setCustomFrom(toDatetimeLocalValue(start));
    setCustomTo(toDatetimeLocalValue(end));
    setRangePreset('custom');
  };

  const nightRanges = useMemo(() => {
    if (!selectedTimeBounds) return [];
    const byDate = Object.fromEntries((sunSchedule ?? []).map((x) => [x.date, { sunrise: x.sunriseLocal, sunset: x.sunsetLocal }]));
    return buildNightRangesBySchedule(selectedTimeBounds.minMs, selectedTimeBounds.maxMs, byDate, sunriseLocal, sunsetLocal);
  }, [selectedTimeBounds, sunSchedule, sunriseLocal, sunsetLocal]);

  async function saveNawaSettings() {
    if (!id) return;
    setSaveMsg(null);
    const parseOptDecimal = (s: string) => {
      const t = s.trim();
      if (t === '') return null;
      const n = Number(t.replace(',', '.'));
      return Number.isFinite(n) ? n : null;
    };
    const mMin = parseOptDecimal(moistureMin);
    const mMax = parseOptDecimal(moistureMax);
    if (mMin != null && mMax != null && mMin >= mMax) {
      setSaveMsg('Wilgotność „podlej” musi być mniejsza niż „za mokro”.');
      return;
    }
    const tMin = parseOptDecimal(temperatureMin);
    const tMax = parseOptDecimal(temperatureMax);
    if (tMin != null && tMax != null && tMin >= tMax) {
      setSaveMsg('Temperatura min musi być mniejsza niż max.');
      return;
    }
    setSaving(true);
    try {
      await api.updateNawa(id, {
        name: name.trim(),
        description: description.trim() || null,
        plantNote: plantNote.trim() || null,
        isActive,
        moistureMin: mMin,
        moistureMax: mMax,
        temperatureMin: tMin,
        temperatureMax: tMax,
      });
      setSaveMsg('Zapisano.');
      refetchDetail();
    } catch (e) {
      setSaveMsg(e instanceof Error ? e.message : 'Błąd zapisu');
    } finally {
      setSaving(false);
    }
  }

  async function saveWeatherSettings() {
    setWeatherSaveMsg(null);
    const parse = (s: string) => {
      const n = Number(s.replace(',', '.'));
      return Number.isFinite(n) ? n : NaN;
    };
    const rainDetected = parse(rainDetectedMinRaw);
    const highHumidity = parse(highHumidityMinRaw);
    const sunnyMin = parse(sunnyMinRaw);
    const cloudyMax = parse(cloudyMaxRaw);
    if ([rainDetected, highHumidity, sunnyMin, cloudyMax].some((x) => Number.isNaN(x))) {
      setWeatherSaveMsg('Uzupełnij poprawne wartości progów pogodowych.');
      return;
    }
    setWeatherSaving(true);
    try {
      await api.updateWeatherConfig({
        rainDetectedMinRaw: rainDetected,
        highHumidityMinRaw: highHumidity,
        sunnyMinRaw: sunnyMin,
        cloudyMaxRaw: cloudyMax,
        sunriseLocal,
        sunsetLocal,
        manualRainStatus,
        manualLightStatus,
        updatedAtUtc: weatherConfig?.updatedAtUtc ?? new Date().toISOString(),
      });
      setWeatherSaveMsg('Zapisano ustawienia pogody.');
      refetchWeatherConfig();
      refetchWeatherStatus();
      refetchWeather();
    } catch (e) {
      setWeatherSaveMsg(e instanceof Error ? e.message : 'Błąd zapisu ustawień pogody.');
    } finally {
      setWeatherSaving(false);
    }
  }

  if (loading || !detail) {
    return (
      <div className="nawy-page nawy-page--detail">
        <NawyPageBackdrop />
        <div className="nawy-page__inner">
          <button type="button" className="nawa-back-btn" onClick={() => navigate('/nawy')}>
            ← Cofnij
          </button>
          <p className="nawy-page__loading">Ładowanie...</p>
        </div>
      </div>
    );
  }

  const qrUrl = `${window.location.origin}/nawy/${id}`;

  const mMinNum = detail.moistureMin;
  const mMaxNum = detail.moistureMax;
  const tMinNum = detail.temperatureMin;
  const tMaxNum = detail.temperatureMax;

  return (
    <div className="nawy-page nawy-page--detail">
      <NawyPageBackdrop />
      <div className="nawy-page__inner">
      <button type="button" className="nawa-back-btn" onClick={() => navigate('/nawy')}>
        ← Cofnij
      </button>
      <header className="nawa-detail-header">
        <h2 className="nawy-page__title">{detail.name}</h2>
        {detail.plantNote && <p className="nawa-detail-sub">🌿 {detail.plantNote}</p>}
        {detail.description && <p className="nawa-detail-desc">{detail.description}</p>}
      </header>

      {fetchErrors.length > 0 && (
        <div
          role="alert"
          aria-live="polite"
          data-testid="nawa-detail-error-banner"
          style={{
            background: '#fef2f2',
            border: '1px solid #fecaca',
            color: '#991b1b',
            padding: '10px 12px',
            borderRadius: 8,
            marginBottom: 12,
            fontSize: 13,
            lineHeight: 1.45,
          }}
        >
          <strong style={{ display: 'block', marginBottom: 4 }}>Część danych nie wczytała się.</strong>
          <ul style={{ margin: 0, paddingLeft: 18 }}>
            {fetchErrors.map((m) => (
              <li key={m}>{m}</li>
            ))}
          </ul>
          <button
            type="button"
            onClick={() => refetchAll()}
            style={{
              marginTop: 8,
              background: '#991b1b',
              color: '#fff',
              border: 'none',
              borderRadius: 6,
              padding: '6px 10px',
              fontSize: 12,
              cursor: 'pointer',
            }}
          >
            Spróbuj ponownie
          </button>
        </div>
      )}

      <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap', marginBottom: 16 }}>
        <InfoCard
          label="Progi wilgotności (aktualne w bazie)"
          value={
            detail.moistureMin !== null && detail.moistureMax !== null
              ? `Podlej ≤ ${detail.moistureMin}% · Za mokro ≥ ${detail.moistureMax}%`
              : 'Nie ustawione'
          }
        />
        <InfoCard label="Sensory" value={String(detail.sensors.length)} />
      </div>

      <div style={{ marginBottom: 16 }}>
        <button
          type="button"
          className="btn-primary"
          disabled={voiceBriefLoading || !id}
          style={{ width: '100%', padding: '12px 16px', fontSize: 15, fontWeight: 600 }}
          onClick={async () => {
            if (!id) return;
            setVoiceBriefError(null);
            setVoiceBriefLoading(true);
            try {
              const brief = await api.getNawaVoiceBrief(id);
              speakPolish(brief.spokenText);
            } catch (e) {
              setVoiceBriefError(e instanceof Error ? e.message : 'Błąd pobierania podsumowania');
            } finally {
              setVoiceBriefLoading(false);
            }
          }}
        >
          {voiceBriefLoading ? 'Ładowanie…' : 'Odczytaj stan nawy (głos, szczegóły)'}
        </button>
        <p className="nawa-voice-hint" style={{ fontSize: 11, margin: '6px 0 0', lineHeight: 1.35 }}>
          Krótki opis po polsku: status wilgotności, progi, ewentualna anomalia temperatury oraz — gdy da się to oszacować z historii —{' '}
          <strong>od kiedy</strong> utrzymuje się alarm (okno ok. 72 godzin).
        </p>
        {voiceBriefError && (
          <p style={{ color: '#d32f2f', fontSize: 13, marginTop: 6 }} role="alert">
            {voiceBriefError}
          </p>
        )}
      </div>

      <section
        className="nawa-glass"
        style={{
          borderRadius: 12,
          padding: 16,
          marginBottom: 16,
        }}
      >
        <h3 style={{ fontSize: 15, marginBottom: 10 }}>Ustawienia nawy i progi</h3>
        <p style={{ fontSize: 12, color: '#64748b', marginBottom: 12 }}>
          <strong>Wilgotność:</strong> „Podlej” — gdy gleba jest sucha (poniżej progu). „Za mokro” — gdy za dużo wody. Na
          wykresie widać te poziomy jako linie.
        </p>
        <div style={{ display: 'grid', gap: 10, maxWidth: 480 }}>
          <label style={lbl}>
            Nazwa nawy
            <input style={inp} value={name} onChange={(e) => setName(e.target.value)} />
          </label>
          <label style={lbl}>
            Opis (opcjonalnie)
            <input style={inp} value={description} onChange={(e) => setDescription(e.target.value)} />
          </label>
          <label style={lbl}>
            Notatka o roślinach
            <input style={inp} value={plantNote} onChange={(e) => setPlantNote(e.target.value)} />
          </label>
          <label style={{ ...lbl, flexDirection: 'row', alignItems: 'center', gap: 8 }}>
            <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
            Nawa aktywna
          </label>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
            <label style={lbl}>
              Podlej przy wilgotności ≤ (%)
              <input
                style={inp}
                inputMode="decimal"
                placeholder="np. 30"
                value={moistureMin}
                onChange={(e) => setMoistureMin(e.target.value)}
              />
            </label>
            <label style={lbl}>
              Za mokro przy wilgotności ≥ (%)
              <input
                style={inp}
                inputMode="decimal"
                placeholder="np. 70"
                value={moistureMax}
                onChange={(e) => setMoistureMax(e.target.value)}
              />
            </label>
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
            <label style={lbl}>
              Temp. alert min (°C, opcj.)
              <input
                style={inp}
                inputMode="decimal"
                value={temperatureMin}
                onChange={(e) => setTemperatureMin(e.target.value)}
              />
            </label>
            <label style={lbl}>
              Temp. alert max (°C, opcj.)
              <input
                style={inp}
                inputMode="decimal"
                value={temperatureMax}
                onChange={(e) => setTemperatureMax(e.target.value)}
              />
            </label>
          </div>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
            <button type="button" style={btnPrimary} disabled={saving} onClick={() => void saveNawaSettings()}>
              {saving ? 'Zapisywanie…' : 'Zapisz ustawienia'}
            </button>
            {saveMsg && <span style={{ fontSize: 13, color: saveMsg.startsWith('Zapisano') ? '#15803d' : '#b91c1c' }}>{saveMsg}</span>}
          </div>
        </div>
      </section>

      <section
        style={{
          background: '#fff',
          borderRadius: 12,
          padding: 16,
          marginBottom: 16,
          boxShadow: '0 1px 3px rgba(0,0,0,.08)',
        }}
      >
        <h3 style={{ fontSize: 15, marginBottom: 10 }}>Zakres czasu wykresów</h3>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, marginBottom: 12 }}>
          {(Object.keys(PRESET_HOURS) as Exclude<RangePreset, 'custom'>[]).map((key) => (
            <button
              key={key}
              type="button"
              onClick={() => setRangePreset(key)}
              style={rangePreset === key ? btnPrimary : btnGhost}
            >
              {key === '6h' && '6 h'}
              {key === '1h' && '1 h'}
              {key === '24h' && '24 h'}
              {key === '7d' && '7 dni'}
              {key === '30d' && '30 dni'}
            </button>
          ))}
          <button type="button" onClick={startCustomRange} style={rangePreset === 'custom' ? btnPrimary : btnGhost}>
            Własny zakres
          </button>
        </div>
        {rangePreset === 'custom' && (
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10, maxWidth: 420 }}>
            <label style={lbl}>
              Od (lokalnie)
              <input type="datetime-local" style={inp} value={customFrom} onChange={(e) => setCustomFrom(e.target.value)} />
            </label>
            <label style={lbl}>
              Do (lokalnie)
              <input type="datetime-local" style={inp} value={customTo} onChange={(e) => setCustomTo(e.target.value)} />
            </label>
          </div>
        )}
        <p style={{ fontSize: 11, color: '#94a3b8', marginTop: 8 }}>
          Wykresy, podlania i tempo wysychania używają tego samego zakresu.
        </p>
      </section>

      <div className="nawa-glass nawa-chart-shell">
        <MoistureChart
          points={points ?? []}
          sensorLegendById={sensorLegendById}
          rangeMs={selectedRangeMs}
          timeBounds={selectedTimeBounds}
          wateringEvents={wateringEvents ?? []}
          title="Wilgotność gleby"
          moistureMin={mMinNum}
          moistureMax={mMaxNum}
        />
      </div>
      <div className="nawa-glass nawa-chart-shell">
        <TemperatureChart
          points={points ?? []}
          sensorLegendById={sensorLegendById}
          rangeMs={selectedRangeMs}
          timeBounds={selectedTimeBounds}
          temperatureMin={tMinNum}
          temperatureMax={tMaxNum}
        />
      </div>
      <div className="nawa-glass nawa-chart-shell">
        <BatteryChart
          points={batteryPointsAllSensors}
          sensorLegendById={sensorLegendById}
          rangeMs={selectedRangeMs}
          timeBounds={selectedTimeBounds}
        />
      </div>
      <div className="nawa-glass nawa-chart-shell">
        <h3 style={{ fontSize: 14, marginBottom: 10 }}>Serie pogodowe</h3>
        {weatherCurrentStatus && (
          <p style={{ fontSize: 12, color: '#475569', marginBottom: 10 }}>
            Aktualny status: opad <strong>{rainStatusLabel(weatherCurrentStatus.rainStatus)}</strong>, nasłonecznienie{' '}
            <strong>{lightStatusLabel(weatherCurrentStatus.lightStatus)}</strong>, pora <strong>{weatherCurrentStatus.isNightBySchedule ? 'noc' : 'dzień'}</strong>.
            {weatherCurrentStatus.rainIntensityRaw != null && (
              <>
                {' '}
                Opad surowy: <strong>{weatherCurrentStatus.rainIntensityRaw}</strong>.
              </>
            )}
            {weatherCurrentStatus.illuminanceRaw != null && (
              <>
                {' '}
                Jasność surowa: <strong>{weatherCurrentStatus.illuminanceRaw}</strong>.
              </>
            )}
          </p>
        )}
        <section className="nawa-glass" style={{ borderRadius: 10, padding: 12, marginBottom: 12 }}>
          <h4 style={{ fontSize: 13, marginBottom: 8 }}>Globalne ustawienia pogody</h4>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 10 }}>
            <label style={lbl}>
              Opad od (raw)
              <input style={inp} inputMode="decimal" value={rainDetectedMinRaw} onChange={(e) => setRainDetectedMinRaw(e.target.value)} />
            </label>
            <label style={lbl}>
              Duża wilgotność od (raw)
              <input style={inp} inputMode="decimal" value={highHumidityMinRaw} onChange={(e) => setHighHumidityMinRaw(e.target.value)} />
            </label>
            <label style={lbl}>
              Słonecznie od (raw)
              <input style={inp} inputMode="decimal" value={sunnyMinRaw} onChange={(e) => setSunnyMinRaw(e.target.value)} />
            </label>
            <label style={lbl}>
              Zachmurzenie do (raw)
              <input style={inp} inputMode="decimal" value={cloudyMaxRaw} onChange={(e) => setCloudyMaxRaw(e.target.value)} />
            </label>
            <label style={lbl}>
              Wschód słońca (HH:mm)
              <input style={inp} value={sunriseLocal} onChange={(e) => setSunriseLocal(e.target.value)} />
            </label>
            <label style={lbl}>
              Zachód słońca (HH:mm)
              <input style={inp} value={sunsetLocal} onChange={(e) => setSunsetLocal(e.target.value)} />
            </label>
            <label style={lbl}>
              Ręczny status opadu
              <select style={inp} value={manualRainStatus} onChange={(e) => setManualRainStatus(e.target.value as ManualRainStatus)}>
                <option value="auto">Auto</option>
                <option value="raining">Aktualnie pada</option>
                <option value="no-rain">Aktualnie nie pada</option>
                <option value="high-humidity">Aktualnie duża wilgotność</option>
              </select>
            </label>
            <label style={lbl}>
              Ręczny status nasłonecznienia
              <select style={inp} value={manualLightStatus} onChange={(e) => setManualLightStatus(e.target.value as ManualLightStatus)}>
                <option value="auto">Auto</option>
                <option value="sunny">Jest słonecznie</option>
                <option value="cloudy">Jest zachmurzenie</option>
                <option value="night">Jest noc</option>
              </select>
            </label>
          </div>
          <div style={{ marginTop: 10, display: 'flex', gap: 8, alignItems: 'center' }}>
            <button type="button" style={btnPrimary} disabled={weatherSaving} onClick={() => void saveWeatherSettings()}>
              {weatherSaving ? 'Zapisywanie…' : 'Zapisz ustawienia pogody'}
            </button>
            {weatherSaveMsg && <span style={{ fontSize: 12, color: weatherSaveMsg.startsWith('Zapisano') ? '#15803d' : '#b91c1c' }}>{weatherSaveMsg}</span>}
          </div>
          <div style={{ marginTop: 10 }}>
            <label style={{ ...lbl, fontSize: 12 }}>
              Import CSV wschód/zachód (date,sunrise_local,sunset_local)
              <input
                type="file"
                accept=".csv,text/csv"
                style={inp}
                onChange={async (e) => {
                  const file = e.currentTarget.files?.[0];
                  if (!file) return;
                  setSunCsvImportMsg(null);
                  try {
                    const text = await file.text();
                    const result = await api.importSunScheduleCsv(text);
                    setSunCsvImportMsg(`Zaimportowano: ${result.importedRows}, pominięto: ${result.ignoredRows}.`);
                    refetchSunSchedule();
                  } catch (err) {
                    setSunCsvImportMsg(err instanceof Error ? err.message : 'Błąd importu CSV.');
                  } finally {
                    e.currentTarget.value = '';
                  }
                }}
              />
            </label>
            {sunCsvImportMsg && <p style={{ marginTop: 6, fontSize: 12 }}>{sunCsvImportMsg}</p>}
          </div>
        </section>
        <WeatherChart
          points={weatherPoints ?? []}
          selectedMetrics={RAIN_METRICS}
          sensorLegendById={sensorLegendById}
          rangeMs={selectedRangeMs}
          timeBounds={selectedTimeBounds}
          title="Opad"
          nightRangesMs={nightRanges}
        />
        <div style={{ height: 12 }} />
        <WeatherChart
          points={weatherPoints ?? []}
          selectedMetrics={LIGHT_METRICS}
          sensorLegendById={sensorLegendById}
          rangeMs={selectedRangeMs}
          timeBounds={selectedTimeBounds}
          title="Nasłonecznienie"
          nightRangesMs={nightRanges}
        />
      </div>

      {dryingRates && dryingRates.length > 0 && (
        <div style={{ background: '#fff', borderRadius: 12, padding: 16, marginTop: 12, boxShadow: '0 1px 3px rgba(0,0,0,.08)' }}>
          <h3 style={{ fontSize: 14, marginBottom: 8 }}>Tempo wysychania</h3>
          {dryingRates.map((r) => (
            <div
              key={`${r.sensorId ?? r.sensorIdentifier}__${r.windowStart}__${r.windowEnd}`}
              style={{ fontSize: 13, color: '#475569' }}
            >
              {r.sensorIdentifier}: <strong>{r.percentPerHour} %/h</strong>
            </div>
          ))}
        </div>
      )}

      {wateringEvents && wateringEvents.length > 0 && (
        <div className="nawa-glass" style={{ borderRadius: 12, padding: 16, marginTop: 12 }}>
          <h3 style={{ fontSize: 14, marginBottom: 8 }}>Skoki wilgotności (podlanie / deszcz?)</h3>
          {wateringEvents.map((e) => {
            const kindLabel =
              e.inferredKind === 'likelyRain'
                ? 'Deszcz?'
                : e.inferredKind === 'likelyManual'
                  ? 'Podlanie'
                  : 'Nieznane';
            const kindColor =
              e.inferredKind === 'likelyRain' ? '#0369a1' : e.inferredKind === 'likelyManual' ? '#15803d' : '#b45309';
            return (
              <div
                key={keyFromTimestampAndLabel(e.detectedAtUtc, `watering-${e.inferredKind}`)}
                style={{ fontSize: 13, color: '#475569', marginBottom: 6 }}
              >
                <span style={{ fontWeight: 600, color: kindColor }}>{kindLabel}</span>
                {' · '}
                {formatDateTimeFullPl(e.detectedAtUtc)}
                {' · '}
                {e.moistureBefore}% → {e.moistureAfter}% (+{e.deltaMoisture}%)
                {e.contributingSensorCount > 1 && (
                  <span style={{ fontSize: 11, color: '#94a3b8' }}> · {e.contributingSensorCount} czujniki</span>
                )}
              </div>
            );
          })}
        </div>
      )}

      <details style={{ marginTop: 16 }}>
        <summary style={{ cursor: 'pointer', fontSize: 13, color: '#64748b' }}>QR kod nawy</summary>
        <div style={{ padding: 16, textAlign: 'center' }}>
          <QRCodeSVG value={qrUrl} size={160} />
          <p style={{ fontSize: 11, color: '#94a3b8', marginTop: 4 }}>{qrUrl}</p>
        </div>
      </details>

      <h3 style={{ fontSize: 15, marginTop: 16 }}>Czujniki ({detail.sensors.length})</h3>
      {detail.sensors.length === 0 && <p style={{ color: '#9ca3af', fontSize: 13 }}>Brak przypisanych czujników</p>}
      {detail.sensors.map((s) => (
        <div
          key={s.id}
          style={{
            background: '#fff',
            borderRadius: 8,
            padding: 12,
            marginBottom: 8,
            boxShadow: '0 1px 2px rgba(0,0,0,.05)',
          }}
        >
          <div style={{ fontWeight: 600, fontSize: 14 }}>{s.displayName ?? s.externalId}</div>
          {s.displayName && <div style={{ fontSize: 11, color: '#94a3b8' }}>{s.externalId}</div>}
        </div>
      ))}
      </div>
    </div>
  );
}

const lbl: CSSProperties = { display: 'flex', flexDirection: 'column', gap: 4, fontSize: 12, color: '#475569' };
const inp: CSSProperties = {
  padding: '8px 10px',
  borderRadius: 8,
  border: '1px solid #e2e8f0',
  fontSize: 14,
};
const btnPrimary: React.CSSProperties = {
  background: 'var(--color-brand-green, #16a34a)',
  color: '#fff',
  border: 'none',
  borderRadius: 8,
  padding: '8px 16px',
  fontSize: 14,
  cursor: 'pointer',
};
const btnGhost: CSSProperties = {
  background: '#f1f5f9',
  color: '#334155',
  border: '1px solid #e2e8f0',
  borderRadius: 8,
  padding: '8px 12px',
  fontSize: 13,
  cursor: 'pointer',
};

function InfoCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="nawa-glass" style={{ borderRadius: 8, padding: '8px 12px' }}>
      <div style={{ fontSize: 11, color: '#94a3b8' }}>{label}</div>
      <div style={{ fontSize: 14, fontWeight: 600 }}>{value}</div>
    </div>
  );
}

function rainStatusLabel(v: string): string {
  switch (v) {
    case 'raining':
      return 'aktualnie pada';
    case 'no-rain':
      return 'aktualnie nie pada';
    case 'high-humidity':
      return 'aktualnie duża wilgotność';
    case 'auto':
      return 'auto';
    default:
      return 'nieznane';
  }
}

function lightStatusLabel(v: string): string {
  switch (v) {
    case 'sunny':
      return 'jest słonecznie';
    case 'cloudy':
      return 'jest zachmurzenie';
    case 'night':
      return 'noc';
    case 'auto':
      return 'auto';
    default:
      return 'nieznane';
  }
}

function parseTime(v: string): { h: number; m: number } | null {
  const m = /^(\d{1,2}):(\d{2})$/.exec(v.trim());
  if (!m) return null;
  const h = Number(m[1]);
  const mm = Number(m[2]);
  if (!Number.isInteger(h) || !Number.isInteger(mm) || h < 0 || h > 23 || mm < 0 || mm > 59) return null;
  return { h, m: mm };
}

function buildNightRangesBySchedule(
  fromMs: number,
  toMs: number,
  byDate: Record<string, { sunrise: string; sunset: string }>,
  fallbackSunrise: string,
  fallbackSunset: string,
): Array<{ fromMs: number; toMs: number }> {
  const out: Array<{ fromMs: number; toMs: number }> = [];
  const d0 = new Date(fromMs);
  d0.setHours(0, 0, 0, 0);
  for (let day = new Date(d0.getTime()); day.getTime() <= toMs + 24 * 3600_000; day = new Date(day.getTime() + 24 * 3600_000)) {
    const key = `${day.getFullYear()}-${String(day.getMonth() + 1).padStart(2, '0')}-${String(day.getDate()).padStart(2, '0')}`;
    const src = byDate[key];
    const sr = parseTime(src?.sunrise ?? fallbackSunrise);
    const ss = parseTime(src?.sunset ?? fallbackSunset);
    if (!sr || !ss) continue;
    const sunrise = sr;
    const sunset = ss;
    const sunriseMs = new Date(day.getFullYear(), day.getMonth(), day.getDate(), sunrise.h, sunrise.m, 0, 0).getTime();
    const sunsetMs = new Date(day.getFullYear(), day.getMonth(), day.getDate(), sunset.h, sunset.m, 0, 0).getTime();
    const nextSunriseMs = sunriseMs + 24 * 3600_000;
    if (sunsetMs >= sunriseMs) {
      out.push({ fromMs: Math.max(fromMs, day.getTime()), toMs: Math.min(toMs, sunriseMs) });
      out.push({ fromMs: Math.max(fromMs, sunsetMs), toMs: Math.min(toMs, nextSunriseMs) });
    } else {
      out.push({ fromMs: Math.max(fromMs, sunsetMs), toMs: Math.min(toMs, sunriseMs) });
    }
  }
  return out.filter((r) => r.toMs > r.fromMs);
}
