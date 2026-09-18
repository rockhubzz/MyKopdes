'use client';

import { useEffect, useState } from 'react';
import AppShell, { NavItem } from '@/components/AppShell';
import { apiFetch } from '@/lib/api';
import type { StaffUser } from '@/lib/types';

const NAV: NavItem[] = [
  { href: '/owner/dashboard', label: 'Dashboard', icon: '📊' },
  { href: '/owner/items', label: 'Items', icon: '📦' },
  { href: '/owner/categories', label: 'Categories', icon: '🗂️' },
  { href: '/owner/suppliers', label: 'Suppliers', icon: '🚚' },
  { href: '/owner/employees', label: 'Employees', icon: '👤' },
  { href: '/owner/members', label: 'Members', icon: '🪪' },
  { href: '/owner/transactions', label: 'Transactions', icon: '🧾' },
  { href: '/owner/reports', label: 'Reports', icon: '📈' },
];

export default function OwnerLayout({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<StaffUser | null>(null);

  useEffect(() => {
    apiFetch<StaffUser>('/auth/staff/me').then(setUser).catch(() => {});
  }, []);

  return (
    <AppShell navItems={NAV} roleLabel="Shop Owner" userName={user?.name}>
      {children}
    </AppShell>
  );
}
