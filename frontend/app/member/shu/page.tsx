'use client';

import { useEffect, useState } from 'react';
import StatCard from '@/components/StatCard';
import { apiFetch } from '@/lib/api';

interface ShuResponse {
  shu_balance: string;
  accrual_note: string;
}

export default function MemberShuPage() {
  const [shu, setShu] = useState<ShuResponse | null>(null);

  useEffect(() => {
    apiFetch<ShuResponse>('/member/shu').then(setShu);
  }, []);

  return (
    <div className="space-y-6 max-w-lg">
      <h1 className="text-2xl font-bold text-koperasi-800">My SHU (Sisa Hasil Usaha)</h1>

      <StatCard
        label="Current SHU Balance"
        value={shu ? `Rp ${Number(shu.shu_balance).toLocaleString('id-ID')}` : '...'}
        accent="harvest"
      />

      <div className="card text-sm text-koperasi-600 space-y-2">
        <p>{shu?.accrual_note}</p>
        <p className="text-koperasi-400">
          SHU (Sisa Hasil Usaha) is your share of the cooperative&apos;s surplus, accrued automatically as you shop
          here. The rate is set by the cooperative&apos;s administrators and may be reviewed or distributed
          periodically per your cooperative&apos;s bylaws.
        </p>
      </div>
    </div>
  );
}
