'use client';

import { useEffect, useState } from 'react';
import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import { apiFetch, ApiError } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { Transaction } from '@/lib/types';

export default function TransactionsPage() {
  const [reloadKey, setReloadKey] = useState(0);
  const [detail, setDetail] = useState<DetailTarget | null>(null);
  const [methodFilter, setMethodFilter] = useState('');
  const [reconciliation, setReconciliation] = useState<Record<string, { count: number; total: number }>>({});
  const { t, tx } = useLanguage();

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

  async function handleVoid(row: Transaction) {
    if (!confirm(t('transactions.voidConfirm', { code: row.transaction_code }))) return;
    try {
      await apiFetch(`/transactions/${row.id}/void`, { method: 'POST' });
      setReloadKey((k) => k + 1);
    } catch (err) {
      alert(err instanceof ApiError ? err.message : t('transactions.voidFailed'));
    }
  }

  const columns: Column<Transaction>[] = [
    { header: t('transactions.colCode'), render: (t) => <code className="text-xs">{t.transaction_code}</code> },
    { header: t('transactions.colDate'), render: (t) => new Date(t.created_at).toLocaleString('id-ID') },
    { header: t('transactions.colCashier'), render: (t) => t.cashier?.name ?? '—' },
    { header: t('transactions.colMember'), render: (t) => t.member?.name ?? '—' },
    { header: t('transactions.colTotal'), render: (t) => `Rp ${Number(t.total).toLocaleString('id-ID')}` },
    { header: t('transactions.colPayment'), render: (t) => <span className="capitalize">{tx(`pay.${t.payment_method}`, t.payment_method.replace('_', ' '))}</span> },
    {
      header: t('transactions.colStatus'),
      render: (t) => (
        <span
          className={`badge ${
            t.payment_status === 'paid' ? 'bg-green-100 text-green-700' : t.payment_status === 'void' ? 'bg-red-100 text-red-700' : 'bg-yellow-100 text-yellow-700'
          }`}
        >
          {tx(`status.${t.payment_status}`, t.payment_status)}
        </span>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-koperasi-800">{t('transactions.title')}</h1>

      <div className="card">
        <h2 className="font-semibold text-koperasi-800 mb-3">{t('transactions.reconTitle')}</h2>
        <div className="grid grid-cols-3 gap-4">
          {(['cash', 'qris', 'bank_transfer'] as const).map((m) => (
            <div key={m} className="text-center">
              <div className="text-xs uppercase text-koperasi-400">{tx(`pay.${m}`, m.replace('_', ' '))}</div>
              <div className="text-lg font-bold text-koperasi-700">Rp {(reconciliation[m]?.total ?? 0).toLocaleString('id-ID')}</div>
              <div className="text-xs text-koperasi-400">{t('transactions.countSuffix', { count: reconciliation[m]?.count ?? 0 })}</div>
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
        actions={(row) =>
          row.payment_status === 'paid' ? (
            <button className="btn-action-danger" onClick={() => handleVoid(row)}>
              {t('transactions.void')}
            </button>
          ) : (
            <span className="text-koperasi-300 text-sm">—</span>
          )
        }
      />
    </div>
  );
}
