'use client';

import { useEffect, useState } from 'react';
import { History, House, Receipt, Tags, User, Wallet } from 'lucide-react';
import AppShell, { NavItem } from '@/components/AppShell';
import { PROFILE_UPDATED_EVENT } from '@/components/StaffProfileForm';
import { apiFetch } from '@/lib/api';
import { useLanguage, normalizeLocale, type TKey } from '@/lib/i18n/LanguageContext';
import type { Member } from '@/lib/types';

const NAV_KEYS: { href: string; labelKey: TKey; icon: React.ReactNode }[] = [
  { href: '/member/dashboard', labelKey: 'nav.dashboard', icon: <House size={16} /> },
  { href: '/member/history', labelKey: 'nav.myPurchases', icon: <Receipt size={16} /> },
  { href: '/member/discounts', labelKey: 'nav.discountsForMe', icon: <Tags size={16} /> },
  { href: '/member/shu', labelKey: 'nav.mySHU', icon: <Wallet size={16} /> },
  { href: '/member/profile', labelKey: 'nav.myProfile', icon: <User size={16} /> },
];

export default function MemberLayout({ children }: { children: React.ReactNode }) {
  const [member, setMember] = useState<Member | null>(null);
  const { t, setLang } = useLanguage();

  useEffect(() => {
    const load = () => {
      apiFetch<Member>('/member/me').then((m) => {
        setMember(m);
        // Adopt the account's saved language (e.g. fresh login on a new device).
        if (m?.locale) setLang(normalizeLocale(m.locale));
      }).catch(() => {});
    };
    load();
    // A profile save updates the header name/photo without a full reload.
    window.addEventListener(PROFILE_UPDATED_EVENT, load);
    return () => window.removeEventListener(PROFILE_UPDATED_EVENT, load);
  }, []);

  const NAV: NavItem[] = NAV_KEYS.map((n) => ({ href: n.href, icon: n.icon, label: t(n.labelKey) }));

  return (
    <AppShell navItems={NAV} roleLabel={t('roles.cooperativeMember')} userName={member?.name} avatarPath={member?.avatar_path} profileHref="/member/profile" themeColor="harvest">
      {children}
    </AppShell>
  );
}
