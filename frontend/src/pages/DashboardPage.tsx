import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, type NawaSnapshot } from '../api/client';
import { useFetch } from '../hooks/useFetch';
import { speakPolish, useTts } from '../hooks/useTts';
import { buildVoiceDailyReportText } from '../voice/voiceDailyReportText';
import { StatusBadge } from '../components/StatusBadge';
import { BatteryIcon } from '../components/BatteryIcon';
import { DashboardHero } from '../components/DashboardHero';
import { DashboardPageBackdrop } from '../components/DashboardPageBackdrop';

export function DashboardPage() {
  const { data, loading, refetch } = useFetch(() => api.getDashboard());
  const navigate = useNavigate();
  const tts = useTts();
  const [voiceReportLoading, setVoiceReportLoading] = useState(false);
  const [voiceReportError, setVoiceReportError] = useState<string | null>(null);

  useEffect(() => {
    const interval = setInterval(refetch, 30_000);
    return () => clearInterval(interval);
  }, [refetch]);

  useEffect(() => {
    if (!data) return;
    const dry = data.filter((s) => s.status === 2);
    if (dry.length > 0) {
      const intro =
        dry.length === 1 ? 'Uwaga, potrzebne podlanie w nawie.' : 'Uwaga, potrzebne podlanie w kilku nawach.';
      const parts = dry.map((s) => {
        const note = s.wateringSpeechNote?.trim();
        return note ? `${s.nawaName}. ${note}` : s.nawaName;
      });
      tts.speak(`${intro} ${parts.join(' Następna nawa: ')}`);
    }
    const conflict = data.filter((s) => s.status === 4);
    if (conflict.length > 0) {
      const parts = conflict.map((s) => {
        const note = s.wateringSpeechNote?.trim();
        return note ? `${s.nawaName}. ${note}` : s.nawaName;
      });
      tts.speak(
        `Uwaga, sprzeczne odczyty wilgotności w ${conflict.length} nawach: ${parts.join(' Następna nawa: ')}. Sprawdź czujniki.`,
      );
    }
    const uneven = data.filter((s) => s.status === 5);
    if (uneven.length > 0) {
      tts.speak(
        `Informacja: duży rozstrzał między czujnikami w: ${uneven.map((s) => s.nawaName).join(', ')}.`,
      );
    }
  }, [data, tts]);

  if (loading) {
    return (
      <div className="dashboard-page">
        <DashboardPageBackdrop />
        <div className="dashboard-page__inner">
          <p className="dashboard-page__loading">Ładowanie...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="dashboard-page">
      <DashboardPageBackdrop />
      <div className="dashboard-page__inner">
      <DashboardHero />

      <div className="dashboard-toolbar" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 12, gap: 12 }}>
        <h2 style={{ margin: 0 }}>Pulpit</h2>
        <div style={{ textAlign: 'right', maxWidth: 220 }}>
          <label style={{ fontSize: 12, display: 'flex', alignItems: 'center', gap: 6, justifyContent: 'flex-end' }} className="text-muted">
            <input
              type="checkbox"
              checked={tts.enabled}
              onChange={(e) => {
                const on = e.target.checked;
                tts.setEnabled(on);
                if (on) {
                  tts.speakImmediate(
                    'Głos włączony. Usłyszysz ostrzeżenie, gdy nawa będzie w stanie sucho.',
                  );
                }
              }}
            />
            Głos
          </label>
          <p style={{ fontSize: 10, color: '#94a3b8', margin: '4px 0 0', lineHeight: 1.35 }}>
            Komunikat tylko przy statusie <strong>Sucho</strong> (nie przy każdym odświeżeniu). Na iPhonie część przeglądarek wymaga włączenia dźwięku i nie blokuje trybu cichego.
          </p>
        </div>
      </div>

      <div
        style={{
          background: 'var(--card-bg, #f8fafc)',
          borderRadius: 10,
          padding: '12px 14px',
          marginBottom: 14,
          border: '1px solid rgba(0,0,0,.06)',
        }}
      >
        <p style={{ fontSize: 12, margin: 0, color: '#64748b', lineHeight: 1.5 }}>
          <strong>Zakresy i statusy:</strong> Kolor statusu wynika z progów wilgotności zapisanych w nawie:{' '}
          <em>Sucho</em>, gdy najsuchszy czujnik jest na lub poniżej progu „podlej”; <em>Za mokro</em>, gdy najmokrzejszy — na lub powyżej progu „za mokro” przez co najmniej pół godziny; <em>Po podlaniu</em> — krótsze przemoczenie (poniżej 30 min).{' '}
          <em>Rozstrzał</em> i <em>sprzeczne czujniki</em> to osobne sytuacje przy większej liczbie punktów pomiarowych. Na karcie widać średnią temperaturę z ostatnich odczytów; progi alertu °C są w ustawieniach nawy i na dole karty (jeśli ustawione).
        </p>
      </div>

      <div className="dashboard-page__voice" style={{ marginBottom: 16 }}>
        <button
          type="button"
          className="btn-primary"
          disabled={voiceReportLoading}
          style={{ width: '100%', padding: '12px 16px', fontSize: 15, fontWeight: 600 }}
          onClick={async () => {
            setVoiceReportError(null);
            setVoiceReportLoading(true);
            try {
              const report = await api.getVoiceDailyReport();
              speakPolish(buildVoiceDailyReportText(report));
            } catch (e) {
              setVoiceReportError(e instanceof Error ? e.message : 'Błąd pobierania raportu');
            } finally {
              setVoiceReportLoading(false);
            }
          }}
        >
          {voiceReportLoading ? 'Ładowanie raportu…' : 'Odczytaj dzienny raport (głos)'}
        </button>
        <p className="text-muted" style={{ fontSize: 11, margin: '6px 0 0', lineHeight: 1.35 }}>
          Dane z maliny (średnie od lokalnej północy, strefa z konfiguracji API). Wymaga działającej syntezy mowy w
          przeglądarce.
        </p>
        {voiceReportError && (
          <p style={{ color: '#d32f2f', fontSize: 13, marginTop: 6 }} role="alert">
            {voiceReportError}
          </p>
        )}
      </div>

      {(!data || data.length === 0) && <p className="text-muted">Brak naw. Dodaj pierwszą nawę.</p>}

      {data && data.length > 0 && data.every((s) => s.sensorCount === 0) && (
        <p className="text-muted" style={{ marginBottom: 12, fontSize: 14 }}>
          Nawy są utworzone, ale <strong>żaden czujnik nie jest przypisany</strong>. Otwórz{' '}
          <Link to="/sensory">Sensory</Link> i wybierz nawę przy każdym czujniku.
        </p>
      )}

      <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
        {data?.map((snap) => (
          <NawaCard key={snap.nawaId} snap={snap} onClick={() => navigate(`/nawy/${snap.nawaId}`)} />
        ))}
      </div>
      </div>
    </div>
  );
}

