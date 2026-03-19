import { api } from '../api/client';
import { useFetch } from '../hooks/useFetch';

export function HealthPage() {
  const { data: health, loading } = useFetch(() => api.getSensorHealth());

  if (loading) return <p>Ładowanie...</p>;

  const lowBattery = health?.filter((s) => s.battery !== null && s.battery < 20) ?? [];
  const noData = health?.filter((s) => s.totalReadings24h === 0) ?? [];
  const total = health?.length ?? 0;

  return (
    <div>
      <h2 style={{ fontSize: 20, marginBottom: 12 }}>Zdrowie systemu</h2>

      <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', marginBottom: 16 }}>
        <HealthCard label="Czujniki" value={String(total)} color="#45a249" />
        <HealthCard label="Niski poziom baterii" value={String(lowBattery.length)} color={lowBattery.length > 0 ? '#d32f2f' : '#45a249'} />
        <HealthCard label="Brak danych 24h" value={String(noData.length)} color={noData.length > 0 ? '#c68400' : '#45a249'} />
      </div>

      {lowBattery.length > 0 && (
        <div style={{ marginBottom: 16 }}>
          <h3 style={{ fontSize: 14, marginBottom: 4 }}>⚠️ Niski poziom baterii</h3>
          {lowBattery.map((s) => (
            <div key={s.sensorId} style={{ fontSize: 13, color: '#d32f2f', marginBottom: 2 }}>
              {s.displayName ?? s.externalId}: 🔋 {s.battery}%
            </div>
          ))}
        </div>
      )}

      {noData.length > 0 && (
        <div>
          <h3 style={{ fontSize: 14, marginBottom: 4 }}>⚠️ Brak danych (24h)</h3>
          {noData.map((s) => (
            <div key={s.sensorId} style={{ fontSize: 13, color: '#c68400', marginBottom: 2 }}>
              {s.displayName ?? s.externalId}
            </div>
          ))}
        </div>
      )}

      <h3 style={{ fontSize: 14, marginTop: 16, marginBottom: 8 }}>Wszystkie czujniki</h3>
      <table style={{ width: '100%', fontSize: 12, borderCollapse: 'collapse' }}>
        <thead>
          <tr style={{ textAlign: 'left', borderBottom: '1px solid #e2e8f0' }}>
            <th style={{ padding: 4 }}>Czujnik</th>
            <th style={{ padding: 4 }}>🔋</th>
            <th style={{ padding: 4 }}>📶</th>
            <th style={{ padding: 4 }}>24h</th>
          </tr>
        </thead>
        <tbody>
          {health?.map((s) => (
            <tr key={s.sensorId} style={{ borderBottom: '1px solid #f1f5f9' }}>
              <td style={{ padding: 4 }}>{s.displayName ?? s.externalId}</td>
              <td style={{ padding: 4, color: (s.battery ?? 100) < 20 ? '#d32f2f' : '#475569' }}>{s.battery ?? '—'}%</td>
              <td style={{ padding: 4 }}>{s.linkQuality ?? '—'}</td>
              <td style={{ padding: 4 }}>{s.totalReadings24h}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function HealthCard({ label, value, color }: { label: string; value: string; color: string }) {
  return (
    <div style={{ background: '#fff', borderRadius: 8, padding: '8px 14px', boxShadow: '0 1px 2px rgba(0,0,0,.05)', borderLeft: `3px solid ${color}` }}>
      <div style={{ fontSize: 11, color: '#94a3b8' }}>{label}</div>
      <div style={{ fontSize: 18, fontWeight: 700, color }}>{value}</div>
    </div>
  );
}
