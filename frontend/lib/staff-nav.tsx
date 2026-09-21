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
    { href: '/admin/dashboard', labelKey: 'nav.dashboard', icon: <LayoutDashboard size={S} /> },
    { href: '/admin/items', labelKey: 'nav.itemsStock', icon: <Boxes size={S} /> },
    { href: '/admin/reports', labelKey: 'nav.reports', icon: <TrendingUp size={S} /> },
    { href: '/admin/members', labelKey: 'nav.members', icon: <IdCard size={S} /> },
    { href: '/admin/users', labelKey: 'nav.staffAccounts', icon: <Users size={S} /> },
    { href: '/admin/discounts', labelKey: 'nav.discountsPromos', icon: <Tag size={S} /> },
    { href: '/admin/settings', labelKey: 'nav.settings', icon: <Settings size={S} /> },
    { href: '/admin/audit-logs', labelKey: 'nav.auditLog', icon: <ScrollText size={S} /> },
    { href: '/admin/backups', labelKey: 'nav.backupRestore', icon: <Archive size={S} /> },
    { href: '/admin/profile', labelKey: 'nav.myProfile', icon: <User size={S} /> },
  ],
  shop_owner: [
    { href: '/owner/dashboard', labelKey: 'nav.dashboard', icon: <LayoutDashboard size={S} /> },
    { href: '/owner/items', labelKey: 'nav.items', icon: <Package size={S} /> },
    { href: '/owner/categories', labelKey: 'nav.categories', icon: <FolderOpen size={S} /> },
    { href: '/owner/suppliers', labelKey: 'nav.suppliers', icon: <Truck size={S} /> },
    { href: '/owner/employees', labelKey: 'nav.employees', icon: <User size={S} /> },
    { href: '/owner/members', labelKey: 'nav.members', icon: <IdCard size={S} /> },
    { href: '/owner/transactions', labelKey: 'nav.transactions', icon: <Receipt size={S} /> },
    { href: '/owner/reports', labelKey: 'nav.reports', icon: <TrendingUp size={S} /> },
    { href: '/owner/profile', labelKey: 'nav.myProfile', icon: <User size={S} /> },
  ],
  employee: [
    { href: '/employee/dashboard', labelKey: 'nav.dashboard', icon: <LayoutDashboard size={S} /> },
    { href: '/employee/cashier', labelKey: 'nav.cashierMode', icon: <ShoppingCart size={S} /> },
    { href: '/employee/restock', labelKey: 'nav.restock', icon: <PackagePlus size={S} /> },
    { href: '/employee/my-history', labelKey: 'nav.myHistory', icon: <History size={S} /> },
    { href: '/employee/profile', labelKey: 'nav.myProfile', icon: <User size={S} /> },
  ],
};
