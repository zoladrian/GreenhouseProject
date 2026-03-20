import { useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, type NawaSnapshot } from '../api/client';
import { useFetch } from '../hooks/useFetch';
import { useTts } from '../hooks/useTts';
import { StatusBadge } from '../components/StatusBadge';
import { BatteryIcon } from '../components/BatteryIcon';
import { DashboardHero } from '../components/DashboardHero';

export function DashboardPage() {
  const { data, loading, refetch } = useFetch(() => api.getDashboard());
  const navigate = useNavigate();
  const tts = useTts();

  useEffect(() => {
    const interval = setInterval(refetch, 30_000);
    return () => clearInterval(interval);
  }, [refetch]);

  useEffect(() => {
    if (!data) return;
    const dry = data.filter((s) => s.status === 2);
    if (dry.length > 0) {
      tts.speak(`Uwaga! ${dry.length} naw wymaga podlania: ${dry.map((s) => s.nawaName).join(', ')}`);
    }
  }, [data, tts]);

  if (loading) return <p className="text-muted">Ładowanie...</p>;

  return (
    <div>
      <DashboardHero />

      <div className="dashboard-toolbar" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
        <h2>Dashboard</h2>
        <label style={{ fontSize: 12, display: 'flex', alignItems: 'center', gap: 4 }} className="text-muted">
          <input type="checkbox" checked={tts.enabled} onChange={(e) => tts.setEnabled(e.target.checked)} />
          Głos
        </label>
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
  );
}

function NawaCard({ snap, onClick }: { snap: NawaSnapshot; onClick: () => void }) {
  return (
    <div role="button" tabIndex={0} className="nawa-card" onClick={onClick} onKeyDown={(e) => e.key === 'Enter' && onClick()}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <div className="nawa-card__title">{snap.nawaName}</div>
          {snap.plantNote && <div style={{ fontSize: 12 }} className="text-muted">{snap.plantNote}</div>}
        </div>
        <StatusBadge status={snap.status} />
      </div>

      <div className="nawa-card__meta">
        <div>
          💧 {snap.avgMoisture !== null ? `${snap.avgMoisture}%` : '—'}
          {snap.minMoisture !== null && snap.maxMoisture !== null && (
            <span style={{ fontSize: 11, opacity: 0.85 }}> ({snap.minMoisture}–{snap.maxMoisture})</span>
          )}
        </div>
        <div>🌡️ {snap.avgTemperature !== null ? `${snap.avgTemperature}°C` : '—'}</div>
        <BatteryIcon level={snap.lowestBattery} />
        <div style={{ fontSize: 11, opacity: 0.85 }}>📡 {snap.sensorCount}</div>
      </div>
    </div>
  );
}
