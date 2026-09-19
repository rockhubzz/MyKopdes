import type { NavItem } from '@/components/AppShell';
import type { StaffRole } from '@/lib/types';

export const STAFF_ROLE_LABELS: Record<StaffRole, string> = {
  admin: 'Admin',
  shop_owner: 'Shop Owner',
  employee: 'Employee',
};

/**
 * Single source of truth for staff navigation, keyed by the LOGGED-IN
 * user's role — never by the URL section being visited. This is what
 * prevents the "role switch" illusion: previously AdminLayout linked to
 * /owner/* pages, which mounted OwnerLayout (hardcoded "Shop Owner"),
 * so an admin opening Items appeared to become a shop owner.
 */
export const STAFF_NAVS: Record<StaffRole, NavItem[]> = {
  admin: [
    { href: '/admin/dashboard', label: 'Dashboard', icon: '📊' },
    { href: '/admin/items', label: 'Items & Stock', icon: '📦' },
    { href: '/admin/reports', label: 'Reports', icon: '📈' },
    { href: '/admin/members', label: 'Members', icon: '🪪' },
    { href: '/admin/users', label: 'Staff Accounts', icon: '👥' },
    { href: '/admin/discounts', label: 'Discounts & Promos', icon: '🏷️' },
    { href: '/admin/settings', label: 'Settings', icon: '⚙️' },
    { href: '/admin/audit-logs', label: 'Audit Log', icon: '🧾' },
    { href: '/admin/backups', label: 'Backup & Restore', icon: '💾' },
  ],
  shop_owner: [
    { href: '/owner/dashboard', label: 'Dashboard', icon: '📊' },
    { href: '/owner/items', label: 'Items', icon: '📦' },
    { href: '/owner/categories', label: 'Categories', icon: '🗂️' },
    { href: '/owner/suppliers', label: 'Suppliers', icon: '🚚' },
    { href: '/owner/employees', label: 'Employees', icon: '👤' },
    { href: '/owner/members', label: 'Members', icon: '🪪' },
    { href: '/owner/transactions', label: 'Transactions', icon: '🧾' },
    { href: '/owner/reports', label: 'Reports', icon: '📈' },
  ],
  employee: [
    { href: '/employee/dashboard', label: 'Dashboard', icon: '📊' },
    { href: '/employee/cashier', label: 'Cashier Mode', icon: '🛒' },
    { href: '/employee/restock', label: 'Restock', icon: '📥' },
    { href: '/employee/my-history', label: 'My History', icon: '🧾' },
  ],
};
