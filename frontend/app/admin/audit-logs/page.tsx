'use client';

import { useState } from 'react';

import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { AuditLog } from '@/lib/types';

export default function AuditLogsPage() {
  const { t } = useLanguage();
  const columns: Column<AuditLog>[] = [
    { header: t('audit.colWhen'), render: (l) => new Date(l.created_at).toLocaleString('id-ID') },
    { header: t('audit.colActor'), render: (l) => l.actor?.name ?? l.actor?.email ?? `#${l.actor_id ?? '—'}` },
    { header: t('audit.colAction'), render: (l) => <code className="text-xs">{l.action}</code> },
    { header: t('audit.colTarget'), render: (l) => (l.auditable_type ? `${l.auditable_type.split('\\').pop()} #${l.auditable_id}` : '—') },
    { header: t('audit.colIp'), render: (l) => l.ip_address ?? '—' },
  ];
  const [detail, setDetail] = useState<DetailTarget | null>(null);

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold text-koperasi-800">{t('audit.title')}</h1>
      <p className="text-sm text-koperasi-500 -mt-3">{t('audit.subtitle')}</p>
      {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}
      <DataTable endpoint="/audit-logs" columns={columns} onRowClick={(l) => setDetail({ entity: 'auditlog', id: l.id })} searchable={false} />
    </div>
  );
}
