'use client';

import { useState } from 'react';

import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import type { Transaction } from '@/lib/types';

export default function MyHistoryPage() {
  const columns: Column<Transaction>[] = [
    { header: 'Code', render: (t) => <code className="text-xs">{t.transaction_code}</code> },
    { header: 'Date', render: (t) => new Date(t.created_at).toLocaleString('id-ID') },
    { header: 'Member', render: (t) => t.member?.name ?? '—' },
    { header: 'Total', render: (t) => `Rp ${Number(t.total).toLocaleString('id-ID')}` },
    { header: 'Payment', render: (t) => <span className="capitalize">{t.payment_method.replace('_', ' ')}</span> },
    {
      header: 'Status',
      render: (t) => (
        <span className={`badge ${t.payment_status === 'paid' ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
          {t.payment_status}
        </span>
      ),
    },
  ];
  const [detail, setDetail] = useState<DetailTarget | null>(null);

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold text-koperasi-800">My Transaction History</h1>
      {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}
      <DataTable endpoint="/transactions" columns={columns} onRowClick={(t) => setDetail({ entity: 'transaction', id: t.id })} searchable={false} />
    </div>
  );
}
