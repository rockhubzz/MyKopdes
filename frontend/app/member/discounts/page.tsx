'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { Discount } from '@/lib/types';

export default function MemberDiscountsPage() {
  const [discounts, setDiscounts] = useState<Discount[]>([]);
  const { t } = useLanguage();

  useEffect(() => {
    apiFetch<Discount[]>('/member/discounts').then(setDiscounts);
  }, []);

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold text-koperasi-800">{t('memberDiscounts.title')}</h1>
      <div className="grid md:grid-cols-2 gap-4">
        {discounts.map((d) => (
          <div key={d.id} className="card">
            <div className="flex items-center justify-between mb-1">
              <h3 className="font-semibold">{d.name}</h3>
              {d.scope === 'member' && <span className="badge bg-harvest-400/20 text-harvest-600">{t('memberDiscounts.memberOnly')}</span>}
            </div>
            {d.description && <p className="text-sm text-koperasi-500 mb-2">{d.description}</p>}
            <p className="text-sm font-medium text-koperasi-700">
              {d.type === 'percentage' && t('memberDiscounts.pctOff', { value: d.value })}
              {d.type === 'flat' && t('memberDiscounts.flatOff', { value: Number(d.value).toLocaleString('id-ID') })}
              {d.type === 'buy_x_get_y' && t('memberDiscounts.bxgy', { x: d.buy_qty ?? 0, y: d.get_qty ?? 0 })}
            </p>
            {d.ends_at && <p className="text-xs text-koperasi-400 mt-1">{t('memberDiscounts.validUntil', { date: new Date(d.ends_at).toLocaleDateString('id-ID') })}</p>}
          </div>
        ))}
        {discounts.length === 0 && <p className="text-koperasi-400">{t('memberDiscounts.empty')}</p>}
      </div>
    </div>
  );
}
