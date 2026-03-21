const STATUS_LABELS: Record<number, { label: string; color: string; bg: string }> = {
  0: { label: 'OK', color: '#2e7d32', bg: '#e8f5e9' },
  1: { label: 'Za mokro', color: '#c68400', bg: '#fff8e1' },
  2: { label: 'Sucho', color: '#d32f2f', bg: '#ffebee' },
  3: { label: 'Brak danych', color: '#5c6b5f', bg: '#f1f5f4' },
  4: { label: 'Sprzeczne czujniki', color: '#7b1fa2', bg: '#f3e5f5' },
  5: { label: 'Rozstrzał czujników', color: '#00695c', bg: '#e0f2f1' },
  6: { label: 'Po podlaniu', color: '#2e7d32', bg: '#e8f5e9' },
};

export function StatusBadge({ status }: { status: number }) {
  const s = STATUS_LABELS[status] ?? STATUS_LABELS[3];
  return (
    <span
      style={{
        display: 'inline-block',
        padding: '2px 10px',
        borderRadius: 12,
        fontSize: 13,
        fontWeight: 600,
        color: s.color,
        backgroundColor: s.bg,
      }}
    >
      {s.label}
    </span>
  );
}
