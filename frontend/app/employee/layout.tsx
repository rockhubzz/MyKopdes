'use client';

import { useEffect, useState } from 'react';
import AppShell, { NavItem } from '@/components/AppShell';
import { apiFetch } from '@/lib/api';
import type { StaffUser } from '@/lib/types';

const NAV: NavItem[] = [
  { href: '/employee/dashboard', label: 'Dashboard', icon: '📊' },
  { href: '/employee/cashier', label: 'Cashier Mode', icon: '🛒' },
  { href: '/employee/restock', label: 'Restock', icon: '📥' },
  { href: '/employee/my-history', label: 'My History', icon: '🧾' },
];

export default function EmployeeLayout({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<StaffUser | null>(null);

  useEffect(() => {
    apiFetch<StaffUser>('/auth/staff/me').then(setUser).catch(() => {});
  }, []);

  return (
    <AppShell navItems={NAV} roleLabel="Employee" userName={user?.name}>
      {children}
    </AppShell>
  );
}
