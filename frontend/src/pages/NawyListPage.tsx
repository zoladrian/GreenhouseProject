import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import { useFetch } from '../hooks/useFetch';

export function NawyListPage() {
  const { data: nawy, loading, refetch } = useFetch(() => api.getNawy());
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

  if (loading) return <p className="text-muted">Ładowanie...</p>;

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
        <h2 style={{ margin: 0, fontSize: 20 }}>Nawy</h2>
        <button type="button" className="btn-primary" onClick={() => setShowForm(!showForm)}>
          + Dodaj
        </button>
      </div>

      {showForm && (
        <div style={{ marginBottom: 12, display: 'flex', gap: 8 }}>
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
  );
}