function formatSnapshotRanges(snap: NawaSnapshot): string | null {
  const m: string[] = [];
  if (snap.moistureMin != null) m.push(`podlej ≤ ${snap.moistureMin}%`);
  if (snap.moistureMax != null) m.push(`za mokro ≥ ${snap.moistureMax}%`);
  const t: string[] = [];
  if (snap.temperatureMin != null) t.push(`temp. min ${snap.temperatureMin}°C`);
  if (snap.temperatureMax != null) t.push(`temp. max ${snap.temperatureMax}°C`);
  if (m.length === 0 && t.length === 0) return null;
  return [...m, ...t].join(' · ');
}

function NawaCard({ snap, onClick }: { snap: NawaSnapshot; onClick: () => void }) {
  const ranges = formatSnapshotRanges(snap);
  return (
    <div role="button" tabIndex={0} className="nawa-card" onClick={onClick} onKeyDown={(e) => e.key === 'Enter' && onClick()}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <div className="nawa-card__title">{snap.nawaName}</div>
          {snap.plantNote && <div style={{ fontSize: 12 }} className="text-muted">{snap.plantNote}</div>}
        </div>
        <StatusBadge status={snap.status} />
      </div>
      {ranges && (
        <div style={{ fontSize: 11, color: '#64748b', marginTop: 8, lineHeight: 1.35 }} title="Progi z ustawień nawy">
          <strong>Zakresy:</strong> {ranges}
        </div>
      )}

      <div className="nawa-card__meta">
        <div>
          💧 {snap.avgMoisture !== null ? `${snap.avgMoisture}%` : '—'}
          {snap.minMoisture !== null && snap.maxMoisture !== null && (
            <span style={{ fontSize: 11, opacity: 0.85 }} title="min–max (najsuchszy / najmokrzejszy punkt)">
              {' '}
              ({snap.minMoisture}–{snap.maxMoisture}
              {snap.moistureSpread != null && snap.sensorCount > 1 ? `, Δ${snap.moistureSpread}` : ''})
            </span>
          )}
          {snap.sensorCount > 0 && snap.moistureReadingCount < snap.sensorCount && (
            <span style={{ fontSize: 10, color: '#b45309', marginLeft: 4 }} title="Część czujników bez odczytu wilgotności">
              ({snap.moistureReadingCount}/{snap.sensorCount} z wilg.)
            </span>
          )}
        </div>
        <div>🌡️ {snap.avgTemperature !== null ? `${snap.avgTemperature}°C` : '—'}</div>
        <BatteryIcon level={snap.lowestBattery} />
        <div style={{ fontSize: 11, opacity: 0.85 }}>📡 {snap.sensorCount}</div>
      </div>
    </div>
  );
}
