'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import StatCard from '@/components/StatCard';
import { apiFetch } from '@/lib/api';

interface EmployeeSummary {
  transactions_today: number;
  revenue_today: number;
  restocks_submitted_today: number;
}

export default function EmployeeDashboardPage() {
  const [summary, setSummary] = useState<EmployeeSummary | null>(null);

  useEffect(() => {
    apiFetch<EmployeeSummary>('/dashboard/employee-summary').then(setSummary).catch(() => {});
  }, []);

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-koperasi-800">My Shift</h1>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <StatCard label="Transactions Today" value={summary?.transactions_today ?? '...'} />
        <StatCard label="Revenue Rung Up Today" value={summary ? `Rp ${summary.revenue_today.toLocaleString('id-ID')}` : '...'} accent="harvest" />
        <StatCard label="Restocks Submitted Today" value={summary?.restocks_submitted_today ?? '...'} />
      </div>

      <div className="card flex flex-wrap gap-3">
        <Link href="/employee/cashier" className="btn-primary">
          🛒 Open Cashier Mode
        </Link>
        <Link href="/employee/restock" className="btn-secondary">
          📥 Submit Restock
        </Link>
      </div>
    </div>
  );
}
