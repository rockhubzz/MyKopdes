'use client';

import { useEffect, useState } from 'react';
import AppShell, { NavItem } from '@/components/AppShell';
import { apiFetch } from '@/lib/api';
import type { StaffUser } from '@/lib/types';

const NAV: NavItem[] = [
  { href: '/admin/dashboard', label: 'Dashboard', icon: '📊' },
  { href: '/owner/items', label: 'Items & Stock', icon: '📦' },
  { href: '/owner/reports', label: 'Reports', icon: '📈' },
  { href: '/owner/members', label: 'Members', icon: '🪪' },
  { href: '/admin/users', label: 'Staff Accounts', icon: '👥' },
  { href: '/admin/discounts', label: 'Discounts & Promos', icon: '🏷️' },
  { href: '/admin/settings', label: 'Settings', icon: '⚙️' },
  { href: '/admin/audit-logs', label: 'Audit Log', icon: '🧾' },
  { href: '/admin/backups', label: 'Backup & Restore', icon: '💾' },
];

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<StaffUser | null>(null);

  useEffect(() => {
    apiFetch<StaffUser>('/auth/staff/me').then(setUser).catch(() => {});
  }, []);

  return (
    <AppShell navItems={NAV} roleLabel="Admin" userName={user?.name}>
      {children}
    </AppShell>
  );
}
