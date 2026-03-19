import { api } from '../api/client';
import { useFetch } from '../hooks/useFetch';
import { BatteryIcon } from '../components/BatteryIcon';

export function SensorsPage() {
  const { data, loading } = useFetch(() => api.getSensorHealth());

  if (loading) return <p>Ładowanie...</p>;

  return (
    <div>
      <h2 style={{ fontSize: 20, marginBottom: 12 }}>Sensory</h2>

      {(!data || data.length === 0) && <p style={{ color: '#9ca3af' }}>Brak zarejestrowanych czujników</p>}

      {data?.map((s) => (
        <div
          key={s.sensorId}
          style={{
            background: '#fff',
            borderRadius: 12,
            padding: 14,
            marginBottom: 8,
            boxShadow: '0 1px 3px rgba(0,0,0,.08)',
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div>
              <div style={{ fontWeight: 700, fontSize: 14 }}>{s.displayName ?? s.externalId}</div>
              {s.displayName && <div style={{ fontSize: 11, color: '#94a3b8' }}>{s.externalId}</div>}
            </div>
            <BatteryIcon level={s.battery} />
          </div>

          <div style={{ display: 'flex', gap: 12, marginTop: 8, fontSize: 12, color: '#64748b' }}>
            <span>📶 LQ: {s.linkQuality ?? '—'}</span>
            <span>📊 24h: {s.totalReadings24h}</span>
            <span>
              ⏱️{' '}
              {s.lastReadingUtc
                ? new Date(s.lastReadingUtc).toLocaleString('pl-PL', { hour: '2-digit', minute: '2-digit' })
                : '—'}
            </span>
          </div>
        </div>
      ))}
    </div>
  );
}
