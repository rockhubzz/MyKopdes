'use client';

import { useEffect, useState } from 'react';
import { History, House, Receipt, Tags, User, Wallet } from 'lucide-react';
import AppShell, { NavItem } from '@/components/AppShell';
import { PROFILE_UPDATED_EVENT } from '@/components/StaffProfileForm';
import { apiFetch, apiPrefetch } from '@/lib/api';
import { getCachedMember, setCachedMember } from '@/lib/session-cache';
import { useLanguage, normalizeLocale, type TKey } from '@/lib/i18n/LanguageContext';
import type { Member } from '@/lib/types';

const NAV_KEYS: { href: string; labelKey: TKey; icon: React.ReactNode; prefetchApi?: string[] }[] = [
  { href: '/member/dashboard', labelKey: 'nav.dashboard', icon: <House size={16} />, prefetchApi: ['/member/dashboard-summary'] },
  { href: '/member/history', labelKey: 'nav.myPurchases', icon: <Receipt size={16} />, prefetchApi: ['/member/transactions?page=1&per_page=15'] },
  { href: '/member/discounts', labelKey: 'nav.discountsForMe', icon: <Tags size={16} />, prefetchApi: ['/member/discounts'] },
  { href: '/member/shu', labelKey: 'nav.mySHU', icon: <Wallet size={16} />, prefetchApi: ['/member/shu'] },
  { href: '/member/profile', labelKey: 'nav.myProfile', icon: <User size={16} />, prefetchApi: ['/member/profile'] },
];

export default function MemberLayout({ children }: { children: React.ReactNode }) {
  const [member, setMember] = useState<Member | null>(() => getCachedMember());
  const { t, setLang } = useLanguage();

  useEffect(() => {
    // Revalidate even on a cache hit so renames/avatars converge quickly.
    const load = () => {
      apiFetch<Member>('/member/me').then((m) => {
        setCachedMember(m);
        setMember(m);
        // Adopt the account's saved language (e.g. fresh login on a new device).
        if (m?.locale) setLang(normalizeLocale(m.locale));
      }).catch(() => {});
    };
    load();
    apiPrefetch('/member/dashboard-summary');
    // A profile save updates the header name/photo without a full reload.
    window.addEventListener(PROFILE_UPDATED_EVENT, load);
    return () => window.removeEventListener(PROFILE_UPDATED_EVENT, load);
  }, [setLang]);

  const NAV: NavItem[] = NAV_KEYS.map((n) => ({ href: n.href, icon: n.icon, label: t(n.labelKey), prefetchApi: n.prefetchApi }));

  return (
    <AppShell navItems={NAV} roleLabel={t('roles.cooperativeMember')} userName={member?.name} avatarPath={member?.avatar_path} profileHref="/member/profile" themeColor="harvest">
      {children}
    </AppShell>
  );
}
