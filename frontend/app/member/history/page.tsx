'use client';

import { useState } from 'react';

import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { Transaction } from '@/lib/types';

export default function MemberHistoryPage() {
  const { t, tx } = useLanguage();
  const columns: Column<Transaction>[] = [
    { header: t('memberHistory.colCode'), render: (row) => <code className="text-xs">{row.transaction_code}</code> },
    { header: t('memberHistory.colDate'), render: (row) => new Date(row.created_at).toLocaleString('id-ID') },
    { header: t('memberHistory.colItems'), render: (row) => row.items?.map((i) => i.item?.name).join(', ') ?? '—' },
    { header: t('memberHistory.colTotal'), render: (row) => `Rp ${Number(row.total).toLocaleString('id-ID')}` },
    { header: t('memberHistory.colPayment'), render: (row) => <span className="capitalize">{tx(`pay.${row.payment_method}`, row.payment_method.replace('_', ' '))}</span> },
  ];
  const [detail, setDetail] = useState<DetailTarget | null>(null);

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold text-koperasi-800">{t('memberHistory.title')}</h1>
      {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}
      <DataTable endpoint="/member/transactions" columns={columns} onRowClick={(t) => setDetail({ entity: 'transaction', id: t.id, endpointBase: '/member/transactions' })} searchable={false} />
    </div>
  );
}
