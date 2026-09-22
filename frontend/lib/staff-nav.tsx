import {
  Archive,
  Boxes,
  FolderOpen,
  History,
  IdCard,
  LayoutDashboard,
  Package,
  PackagePlus,
  Receipt,
  ScrollText,
  Settings,
  ShoppingCart,
  Tag,
  TrendingUp,
  Truck,
  User,
  Users,
} from 'lucide-react';
import type { StaffRole } from '@/lib/types';
import type { TKey } from '@/lib/i18n/LanguageContext';

const S = 16;

export interface StaffNavEntry {
  href: string;
  labelKey: TKey;
  icon?: React.ReactNode;
  /**
   * GET endpoints the destination page fetches on mount. Warmed on link
   * hover/focus so the navigation lands on a hot apiFetch cache.
   */
  prefetchApi?: string[];
}

export const STAFF_ROLE_LABEL_KEYS: Record<StaffRole, TKey> = {
  admin: 'roles.admin',
  shop_owner: 'roles.shopOwner',
  employee: 'roles.employee',
};

/**
 * Single source of truth for staff navigation, keyed by the LOGGED-IN
 * user's role — never by the URL section being visited. This is what
 * prevents the "role switch" illusion: previously AdminLayout linked to
 * /owner/* pages, which mounted OwnerLayout (hardcoded "Shop Owner"),
 * so an admin opening Items appeared to become a shop owner.
 *
 * Labels are translation keys resolved in StaffShell via useLanguage().
 */
export const STAFF_NAVS: Record<StaffRole, StaffNavEntry[]> = {
  admin: [
    { href: '/admin/dashboard', labelKey: 'nav.dashboard', icon: <LayoutDashboard size={S} />, prefetchApi: ['/dashboard/owner-summary', '/dashboard/alerts'] },
    { href: '/admin/items', labelKey: 'nav.itemsStock', icon: <Boxes size={S} />, prefetchApi: ['/items?page=1&per_page=15', '/item-categories?per_page=100'] },
    { href: '/admin/reports', labelKey: 'nav.reports', icon: <TrendingUp size={S} />, prefetchApi: ['/reports/stock-valuation'] },
    { href: '/admin/members', labelKey: 'nav.members', icon: <IdCard size={S} />, prefetchApi: ['/members?page=1&per_page=15'] },
    { href: '/admin/users', labelKey: 'nav.staffAccounts', icon: <Users size={S} />, prefetchApi: ['/users?page=1&per_page=15'] },
    { href: '/admin/discounts', labelKey: 'nav.discountsPromos', icon: <Tag size={S} />, prefetchApi: ['/discounts?page=1&per_page=15'] },
    { href: '/admin/settings', labelKey: 'nav.settings', icon: <Settings size={S} />, prefetchApi: ['/admin/settings'] },
    { href: '/admin/audit-logs', labelKey: 'nav.auditLog', icon: <ScrollText size={S} />, prefetchApi: ['/audit-logs?page=1&per_page=15'] },
    { href: '/admin/backups', labelKey: 'nav.backupRestore', icon: <Archive size={S} />, prefetchApi: ['/backups'] },
    { href: '/admin/profile', labelKey: 'nav.myProfile', icon: <User size={S} /> },
  ],
  shop_owner: [
    { href: '/owner/dashboard', labelKey: 'nav.dashboard', icon: <LayoutDashboard size={S} />, prefetchApi: ['/dashboard/owner-summary'] },
    { href: '/owner/items', labelKey: 'nav.items', icon: <Package size={S} />, prefetchApi: ['/items?page=1&per_page=15', '/item-categories?per_page=100'] },
    { href: '/owner/categories', labelKey: 'nav.categories', icon: <FolderOpen size={S} />, prefetchApi: ['/item-categories?page=1&per_page=15'] },
    { href: '/owner/suppliers', labelKey: 'nav.suppliers', icon: <Truck size={S} />, prefetchApi: ['/suppliers?page=1&per_page=15'] },
    { href: '/owner/employees', labelKey: 'nav.employees', icon: <User size={S} />, prefetchApi: ['/employees?page=1&per_page=15'] },
    { href: '/owner/members', labelKey: 'nav.members', icon: <IdCard size={S} />, prefetchApi: ['/members?page=1&per_page=15'] },
    { href: '/owner/transactions', labelKey: 'nav.transactions', icon: <Receipt size={S} />, prefetchApi: ['/transactions?page=1&per_page=15'] },
    { href: '/owner/reports', labelKey: 'nav.reports', icon: <TrendingUp size={S} />, prefetchApi: ['/reports/stock-valuation'] },
    { href: '/owner/profile', labelKey: 'nav.myProfile', icon: <User size={S} /> },
  ],
  employee: [
    { href: '/employee/dashboard', labelKey: 'nav.dashboard', icon: <LayoutDashboard size={S} />, prefetchApi: ['/dashboard/employee-summary'] },
    { href: '/employee/cashier', labelKey: 'nav.cashierMode', icon: <ShoppingCart size={S} />, prefetchApi: ['/discounts/active?has_member=0'] },
    { href: '/employee/restock', labelKey: 'nav.restock', icon: <PackagePlus size={S} />, prefetchApi: ['/restocking-records?page=1&per_page=15&mine_only=1'] },
    { href: '/employee/my-history', labelKey: 'nav.myHistory', icon: <History size={S} />, prefetchApi: ['/transactions?page=1&per_page=15'] },
    { href: '/employee/profile', labelKey: 'nav.myProfile', icon: <User size={S} /> },
  ],
};
