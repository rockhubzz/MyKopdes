'use client';

import { useEffect, useState } from 'react';
import AppShell from '@/components/AppShell';
import { apiFetch } from '@/lib/api';
import { getRole, isStaffRole } from '@/lib/auth';
import { STAFF_NAVS, STAFF_ROLE_LABELS } from '@/lib/staff-nav';
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
  const [role, setRole] = useState<StaffRole>(() => {
    const cookieRole = getRole();
    return isStaffRole(cookieRole) ? cookieRole : sectionRole;
  });
  const [userName, setUserName] = useState<string | undefined>(undefined);

  useEffect(() => {
    let cancelled = false;
    apiFetch<StaffUser>('/auth/staff/me')
      .then((user) => {
        if (cancelled) return;
        // Trust the server as the source of truth; a stale/tampered
        // cookie must never drive the displayed role.
        if (user?.role) setRole(user.role);
        setUserName(user?.name);
      })
      .catch(() => {
        // Keep the cookie-derived role; apiFetch already redirects to
        // /login on 401.
      });
    return () => {
      cancelled = true;
    };
  }, []);

  return <AppShell navItems={STAFF_NAVS[role]} roleLabel={STAFF_ROLE_LABELS[role]} userName={userName}>{children}</AppShell>;
}
