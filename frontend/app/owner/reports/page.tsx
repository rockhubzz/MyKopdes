'use client';

import { useEffect, useState } from 'react';
import StatCard from '@/components/StatCard';
import { apiUrl, apiFetch } from '@/lib/api';
import { getToken } from '@/lib/auth';
import { useDebouncedValue } from '@/lib/useDebouncedValue';
import { useLanguage } from '@/lib/i18n/LanguageContext';

interface Summary {
  revenue: number; cost_of_goods_sold: number; profit: number; profit_margin_pct: number;
  discount_given: number; tax_collected: number; transaction_count: number; average_basket: number;
}
interface BestSeller { name: string; sku: string; units_sold: number; revenue: number }
interface StockValuation { total_value_at_cost: number; total_value_at_retail: number; potential_profit_if_all_sold: number; low_stock_count: number }

function formatRp(n: number) {
  return 'Rp ' + n.toLocaleString('id-ID');
}

export default function ReportsPage() {
  const today = new Date().toISOString().slice(0, 10);
  const monthAgo = new Date(Date.now() - 30 * 86400000).toISOString().slice(0, 10);

  const [from, setFrom] = useState(monthAgo);
  const [to, setTo] = useState(today);
  const [summary, setSummary] = useState<Summary | null>(null);
  const [bestSellers, setBestSellers] = useState<BestSeller[]>([]);
  const [stock, setStock] = useState<StockValuation | null>(null);
  const { t } = useLanguage();

  // Date inputs fire on every keystroke/selection — wait for a pause before
  // refetching so picking a date doesn't hammer the API.
  const debouncedFrom = useDebouncedValue(from, 500);
  const debouncedTo = useDebouncedValue(to, 500);

  // Stock valuation is date-independent: load once, not on every date change.
  useEffect(() => {
    let cancelled = false;
    apiFetch<StockValuation>('/reports/stock-valuation').then((s) => {
      if (!cancelled) setStock(s);
    });
    return () => {
      cancelled = true;
    };
  }, []);

  // The two date-scoped reports always refresh together — one parallel
  // round-trip with a single abort for superseded date selections.
  useEffect(() => {
    const controller = new AbortController();
    const qs = `?from=${debouncedFrom}&to=${debouncedTo}`;
    Promise.all([
      apiFetch<Summary>(`/reports/summary${qs}`, { signal: controller.signal }),
      apiFetch<BestSeller[]>(`/reports/best-sellers${qs}`, { signal: controller.signal }),
    ])
      .then(([s, b]) => {
        setSummary(s);
        setBestSellers(b);
      })
      .catch(() => {
        // Aborted superseded fetch, or a failed load that leaves the
        // previous period's figures on screen — same as before.
      });
    return () => controller.abort();
  }, [debouncedFrom, debouncedTo]);

  async function handleExport(format: 'csv' | 'xlsx' | 'pdf') {
    const res = await fetch(apiUrl(`/reports/transactions/export?format=${format}&from=${from}&to=${to}`), {
      headers: { Authorization: `Bearer ${getToken()}` },
    });
    const blob = await res.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `transactions_${from}_${to}.${format}`;
    a.click();
    window.URL.revokeObjectURL(url);
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold text-koperasi-800">{t('reports.title')}</h1>
        <div className="flex items-center gap-2 text-sm">
          <input type="date" className="input" value={from} onChange={(e) => setFrom(e.target.value)} />
          <span>{t('reports.to')}</span>
          <input type="date" className="input" value={to} onChange={(e) => setTo(e.target.value)} />
        </div>
      </div>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard label={t('reports.revenue')} value={summary ? formatRp(summary.revenue) : '...'} />
        <StatCard label={t('reports.profit')} value={summary ? formatRp(summary.profit) : '...'} accent="harvest" />
        <StatCard label={t('reports.profitMargin')} value={summary ? `${summary.profit_margin_pct}%` : '...'} />
        <StatCard label={t('reports.transactions')} value={summary?.transaction_count ?? '...'} />
      </div>

      <div className="grid md:grid-cols-2 gap-6">
        <div className="card">
          <h2 className="font-semibold text-koperasi-800 mb-3">{t('reports.bestSellers')}</h2>
          <ul className="space-y-2 text-sm">
            {bestSellers.map((b) => (
              <li key={b.sku} className="flex justify-between border-b border-koperasi-50 pb-1">
                <span>{b.name}</span>
                <span className="text-koperasi-500">{t('reports.unitsSold', { units: b.units_sold, revenue: formatRp(b.revenue) })}</span>
              </li>
            ))}
            {bestSellers.length === 0 && <li className="text-koperasi-400">{t('reports.noSales')}</li>}
          </ul>
        </div>

        <div className="card space-y-2">
          <h2 className="font-semibold text-koperasi-800 mb-1">{t('reports.stockValuation')}</h2>
          <div className="flex justify-between text-sm"><span>{t('reports.valueCost')}</span><span>{stock ? formatRp(stock.total_value_at_cost) : '...'}</span></div>
          <div className="flex justify-between text-sm"><span>{t('reports.valueRetail')}</span><span>{stock ? formatRp(stock.total_value_at_retail) : '...'}</span></div>
          <div className="flex justify-between text-sm font-medium"><span>{t('reports.potentialProfit')}</span><span>{stock ? formatRp(stock.potential_profit_if_all_sold) : '...'}</span></div>
          <div className="flex justify-between text-sm text-red-600"><span>{t('reports.lowStock')}</span><span>{stock?.low_stock_count ?? '...'}</span></div>
        </div>
      </div>

      <div className="card">
        <h2 className="font-semibold text-koperasi-800 mb-3">{t('reports.exportTitle', { from, to })}</h2>
        <div className="flex gap-2">
          <button className="btn-secondary" onClick={() => handleExport('csv')}>{t('reports.exportCsv')}</button>
          <button className="btn-secondary" onClick={() => handleExport('xlsx')}>{t('reports.exportExcel')}</button>
          <button className="btn-secondary" onClick={() => handleExport('pdf')}>{t('reports.exportPdf')}</button>
        </div>
      </div>
    </div>
  );
}
