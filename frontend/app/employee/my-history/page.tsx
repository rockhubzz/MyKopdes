'use client';

import { useState } from 'react';

import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { Transaction } from '@/lib/types';

export default function MyHistoryPage() {
  const { t, tx } = useLanguage();
  const columns: Column<Transaction>[] = [
    { header: t('myHistory.colCode'), render: (t) => <code className="text-xs">{t.transaction_code}</code> },
    { header: t('myHistory.colDate'), render: (t) => new Date(t.created_at).toLocaleString('id-ID') },
    { header: t('myHistory.colMember'), render: (t) => t.member?.name ?? '—' },
    { header: t('myHistory.colTotal'), render: (t) => `Rp ${Number(t.total).toLocaleString('id-ID')}` },
    { header: t('myHistory.colPayment'), render: (t) => <span className="capitalize">{tx(`pay.${t.payment_method}`, t.payment_method.replace('_', ' '))}</span> },
    {
      header: t('myHistory.colStatus'),
      render: (t) => (
        <span className={`badge ${t.payment_status === 'paid' ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
          {tx(`status.${t.payment_status}`, t.payment_status)}
        </span>
      ),
    },
  ];
  const [detail, setDetail] = useState<DetailTarget | null>(null);

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold text-koperasi-800">{t('myHistory.title')}</h1>
      {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}
      <DataTable endpoint="/transactions" columns={columns} onRowClick={(t) => setDetail({ entity: 'transaction', id: t.id })} searchable={false} />
    </div>
  );
}
