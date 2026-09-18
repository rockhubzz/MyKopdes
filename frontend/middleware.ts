import { NextRequest, NextResponse } from 'next/server';

// Route-level access control per the brief's non-functional requirement:
// "Route-level access control in Next.js covering all 4 role tiers, with
// Member routes fully separated from staff/admin routes." This runs at the
// edge before any page code executes.
//
// admin/shop_owner/employee mirror the backend's EnsureRole hierarchy so a
// shop owner can also open Employee-only pages (e.g. to help out at the
// register), but an employee can never open Owner/Admin pages, and a member
// can never reach any staff route at all (and vice versa).
const STAFF_RANK: Record<string, number> = { admin: 0, shop_owner: 1, employee: 2 };
const SECTION_MIN_RANK: Record<string, number> = { admin: 0, owner: 1, employee: 2 };

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const role = request.cookies.get('koperasi_role')?.value;
  const token = request.cookies.get('koperasi_token')?.value;

  const isStaffSection = pathname.startsWith('/admin') || pathname.startsWith('/owner') || pathname.startsWith('/employee');
  const isMemberSection = pathname.startsWith('/member');

  if (!isStaffSection && !isMemberSection) {
    return NextResponse.next();
  }

  if (!token || !role) {
    return NextResponse.redirect(new URL('/login', request.url));
  }

  if (isMemberSection) {
    if (role !== 'member') {
      return NextResponse.redirect(new URL('/login', request.url));
    }
    return NextResponse.next();
  }

  // Staff section: role must be a known staff role and rank high enough
  // for the section being requested.
  if (!(role in STAFF_RANK)) {
    return NextResponse.redirect(new URL('/login', request.url));
  }

  const section = pathname.split('/')[1]; // "admin" | "owner" | "employee"
  const requiredRank = SECTION_MIN_RANK[section];
  const userRank = STAFF_RANK[role];

  if (userRank > requiredRank) {
    // e.g. an employee (rank 2) hitting /admin (needs rank 0) or /owner (needs rank 1)
    return NextResponse.redirect(new URL('/login', request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: ['/admin/:path*', '/owner/:path*', '/employee/:path*', '/member/:path*'],
};
