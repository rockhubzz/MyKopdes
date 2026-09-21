'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { PackagePlus, ShoppingCart } from 'lucide-react';
import StatCard from '@/components/StatCard';
import { apiFetch } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';

interface EmployeeSummary {
  transactions_today: number;
  revenue_today: number;
  restocks_submitted_today: number;
}

export default function EmployeeDashboardPage() {
  const [summary, setSummary] = useState<EmployeeSummary | null>(null);
  const { t } = useLanguage();

  useEffect(() => {
    apiFetch<EmployeeSummary>('/dashboard/employee-summary').then(setSummary).catch(() => {});
  }, []);

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-koperasi-800">{t('empDash.title')}</h1>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <StatCard label={t('empDash.transactionsToday')} value={summary?.transactions_today ?? '...'} />
        <StatCard label={t('empDash.revenueToday')} value={summary ? `Rp ${summary.revenue_today.toLocaleString('id-ID')}` : '...'} accent="harvest" />
        <StatCard label={t('empDash.restocksToday')} value={summary?.restocks_submitted_today ?? '...'} />
      </div>

      <div className="card flex flex-wrap gap-3">
        <Link href="/employee/cashier" className="btn-primary flex items-center gap-2">
          <ShoppingCart size={16} aria-hidden="true" />
          {t('empDash.openCashier')}
        </Link>
        <Link href="/employee/restock" className="btn-secondary flex items-center gap-2">
          <PackagePlus size={16} aria-hidden="true" />
          {t('empDash.submitRestock')}
        </Link>
      </div>
    </div>
  );
}
