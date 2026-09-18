'use client';

import { useEffect, useState } from 'react';
import StatCard from '@/components/StatCard';
import { apiFetch } from '@/lib/api';
import type { Transaction } from '@/lib/types';

interface MemberSummary {
  shu_balance: string;
  total_purchases: number;
  total_spent: number;
  recent_transactions: Transaction[];
}

export default function MemberDashboardPage() {
  const [summary, setSummary] = useState<MemberSummary | null>(null);

  useEffect(() => {
    apiFetch<MemberSummary>('/member/dashboard-summary').then(setSummary).catch(() => {});
  }, []);

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-koperasi-800">Welcome back!</h1>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <StatCard label="SHU Balance" value={summary ? `Rp ${Number(summary.shu_balance).toLocaleString('id-ID')}` : '...'} accent="harvest" />
        <StatCard label="Total Purchases" value={summary?.total_purchases ?? '...'} />
        <StatCard label="Total Spent" value={summary ? `Rp ${summary.total_spent.toLocaleString('id-ID')}` : '...'} />
      </div>

      <div className="card">
        <h2 className="font-semibold text-koperasi-800 mb-3">Recent Purchases</h2>
        <ul className="space-y-2 text-sm">
          {summary?.recent_transactions.map((t) => (
            <li key={t.id} className="flex justify-between border-b border-koperasi-50 pb-1">
              <span>{new Date(t.created_at).toLocaleDateString('id-ID')} — {t.transaction_code}</span>
              <span className="font-medium">Rp {Number(t.total).toLocaleString('id-ID')}</span>
            </li>
          ))}
          {summary && summary.recent_transactions.length === 0 && <li className="text-koperasi-400">No purchases yet.</li>}
        </ul>
      </div>
    </div>
  );
}
