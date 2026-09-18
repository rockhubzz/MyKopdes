'use client';

import { useEffect, useState } from 'react';
import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import { apiFetch, ApiError } from '@/lib/api';
import type { Transaction } from '@/lib/types';

export default function TransactionsPage() {
  const [reloadKey, setReloadKey] = useState(0);
  const [detail, setDetail] = useState<DetailTarget | null>(null);
  const [methodFilter, setMethodFilter] = useState('');
  const [reconciliation, setReconciliation] = useState<Record<string, { count: number; total: number }>>({});

  useEffect(() => {
    apiFetch<Transaction[] | { data: Transaction[] }>('/transactions?per_page=200').then((res) => {
      const list = Array.isArray(res) ? res : res.data;
      const grouped: Record<string, { count: number; total: number }> = {};
      list.forEach((t) => {
        if (t.payment_status !== 'paid') return;
        grouped[t.payment_method] ??= { count: 0, total: 0 };
        grouped[t.payment_method].count += 1;
        grouped[t.payment_method].total += Number(t.total);
      });
      setReconciliation(grouped);
    });
  }, [reloadKey]);

  async function handleVoid(t: Transaction) {
    if (!confirm(`Void transaction ${t.transaction_code}? This restores stock and reverses any SHU accrued.`)) return;
    try {
      await apiFetch(`/transactions/${t.id}/void`, { method: 'POST' });
      setReloadKey((k) => k + 1);
    } catch (err) {
      alert(err instanceof ApiError ? err.message : 'Failed to void.');
    }
  }

  const columns: Column<Transaction>[] = [
    { header: 'Code', render: (t) => <code className="text-xs">{t.transaction_code}</code> },
    { header: 'Date', render: (t) => new Date(t.created_at).toLocaleString('id-ID') },
    { header: 'Cashier', render: (t) => t.cashier?.name ?? '—' },
    { header: 'Member', render: (t) => t.member?.name ?? '—' },
    { header: 'Total', render: (t) => `Rp ${Number(t.total).toLocaleString('id-ID')}` },
    { header: 'Payment', render: (t) => <span className="capitalize">{t.payment_method.replace('_', ' ')}</span> },
    {
      header: 'Status',
      render: (t) => (
        <span
          className={`badge ${
            t.payment_status === 'paid' ? 'bg-green-100 text-green-700' : t.payment_status === 'void' ? 'bg-red-100 text-red-700' : 'bg-yellow-100 text-yellow-700'
          }`}
        >
          {t.payment_status}
        </span>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-koperasi-800">Transactions</h1>

      <div className="card">
        <h2 className="font-semibold text-koperasi-800 mb-3">Payment Reconciliation (last 200 transactions)</h2>
        <div className="grid grid-cols-3 gap-4">
          {(['cash', 'qris', 'bank_transfer'] as const).map((m) => (
            <div key={m} className="text-center">
              <div className="text-xs uppercase text-koperasi-400">{m.replace('_', ' ')}</div>
              <div className="text-lg font-bold text-koperasi-700">Rp {(reconciliation[m]?.total ?? 0).toLocaleString('id-ID')}</div>
              <div className="text-xs text-koperasi-400">{reconciliation[m]?.count ?? 0} transactions</div>
            </div>
          ))}
        </div>
      </div>

      {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}
      <DataTable
        endpoint="/transactions"
        columns={columns}
        onRowClick={(t) => setDetail({ entity: 'transaction', id: t.id })}
        searchable={false}
        reloadKey={reloadKey}
        extraParams={methodFilter ? `&payment_method=${methodFilter}` : ''}
        actions={(t) =>
          t.payment_status === 'paid' ? (
            <button className="text-red-600 hover:underline text-sm" onClick={() => handleVoid(t)}>
              Void
            </button>
          ) : (
            <span className="text-koperasi-300 text-sm">—</span>
          )
        }
      />
    </div>
  );
}
