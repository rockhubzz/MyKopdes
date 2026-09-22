'use client';

import { useCallback, useEffect, useState } from 'react';
import AppShell from '@/components/AppShell';
import { PROFILE_UPDATED_EVENT } from '@/components/StaffProfileForm';
import { apiFetch, apiPrefetch } from '@/lib/api';
import { getRole, isStaffRole } from '@/lib/auth';
import { getCachedStaffUser, setCachedStaffUser } from '@/lib/session-cache';
import { STAFF_NAVS, STAFF_ROLE_LABEL_KEYS } from '@/lib/staff-nav';
import { useLanguage, normalizeLocale } from '@/lib/i18n/LanguageContext';
import type { StaffRole, StaffUser } from '@/lib/types';

/**
 * Role-aware shell for all staff sections (/admin, /owner, /employee).
 *
 * The displayed role label + nav items are ALWAYS derived from the
 * logged-in user's actual role (verified via GET /auth/staff/me, with the
 * role cookie as the synchronous initial value) — never from which URL
 * section is being visited.
 *
 * Why: higher-rank roles may legitimately visit lower-rank sections
 * (admin → owner pages, owner → employee pages, bookmarks, shared links).
 * Deriving the shell from the URL made an admin opening Items look like
 * they "switched to shop owner". Deriving it from the user fixes every
 * such case at once.
 */
export default function StaffShell({
  sectionRole,
  children,
}: {
  /** Role assumed from the URL section; only a fallback until the real role is known. */
  sectionRole: StaffRole;
  children: React.ReactNode;
}) {
  // Initialise synchronously from the cookie so the first paint already
  // shows the right role — no flash of the section's fallback shell.
  // The last verified user (if any) additionally fills the header name and
  // avatar instantly; fetchMe() below revalidates in the background.
  const [role, setRole] = useState<StaffRole>(() => {
    const cached = getCachedStaffUser();
    if (cached) return cached.role;
    const cookieRole = getRole();
    return isStaffRole(cookieRole) ? cookieRole : sectionRole;
  });
  const [userName, setUserName] = useState<string | undefined>(() => getCachedStaffUser()?.name);
  const [avatarPath, setAvatarPath] = useState<string | null>(() => getCachedStaffUser()?.avatar_path ?? null);
  const { t, setLang } = useLanguage();

  const fetchMe = useCallback(() => {
    apiFetch<StaffUser>('/auth/staff/me')
      .then((user) => {
        // Trust the server as the source of truth; a stale/tampered
        // cookie must never drive the displayed role.
        setCachedStaffUser(user);
        if (user?.role) setRole(user.role);
        setUserName(user?.name);
        setAvatarPath(user?.avatar_path ?? null);
        // Adopt the account's saved language (e.g. fresh login on a new device).
        if (user?.locale) setLang(normalizeLocale(user.locale));
      })
      .catch(() => {
        // Keep the cookie-derived role; apiFetch already redirects to
        // /login on 401.
      });
  }, [setLang]);

  useEffect(() => {
    // Revalidate in the background even when serving the cached header —
    // a role change or rename appears within a navigation or two.
    fetchMe();
    // Warm the most likely next destination (the section dashboard) so its
    // first open resolves from cache.
    apiPrefetch('/dashboard/owner-summary');
    apiPrefetch('/dashboard/alerts');
    apiPrefetch('/dashboard/employee-summary');
  }, [fetchMe]);

  // A profile save updates the header name/photo without a full reload.
  useEffect(() => {
    window.addEventListener(PROFILE_UPDATED_EVENT, fetchMe);
    return () => window.removeEventListener(PROFILE_UPDATED_EVENT, fetchMe);
  }, [fetchMe]);

  return (
    <AppShell
      navItems={STAFF_NAVS[role].map((e) => ({ href: e.href, icon: e.icon, label: t(e.labelKey), prefetchApi: e.prefetchApi }))}
      roleLabel={t(STAFF_ROLE_LABEL_KEYS[role])}
      userName={userName}
      avatarPath={avatarPath}
      profileHref={role === 'admin' ? '/admin/profile' : role === 'shop_owner' ? '/owner/profile' : '/employee/profile'}
    >
      {children}
    </AppShell>
  );
}
