import { useCallback, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { useFetch } from '../hooks/useFetch';
import { BatteryIcon } from '../components/BatteryIcon';

export function SensorsPage() {
  const { data, loading, error, refetch } = useFetch(
    () =>
      Promise.all([api.getSensorHealth(), api.getNawy()]).then(([health, nawy]) => ({
        health,
        nawy,
      })),
    [],
  );

  const activeNawy = useMemo(() => (data?.nawy ?? []).filter((n) => n.isActive), [data?.nawy]);

  const nawaNameById = useMemo(() => {
    const m = new Map<string, string>();
    for (const n of data?.nawy ?? []) {
      m.set(n.id, n.name);
    }
    return m;
  }, [data?.nawy]);

  const [assigningId, setAssigningId] = useState<string | null>(null);
  const [assignError, setAssignError] = useState<string | null>(null);

  const onAssignChange = useCallback(
    async (sensorId: string, value: string) => {
      setAssignError(null);
      setAssigningId(sensorId);
      try {
        const nawaId = value === '' ? null : value;
        await api.assignSensor(sensorId, nawaId);
        await refetch();
      } catch (e) {
        setAssignError(e instanceof Error ? e.message : 'Nie udało się zapisać');
      } finally {
        setAssigningId(null);
      }
    },
    [refetch],
  );

  if (loading) return <p>Ładowanie...</p>;

  if (error) {
    return (
      <div>
        <h2 style={{ fontSize: 20, marginBottom: 12 }}>Sensory</h2>
        <p style={{ color: '#d32f2f' }}>{error}</p>
      </div>
    );
  }

  const health = data?.health ?? [];

  return (
    <div>
      <h2 style={{ fontSize: 20, marginBottom: 8 }}>Sensory</h2>
      <p style={{ color: '#64748b', fontSize: 13, marginBottom: 14, lineHeight: 1.45 }}>
        Czujniki pojawiają się po pierwszej wiadomości MQTT ze stanem urządzenia (Zigbee2MQTT → Mosquitto).{' '}
        <strong>Przypisz czujnik do nawy</strong>, żeby wilgotność była widoczna na{' '}
        <Link to="/">Dashboardzie</Link> i w wykresach nawy.
      </p>

      {assignError && (
        <p style={{ color: '#d32f2f', fontSize: 13, marginBottom: 10 }} role="alert">
          {assignError}
        </p>
      )}

      {health.length === 0 && (
        <p style={{ color: '#9ca3af', fontSize: 14 }}>
          Brak zarejestrowanych czujników. Sprawdź, czy kontener API ma{' '}
          <code style={{ fontSize: 12 }}>Mqtt__Enabled=true</code> i czy Zigbee2MQTT publikuje na{' '}
          <code style={{ fontSize: 12 }}>zigbee2mqtt/&lt;nazwa&gt;</code> (JSON stanu).
        </p>
      )}

      {health.map((s) => (
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
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 8 }}>
            <div style={{ minWidth: 0 }}>
              <div style={{ fontWeight: 700, fontSize: 14 }}>{s.displayName ?? s.externalId}</div>
              {s.displayName && (
                <div style={{ fontSize: 11, color: '#94a3b8', wordBreak: 'break-word' }}>{s.externalId}</div>
              )}
            </div>
            <BatteryIcon level={s.battery} />
          </div>

          <div style={{ display: 'flex', gap: 12, marginTop: 8, fontSize: 12, color: '#64748b', flexWrap: 'wrap' }}>
            <span>📶 LQ: {s.linkQuality ?? '—'}</span>
            <span>📊 Odczyty 24h: {s.totalReadings24h}</span>
            <span>
              ⏱️{' '}
              {s.lastReadingUtc
                ? new Date(s.lastReadingUtc).toLocaleString('pl-PL', {
                    day: '2-digit',
                    month: '2-digit',
                    hour: '2-digit',
                    minute: '2-digit',
                  })
                : '—'}
            </span>
          </div>

          <label style={{ display: 'block', marginTop: 12, fontSize: 12, color: '#475569' }}>
            <span style={{ display: 'block', marginBottom: 4 }}>Przypisanie do nawy</span>
            <select
              value={s.nawaId ?? ''}
              disabled={assigningId === s.sensorId}
              onChange={(e) => onAssignChange(s.sensorId, e.target.value)}
              style={{
                width: '100%',
                maxWidth: 320,
                padding: '8px 10px',
                borderRadius: 8,
                border: '1px solid #e2e8f0',
                fontSize: 14,
                background: '#fff',
              }}
            >
              <option value="">— brak (nie wliczaj do nawy) —</option>
              {activeNawy.map((n) => (
                <option key={n.id} value={n.id}>
                  {n.name}
                </option>
              ))}
            </select>
          </label>

          {s.nawaId && (
            <p style={{ fontSize: 12, color: '#94a3b8', marginTop: 8, marginBottom: 0 }}>
              Nawy: <strong>{nawaNameById.get(s.nawaId) ?? s.nawaId}</strong>
            </p>
          )}
        </div>
      ))}
    </div>
  );
}
