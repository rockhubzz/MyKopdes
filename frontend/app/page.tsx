'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { dashboardPathFor, getRole } from '@/lib/auth';
import { useLanguage } from '@/lib/i18n/LanguageContext';

export default function RootPage() {
  const router = useRouter();
  const { t } = useLanguage();

  useEffect(() => {
    const role = getRole();
    router.replace(role ? dashboardPathFor(role) : '/login');
  }, [router]);

  return (
    <div className="min-h-screen flex items-center justify-center text-koperasi-500">
      {t('root.loading')}
    </div>
  );
}
