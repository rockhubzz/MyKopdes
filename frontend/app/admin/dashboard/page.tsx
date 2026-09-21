'use client';

import { useEffect, useState } from 'react';
import { CircleCheck, Clock, TriangleAlert } from 'lucide-react';
import StatCard from '@/components/StatCard';
import { apiFetch } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';

interface OwnerSummary {
  today: { revenue: number; transaction_count: number; profit: number; profit_margin_pct: number };
  month_to_date: { revenue: number; transaction_count: number; profit: number; profit_margin_pct: number };
  best_sellers: { name: string; units_sold: number; revenue: number }[];
  stock: { total_value_at_cost: number; total_value_at_retail: number; low_stock_count: number };
}

interface Alerts {
  low_stock: { id: number; name: string; current_stock: number; min_stock_threshold: number }[];
  expiring_soon: { id: number; name: string; expiry_date: string }[];
}

function formatRp(n: number) {
  return 'Rp ' + n.toLocaleString('id-ID');
}

export default function AdminDashboardPage() {
  const [summary, setSummary] = useState<OwnerSummary | null>(null);
  const [alerts, setAlerts] = useState<Alerts | null>(null);
  const { t } = useLanguage();

  useEffect(() => {
    apiFetch<OwnerSummary>('/dashboard/owner-summary').then(setSummary).catch(() => {});
    apiFetch<Alerts>('/dashboard/alerts').then(setAlerts).catch(() => {});
  }, []);

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-koperasi-800">{t('adminDash.title')}</h1>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard label={t('adminDash.revenueToday')} value={summary ? formatRp(summary.today.revenue) : '...'} />
        <StatCard label={t('adminDash.transactionsToday')} value={summary?.today.transaction_count ?? '...'} />
        <StatCard label={t('adminDash.profitMonth')} value={summary ? formatRp(summary.month_to_date.profit) : '...'} accent="harvest" />
        <StatCard
          label={t('adminDash.lowStock')}
          value={summary?.stock.low_stock_count ?? '...'}
          accent={summary && summary.stock.low_stock_count > 0 ? 'red' : 'koperasi'}
        />
      </div>

      <div className="grid md:grid-cols-2 gap-6">
        <div className="card">
          <h2 className="font-semibold text-koperasi-800 mb-3">{t('adminDash.bestSellers')}</h2>
          <ul className="space-y-2 text-sm">
            {summary?.best_sellers.map((b) => (
              <li key={b.name} className="flex justify-between border-b border-koperasi-50 pb-1">
                <span>{b.name}</span>
                <span className="text-koperasi-500">{t('adminDash.unitsSold', { units: b.units_sold, revenue: formatRp(b.revenue) })}</span>
              </li>
            ))}
            {!summary && <li className="text-koperasi-400">{t('adminDash.loading')}</li>}
          </ul>
        </div>

        <div className="card">
          <h2 className="font-semibold text-koperasi-800 mb-3">{t('adminDash.alerts')}</h2>
          {alerts?.low_stock.length === 0 && alerts?.expiring_soon.length === 0 && (
            <p className="text-sm text-koperasi-400 flex items-center gap-1.5">
              <CircleCheck size={14} aria-hidden="true" /> {t('adminDash.noAlerts')}
            </p>
          )}
          <ul className="space-y-2 text-sm">
            {alerts?.low_stock.map((i) => (
              <li key={`ls-${i.id}`} className="flex justify-between">
                <span className="flex items-center gap-1.5">
                  <TriangleAlert size={14} aria-hidden="true" className="text-amber-500 shrink-0" /> {i.name}
                </span>
                <span className="text-red-500">{t('adminDash.leftMin', { stock: i.current_stock, min: i.min_stock_threshold })}</span>
              </li>
            ))}
            {alerts?.expiring_soon.map((i) => (
              <li key={`ex-${i.id}`} className="flex justify-between">
                <span className="flex items-center gap-1.5">
                  <Clock size={14} aria-hidden="true" className="text-harvest-600 shrink-0" /> {i.name}
                </span>
                <span className="text-harvest-600">{t('adminDash.expires', { date: i.expiry_date })}</span>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
}
