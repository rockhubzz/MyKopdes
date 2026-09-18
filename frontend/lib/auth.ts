'use client';

import Cookies from 'js-cookie';
import type { Member, Role, StaffRole, StaffUser } from './types';

// Non-httpOnly cookies (rather than localStorage alone) so that
// middleware.ts — which runs at the edge, before any React code executes —
// can read the role and gate /admin, /owner, /employee, /member routes
// without an extra round trip. The token itself is only ever sent to our
// own API origin over the Authorization header, never read by other sites.
const TOKEN_COOKIE = 'koperasi_token';
const ROLE_COOKIE = 'koperasi_role';
const COOKIE_OPTS = { expires: 1, sameSite: 'lax' as const, path: '/' };

export function setSession(token: string, role: Role) {
  Cookies.set(TOKEN_COOKIE, token, COOKIE_OPTS);
  Cookies.set(ROLE_COOKIE, role, COOKIE_OPTS);
}

export function clearSession() {
  Cookies.remove(TOKEN_COOKIE, { path: '/' });
  Cookies.remove(ROLE_COOKIE, { path: '/' });
}

export function getToken(): string | undefined {
  return Cookies.get(TOKEN_COOKIE);
}

export function getRole(): Role | undefined {
  return Cookies.get(ROLE_COOKIE) as Role | undefined;
}

export function isStaffRole(role?: Role): role is StaffRole {
  return role === 'admin' || role === 'shop_owner' || role === 'employee';
}

/** Home route each role lands on immediately after login. */
export function dashboardPathFor(role: Role): string {
  switch (role) {
    case 'admin':
      return '/admin/dashboard';
    case 'shop_owner':
      return '/owner/dashboard';
    case 'employee':
      return '/employee/dashboard';
    case 'member':
      return '/member/dashboard';
  }
}

/**
 * Admin has every Shop Owner + Employee permission (see backend
 * EnsureRole middleware) — mirrored here purely for UI decisions like
 * which nav links to show, never as the actual access-control boundary.
 */
const RANK: Record<StaffRole, number> = { admin: 0, shop_owner: 1, employee: 2 };
export function staffHasAtLeast(role: StaffRole, minimum: StaffRole): boolean {
  return RANK[role] <= RANK[minimum];
}

export type SessionUser = StaffUser | Member;
