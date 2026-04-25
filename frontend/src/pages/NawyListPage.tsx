import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import { useFetch } from '../hooks/useFetch';
import { NawyPageBackdrop } from '../components/NawyPageBackdrop';

export function NawyListPage() {
  const { data: nawy, loading, refetch } = useFetch((signal) => api.getNawy(signal));
  const navigate = useNavigate();
  const [showForm, setShowForm] = useState(false);
  const [newName, setNewName] = useState('');

  const handleCreate = async () => {
    if (!newName.trim()) return;
    await api.createNawa({ name: newName.trim() });
    setNewName('');
    setShowForm(false);
    refetch();
  };

  if (loading) {
    return (
      <div className="nawy-page">
        <NawyPageBackdrop />
        <p className="nawy-page__loading">Ładowanie...</p>
      </div>
    );
  }

  return (
    <div className="nawy-page">
      <NawyPageBackdrop />
      <div className="nawy-page__inner">
        <div className="nawy-page__toolbar">
          <h2 className="nawy-page__title">Nawy</h2>
          <button type="button" className="btn-primary" onClick={() => setShowForm(!showForm)}>
            + Dodaj
          </button>
        </div>

        {showForm && (
          <div className="nawy-page__form-row">
            <input
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              placeholder="Nazwa nawy"
              style={{
                flex: 1,
                padding: '6px 10px',
                borderRadius: 6,
                border: '1px solid var(--color-border)',
                fontSize: 14,
              }}
            />
            <button type="button" className="btn-primary" onClick={handleCreate}>
              OK
            </button>
          </div>
        )}

        {nawy && nawy.length === 0 && (
          <p className="nawy-page__empty">Brak naw. Dodaj pierwszą przyciskiem „+ Dodaj”.</p>
        )}

        {nawy?.map((n) => (
          <div
            key={n.id}
            role="button"
            tabIndex={0}
            className="card-surface"
            style={{ padding: 14, marginBottom: 8, cursor: 'pointer' }}
            onClick={() => navigate(`/nawy/${n.id}`)}
            onKeyDown={(e) => e.key === 'Enter' && navigate(`/nawy/${n.id}`)}
          >
            <div style={{ fontWeight: 700 }}>{n.name}</div>
            {n.plantNote && (
              <div style={{ fontSize: 12 }} className="text-muted">
                🌿 {n.plantNote}
              </div>
            )}
            {n.description && (
              <div style={{ fontSize: 12, opacity: 0.75 }} className="text-muted">
                {n.description}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
