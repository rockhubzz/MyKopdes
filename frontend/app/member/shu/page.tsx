'use client';

import { useEffect, useState } from 'react';
import StatCard from '@/components/StatCard';
import { apiFetch } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';

interface ShuResponse {
  shu_balance: string;
  accrual_note: string;
}

export default function MemberShuPage() {
  const [shu, setShu] = useState<ShuResponse | null>(null);
  const { t } = useLanguage();

  useEffect(() => {
    apiFetch<ShuResponse>('/member/shu').then(setShu);
  }, []);

  return (
    <div className="space-y-6 max-w-lg">
      <h1 className="text-2xl font-bold text-koperasi-800">{t('memberShu.title')}</h1>

      <StatCard
        label={t('memberShu.balance')}
        value={shu ? `Rp ${Number(shu.shu_balance).toLocaleString('id-ID')}` : '...'}
        accent="harvest"
      />

      <div className="card text-sm text-koperasi-600 space-y-2">
        <p>{shu?.accrual_note}</p>
        <p className="text-koperasi-400">
          {t('memberShu.explainer')}
        </p>
      </div>
    </div>
  );
}
