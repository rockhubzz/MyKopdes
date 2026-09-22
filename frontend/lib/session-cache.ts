import type { Member, StaffUser } from './types';

/**
 * In-memory copies of the last verified session users. Layouts remount when
 * crossing sections (e.g. /admin → /owner), and without this every crossing
 * repaints the header blank until /me round-trips. Kept in a leaf module so
 * AppShell (logout) can clear them without importing layout components.
 */
let cachedStaffUser: StaffUser | null = null;
let cachedMember: Member | null = null;

export function getCachedStaffUser(): StaffUser | null {
  return cachedStaffUser;
}

export function setCachedStaffUser(user: StaffUser | null) {
  cachedStaffUser = user;
}

export function getCachedMember(): Member | null {
  return cachedMember;
}

export function setCachedMember(member: Member | null) {
  cachedMember = member;
}

export function clearSessionCaches() {
  cachedStaffUser = null;
  cachedMember = null;
}
