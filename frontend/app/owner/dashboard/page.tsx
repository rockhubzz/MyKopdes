'use client';

import { useEffect, useState } from 'react';
import StatCard from '@/components/StatCard';
import { apiFetch } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';

interface OwnerSummary {
  today: { revenue: number; transaction_count: number; profit: number };
  month_to_date: { revenue: number; transaction_count: number; profit: number; profit_margin_pct: number };
  best_sellers: { name: string; units_sold: number; revenue: number }[];
  stock: { total_value_at_cost: number; total_value_at_retail: number; low_stock_count: number };
}

function formatRp(n: number) {
  return 'Rp ' + n.toLocaleString('id-ID');
}

export default function OwnerDashboardPage() {
  const [summary, setSummary] = useState<OwnerSummary | null>(null);
  const { t } = useLanguage();

  useEffect(() => {
    apiFetch<OwnerSummary>('/dashboard/owner-summary').then(setSummary).catch(() => {});
  }, []);

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-koperasi-800">{t('ownerDash.title')}</h1>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard label={t('ownerDash.revenueToday')} value={summary ? formatRp(summary.today.revenue) : '...'} />
        <StatCard label={t('ownerDash.profitMarginMonth')} value={summary ? `${summary.month_to_date.profit_margin_pct}%` : '...'} accent="harvest" />
        <StatCard label={t('ownerDash.stockValueRetail')} value={summary ? formatRp(summary.stock.total_value_at_retail) : '...'} />
        <StatCard
          label={t('ownerDash.lowStock')}
          value={summary?.stock.low_stock_count ?? '...'}
          accent={summary && summary.stock.low_stock_count > 0 ? 'red' : 'koperasi'}
        />
      </div>

      <div className="card">
        <h2 className="font-semibold text-koperasi-800 mb-3">{t('ownerDash.bestSellers')}</h2>
        <ul className="space-y-2 text-sm">
          {summary?.best_sellers.map((b) => (
            <li key={b.name} className="flex justify-between border-b border-koperasi-50 pb-1">
              <span>{b.name}</span>
              <span className="text-koperasi-500">
                {t('ownerDash.unitsSold', { units: b.units_sold, revenue: formatRp(b.revenue) })}
              </span>
            </li>
          ))}
          {!summary && <li className="text-koperasi-400">{t('ownerDash.loading')}</li>}
        </ul>
      </div>
    </div>
  );
}
