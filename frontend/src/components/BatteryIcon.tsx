export function BatteryIcon({ level }: { level: number | null }) {
  if (level === null) return <span className="text-muted">—</span>;

  const color = level > 50 ? '#45a249' : level > 20 ? '#c68400' : '#d32f2f';
  return (
    <span style={{ fontWeight: 600, color, fontSize: 13 }}>
      🔋 {level}%
    </span>
  );
}
