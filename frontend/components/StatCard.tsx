import { memo } from 'react';

function StatCard({
  label,
  value,
  sub,
  accent = 'koperasi',
}: {
  label: string;
  value: string | number;
  sub?: string;
  accent?: 'koperasi' | 'harvest' | 'red';
}) {
  const accentClass = { koperasi: 'text-koperasi-600', harvest: 'text-harvest-600', red: 'text-red-600' }[accent];

  return (
    <div className="card">
      <div className="text-xs uppercase tracking-wider text-koperasi-400 font-semibold">{label}</div>
      <div className={`text-2xl font-bold mt-1 tabular-nums ${accentClass}`}>{value}</div>
      {sub && <div className="text-xs text-koperasi-400 mt-1">{sub}</div>}
    </div>
  );
}

// Purely presentational — never re-render unless props change.
export default memo(StatCard);
