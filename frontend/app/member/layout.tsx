'use client';

import { useEffect, useState } from 'react';
import AppShell, { NavItem } from '@/components/AppShell';
import { apiFetch } from '@/lib/api';
import type { Member } from '@/lib/types';

const NAV: NavItem[] = [
  { href: '/member/dashboard', label: 'Dashboard', icon: '🏠' },
  { href: '/member/history', label: 'My Purchases', icon: '🧾' },
  { href: '/member/discounts', label: 'Discounts for Me', icon: '🏷️' },
  { href: '/member/shu', label: 'My SHU', icon: '💰' },
  { href: '/member/profile', label: 'My Profile', icon: '👤' },
];

export default function MemberLayout({ children }: { children: React.ReactNode }) {
  const [member, setMember] = useState<Member | null>(null);

  useEffect(() => {
    apiFetch<Member>('/member/me').then(setMember).catch(() => {});
  }, []);

  return (
    <AppShell navItems={NAV} roleLabel="Cooperative Member" userName={member?.name} themeColor="harvest">
      {children}
    </AppShell>
  );
}
