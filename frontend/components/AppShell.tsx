'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useCallback, useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';
import { clearSession, getRole } from '@/lib/auth';

export interface NavItem {
  href: string;
  label: string;
  icon?: string;
}

const THEMES = {
  koperasi: {
    sidebar: 'bg-koperasi-800 border-koperasi-700',
    subText: 'text-koperasi-300',
    link: 'text-koperasi-200 hover:bg-koperasi-700 hover:text-white',
    linkActive: 'bg-koperasi-600 text-white shadow-sm',
    badge: 'bg-koperasi-100 text-koperasi-700',
    avatar: 'bg-koperasi-600 text-white',
  },
  harvest: {
    sidebar: 'bg-harvest-800 border-harvest-700',
    subText: 'text-amber-200/80',
    link: 'text-amber-100/90 hover:bg-harvest-700 hover:text-white',
    linkActive: 'bg-harvest-600 text-white shadow-sm',
    badge: 'bg-amber-100 text-harvest-700',
    avatar: 'bg-harvest-600 text-white',
  },
} as const;

export default function AppShell({
  navItems,
  roleLabel,
  userName,
  themeColor = 'koperasi',
  children,
}: {
  navItems: NavItem[];
  roleLabel: string;
  userName?: string;
  themeColor?: 'koperasi' | 'harvest';
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const router = useRouter();
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const theme = THEMES[themeColor];

  // A completed navigation closes the mobile drawer.
  useEffect(() => {
    setSidebarOpen(false);
  }, [pathname]);

  const handleLogout = useCallback(async () => {
    const role = getRole();
    try {
      // Revoke the token server-side, not just locally — otherwise it stays
      // valid indefinitely and a captured token could still be replayed
      // after "logging out".
      await apiFetch(role === 'member' ? '/member/logout' : '/auth/staff/logout', { method: 'POST' });
    } catch {
      // Token may already be invalid/expired — fine, we're clearing it locally regardless.
    }
    clearSession();
    router.push('/login');
  }, [router]);

  const initial = (userName?.trim().charAt(0) ?? roleLabel.charAt(0)).toUpperCase();

  return (
    <div className="min-h-screen flex bg-koperasi-50">
      {/* Mobile scrim */}
      {sidebarOpen && (
        <button
          type="button"
          aria-label="Close navigation"
          className="fixed inset-0 z-30 bg-black/40 md:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      <aside
        className={`fixed md:static inset-y-0 left-0 z-40 w-60 ${theme.sidebar} text-white flex flex-col shrink-0 border-r transition-transform duration-200 print:hidden ${
          sidebarOpen ? 'translate-x-0' : '-translate-x-full md:translate-x-0'
        }`}
      >
        <div className="px-5 py-5 border-b border-white/10">
          <div className="text-lg font-bold flex items-center gap-2">
            <span aria-hidden="true">🌾</span> Koperasi
          </div>
          <div className={`text-xs mt-0.5 ${theme.subText}`}>{roleLabel}</div>
        </div>
        <nav aria-label="Primary" className="flex-1 px-2 py-4 space-y-1 overflow-y-auto">
          {navItems.map((item) => {
            const active = pathname === item.href || pathname?.startsWith(item.href + '/');
            return (
              <Link
                key={item.href}
                href={item.href}
                aria-current={active ? 'page' : undefined}
                className={`flex items-center px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                  active ? theme.linkActive : theme.link
                }`}
              >
                {item.icon && (
                  <span aria-hidden="true" className="mr-2">
                    {item.icon}
                  </span>
                )}
                {item.label}
              </Link>
            );
          })}
        </nav>
        <div className="p-3 border-t border-white/10">
          {userName && <div className={`text-xs px-2 mb-2 truncate ${theme.subText}`}>{userName}</div>}
          <button
            type="button"
            onClick={handleLogout}
            className={`w-full text-left px-3 py-2 rounded-lg text-sm transition-colors ${theme.link}`}
          >
            <span aria-hidden="true">⏻</span> Log out
          </button>
        </div>
      </aside>

      <div className="flex-1 min-w-0 flex flex-col">
        <header className="sticky top-0 z-20 bg-white/90 backdrop-blur border-b border-koperasi-100 print:hidden">
          <div className="flex items-center gap-3 px-4 md:px-8 py-3 max-w-7xl mx-auto w-full">
            <button
              type="button"
              className="md:hidden p-2 -ml-2 rounded-lg text-koperasi-600 hover:bg-koperasi-50"
              aria-label="Open navigation"
              aria-expanded={sidebarOpen}
              onClick={() => setSidebarOpen(true)}
            >
              <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden="true">
                <path d="M3 5h14M3 10h14M3 15h14" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
              </svg>
            </button>
            <span className={`badge ${theme.badge}`}>{roleLabel}</span>
            <div className="flex-1" />
            {userName && (
              <div className="flex items-center gap-2 min-w-0">
                <span aria-hidden="true" className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold shrink-0 ${theme.avatar}`}>
                  {initial}
                </span>
                <span className="text-sm font-medium text-koperasi-800 truncate hidden sm:block">{userName}</span>
              </div>
            )}
          </div>
        </header>

        <main className="flex-1 min-w-0">
          <div className="p-4 md:p-8 max-w-7xl mx-auto">{children}</div>
        </main>
      </div>
    </div>
  );
}
