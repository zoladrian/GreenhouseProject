import { useParams } from 'react-router-dom';
import { api } from '../api/client';
import { useFetch } from '../hooks/useFetch';
import { MoistureChart } from '../components/MoistureChart';
import { TemperatureChart } from '../components/TemperatureChart';
import { BatteryChart } from '../components/BatteryChart';
import { useState } from 'react';
import { QRCodeSVG } from 'qrcode.react';

export function NawaDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: detail, loading } = useFetch(() => api.getNawaDetail(id!), [id]);
  const [range] = useState(24);

  const from = new Date(Date.now() - range * 3600_000).toISOString();
  const to = new Date().toISOString();

  const { data: points } = useFetch(() => api.getMoistureSeries(`nawaId=${id}&from=${from}&to=${to}`), [id, range]);
  const { data: wateringEvents } = useFetch(() => api.getWateringEvents(id!, from, to), [id, range]);
  const { data: dryingRates } = useFetch(() => api.getDryingRates(id!, from, to), [id, range]);

  if (loading || !detail) return <p>Ładowanie...</p>;

  const qrUrl = `${window.location.origin}/nawy/${id}`;

  return (
    <div>
      <h2 style={{ fontSize: 20, marginBottom: 4 }}>{detail.name}</h2>
      {detail.plantNote && <p style={{ color: '#64748b', margin: '0 0 8px', fontSize: 14 }}>🌿 {detail.plantNote}</p>}
      {detail.description && <p style={{ color: '#94a3b8', margin: '0 0 12px', fontSize: 13 }}>{detail.description}</p>}

      <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap', marginBottom: 16 }}>
        <InfoCard label="Progi wilgotności" value={
          detail.moistureMin !== null && detail.moistureMax !== null
            ? `${detail.moistureMin}% – ${detail.moistureMax}%`
            : 'Nie ustawione'
        } />
        <InfoCard label="Sensory" value={String(detail.sensors.length)} />
      </div>

      <MoistureChart points={points ?? []} wateringEvents={wateringEvents ?? []} title="Wilgotność gleby" />
      <TemperatureChart points={points ?? []} />
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
          <h3 style={{ fontSize: 14, marginBottom: 8 }}>Wykryte podlania</h3>
          {wateringEvents.map((e, i) => (
            <div key={i} style={{ fontSize: 13, color: '#475569', marginBottom: 4 }}>
              {new Date(e.detectedAtUtc).toLocaleString('pl-PL')}:{' '}
              {e.moistureBefore}% → {e.moistureAfter}% (+{e.deltaMoisture}%)
            </div>
          ))}
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

function InfoCard({ label, value }: { label: string; value: string }) {
  return (
    <div style={{ background: '#fff', borderRadius: 8, padding: '8px 12px', boxShadow: '0 1px 2px rgba(0,0,0,.05)' }}>
      <div style={{ fontSize: 11, color: '#94a3b8' }}>{label}</div>
      <div style={{ fontSize: 14, fontWeight: 600 }}>{value}</div>
    </div>
  );
}
