'use client';

import { useState } from 'react';

import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import type { AuditLog } from '@/lib/types';

export default function AuditLogsPage() {
  const columns: Column<AuditLog>[] = [
    { header: 'When', render: (l) => new Date(l.created_at).toLocaleString('id-ID') },
    { header: 'Actor', render: (l) => l.actor?.name ?? l.actor?.email ?? `#${l.actor_id ?? '—'}` },
    { header: 'Action', render: (l) => <code className="text-xs">{l.action}</code> },
    { header: 'Target', render: (l) => (l.auditable_type ? `${l.auditable_type.split('\\').pop()} #${l.auditable_id}` : '—') },
    { header: 'IP', render: (l) => l.ip_address ?? '—' },
  ];
  const [detail, setDetail] = useState<DetailTarget | null>(null);

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold text-koperasi-800">Audit Log</h1>
      <p className="text-sm text-koperasi-500 -mt-3">Every account change, checkout, restock, and settings edit in the system.</p>
      {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}
      <DataTable endpoint="/audit-logs" columns={columns} onRowClick={(l) => setDetail({ entity: 'auditlog', id: l.id })} searchable={false} />
    </div>
  );
}
