'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { dashboardPathFor, getRole } from '@/lib/auth';

export default function RootPage() {
  const router = useRouter();

  useEffect(() => {
    const role = getRole();
    router.replace(role ? dashboardPathFor(role) : '/login');
  }, [router]);

  return (
    <div className="min-h-screen flex items-center justify-center text-koperasi-500">
      Loading...
    </div>
  );
}
