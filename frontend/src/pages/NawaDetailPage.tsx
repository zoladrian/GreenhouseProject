import { useParams } from 'react-router-dom';
import { api } from '../api/client';
import { useFetch } from '../hooks/useFetch';
import { MoistureChart } from '../components/MoistureChart';
import { TemperatureChart } from '../components/TemperatureChart';
import { BatteryChart } from '../components/BatteryChart';
import { useEffect, useMemo, useState, type CSSProperties } from 'react';
import { QRCodeSVG } from 'qrcode.react';

type RangePreset = '6h' | '24h' | '48h' | '7d' | '30d' | 'custom';

const PRESET_HOURS: Record<Exclude<RangePreset, 'custom'>, number> = {
  '6h': 6,
  '24h': 24,
  '48h': 48,
  '7d': 168,
  '30d': 720,
};

function toDatetimeLocalValue(d: Date) {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export function NawaDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: detail, loading, refetch } = useFetch(() => api.getNawaDetail(id!), [id]);

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

  const { data: points } = useFetch(() => api.getMoistureSeries(`nawaId=${id}&from=${from}&to=${to}`), [id, from, to]);
  const { data: wateringEvents } = useFetch(() => api.getWateringEvents(id!, from, to), [id, from, to]);
  const { data: dryingRates } = useFetch(() => api.getDryingRates(id!, from, to), [id, from, to]);

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

  const startCustomRange = () => {
    const end = new Date();
    const start = new Date(end.getTime() - 24 * 3600_000);
    setCustomFrom(toDatetimeLocalValue(start));
    setCustomTo(toDatetimeLocalValue(end));
    setRangePreset('custom');
  };

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
      await refetch();
    } catch (e) {
      setSaveMsg(e instanceof Error ? e.message : 'Błąd zapisu');
    } finally {
      setSaving(false);
    }
  }

  if (loading || !detail) return <p>Ładowanie...</p>;

  const qrUrl = `${window.location.origin}/nawy/${id}`;

  const mMinNum = detail.moistureMin;
  const mMaxNum = detail.moistureMax;
  const tMinNum = detail.temperatureMin;
  const tMaxNum = detail.temperatureMax;

  return (
    <div>
      <h2 style={{ fontSize: 20, marginBottom: 4 }}>{detail.name}</h2>
      {detail.plantNote && <p style={{ color: '#64748b', margin: '0 0 8px', fontSize: 14 }}>🌿 {detail.plantNote}</p>}
      {detail.description && <p style={{ color: '#94a3b8', margin: '0 0 12px', fontSize: 13 }}>{detail.description}</p>}

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

      <section
        style={{
          background: '#fff',
          borderRadius: 12,
          padding: 16,
          marginBottom: 16,
          boxShadow: '0 1px 3px rgba(0,0,0,.08)',
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
              {key === '24h' && '24 h'}
              {key === '48h' && '48 h'}
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

      <MoistureChart
        points={points ?? []}
        wateringEvents={wateringEvents ?? []}
        title="Wilgotność gleby"
        moistureMin={mMinNum}
        moistureMax={mMaxNum}
      />
      <TemperatureChart points={points ?? []} temperatureMin={tMinNum} temperatureMax={tMaxNum} />
      <BatteryChart points={points ?? []} />

      {dryingRates && dryingRates.length > 0 && (
        <div style={{ background: '#fff', borderRadius: 12, padding: 16, marginTop: 12, boxShadow: '0 1px 3px rgba(0,0,0,.08)' }}>
          <h3 style={{ fontSize: 14, marginBottom: 8 }}>Tempo wysychania</h3>
          {dryingRates.map((r, i) => (
            <div key={i} style={{ fontSize: 13, color: '#475569' }}>
              {r.sensorIdentifier}: <strong>{r.percentPerHour} %/h</strong>
            </div>
          ))}
        </div>
      )}

      {wateringEvents && wateringEvents.length > 0 && (
        <div style={{ background: '#fff', borderRadius: 12, padding: 16, marginTop: 12, boxShadow: '0 1px 3px rgba(0,0,0,.08)' }}>
          <h3 style={{ fontSize: 14, marginBottom: 8 }}>Skoki wilgotności (podlanie / deszcz?)</h3>
          {wateringEvents.map((e, i) => {
            const kindLabel =
              e.inferredKind === 'likelyRain'
                ? 'Deszcz?'
                : e.inferredKind === 'likelyManual'
                  ? 'Podlanie'
                  : 'Nieznane';
            const kindColor =
              e.inferredKind === 'likelyRain' ? '#0369a1' : e.inferredKind === 'likelyManual' ? '#15803d' : '#b45309';
            return (
              <div key={i} style={{ fontSize: 13, color: '#475569', marginBottom: 6 }}>
                <span style={{ fontWeight: 600, color: kindColor }}>{kindLabel}</span>
                {' · '}
                {new Date(e.detectedAtUtc).toLocaleString('pl-PL')}
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
    <div style={{ background: '#fff', borderRadius: 8, padding: '8px 12px', boxShadow: '0 1px 2px rgba(0,0,0,.05)' }}>
      <div style={{ fontSize: 11, color: '#94a3b8' }}>{label}</div>
      <div style={{ fontSize: 14, fontWeight: 600 }}>{value}</div>
    </div>
  );
}
