'use client';

import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useCallback, useEffect, useRef, useState } from 'react';
import { LogOut, User, Wheat } from 'lucide-react';
import { apiFetch, storageUrl } from '@/lib/api';
import { APP_NAME, APP_VERSION } from '@/lib/app-info';
import { clearSession, getRole } from '@/lib/auth';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import LanguageToggle from '@/components/LanguageToggle';

export interface NavItem {
  href: string;
  label: string;
  icon?: React.ReactNode;
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
  avatarPath,
  profileHref,
  themeColor = 'koperasi',
  children,
}: {
  navItems: NavItem[];
  roleLabel: string;
  userName?: string;
  /** Storage-relative avatar path — photo shown instead of the initial when set. */
  avatarPath?: string | null;
  /** Where the "Profile Settings" button in the profile popup leads. */
  profileHref?: string;
  themeColor?: 'koperasi' | 'harvest';
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const router = useRouter();
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);
  const profileRef = useRef<HTMLDivElement>(null);
  const theme = THEMES[themeColor];
  const { t } = useLanguage();

  // A completed navigation closes the mobile drawer and the profile popup.
  useEffect(() => {
    setSidebarOpen(false);
    setProfileOpen(false);
  }, [pathname]);

  // Esc or an outside click closes the profile popup.
  useEffect(() => {
    if (!profileOpen) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setProfileOpen(false);
    };
    const onPointerDown = (e: MouseEvent) => {
      if (profileRef.current && !profileRef.current.contains(e.target as Node)) {
        setProfileOpen(false);
      }
    };
    document.addEventListener('keydown', onKey);
    document.addEventListener('mousedown', onPointerDown);
    return () => {
      document.removeEventListener('keydown', onKey);
      document.removeEventListener('mousedown', onPointerDown);
    };
  }, [profileOpen]);

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

  const renderAvatar = (large = false) => {
    const size = large ? 'w-12 h-12 text-lg' : 'w-8 h-8 text-sm';
    if (avatarPath) {
      // eslint-disable-next-line @next/next/no-img-element
      return (
        <img
          src={storageUrl(avatarPath)}
          alt={userName ?? roleLabel}
          width={large ? 48 : 32}
          height={large ? 48 : 32}
          className={`${large ? 'w-12 h-12' : 'w-8 h-8'} rounded-full object-cover shrink-0`}
        />
      );
    }
    return (
      <span aria-hidden="true" className={`${size} rounded-full flex items-center justify-center font-bold shrink-0 ${theme.avatar}`}>
        {initial}
      </span>
    );
  };

  return (
    <div className="min-h-screen flex bg-koperasi-50">
      {/* Mobile scrim */}
      {sidebarOpen && (
        <button
          type="button"
          aria-label={t('shell.closeNav')}
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
          <div className={`text-xs ${theme.subText}`}>{t('shell.welcome')}</div>
          <div className="text-lg font-bold truncate">{userName ?? roleLabel}</div>
        </div>
        <nav aria-label={t('shell.primaryNav')} className="flex-1 px-2 py-4 space-y-1 overflow-y-auto">
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
                  <span aria-hidden="true" className="mr-2 flex shrink-0">
                    {item.icon}
                  </span>
                )}
                {item.label}
              </Link>
            );
          })}
        </nav>
        <div className="p-4 border-t border-white/10">
          <div className="text-sm font-semibold flex items-center gap-2">
            <Wheat size={16} aria-hidden="true" /> {APP_NAME}
          </div>
          <div className={`text-xs mt-0.5 ${theme.subText}`}>
            {t('shell.version')} {APP_VERSION}
          </div>
        </div>
      </aside>

      <div className="flex-1 min-w-0 flex flex-col">
        <header className="sticky top-0 z-20 bg-white/90 backdrop-blur border-b border-koperasi-100 print:hidden">
          <div className="flex items-center gap-3 px-4 md:px-8 py-3 max-w-7xl mx-auto w-full">
            <button
              type="button"
              className="md:hidden p-2 -ml-2 rounded-lg text-koperasi-600 hover:bg-koperasi-50"
              aria-label={t('shell.openNav')}
              aria-expanded={sidebarOpen}
              onClick={() => setSidebarOpen(true)}
            >
              <svg width="20" height="20" viewBox="0 0 20 20" fill="none" aria-hidden="true">
                <path d="M3 5h14M3 10h14M3 15h14" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
              </svg>
            </button>
            <div className="text-lg font-bold text-koperasi-800 flex items-center gap-2">
              <Wheat size={20} aria-hidden="true" /> Koperasi
            </div>
            <div className="flex-1" />
            {userName && (
              <div className="relative shrink-0" ref={profileRef}>
                <button
                  type="button"
                  aria-label={t('shell.profileMenu')}
                  aria-expanded={profileOpen}
                  onClick={() => setProfileOpen((o) => !o)}
                  className="flex items-center gap-2 min-w-0 rounded-lg p-1 -m-1 hover:bg-koperasi-50 transition-colors"
                >
                  {renderAvatar()}
                  <span className="text-sm font-medium text-koperasi-800 truncate hidden sm:block">{userName}</span>
                </button>
                {profileOpen && (
                  <div className="absolute right-0 top-full mt-2 z-40 w-64 card p-4">
                    <div className="flex items-center gap-3 min-w-0">
                      {renderAvatar(true)}
                      <div className="min-w-0 space-y-1">
                        <div className="font-semibold text-koperasi-800 leading-tight truncate">{userName ?? roleLabel}</div>
                        <span className={`badge ${theme.badge}`}>{roleLabel}</span>
                      </div>
                    </div>
                    <div className="mt-4 pt-3 border-t border-koperasi-100 space-y-2">
                      {profileHref && (
                        <Link href={profileHref} className="btn-secondary w-full text-sm flex items-center justify-center gap-2">
                          <User size={16} aria-hidden="true" /> {t('shell.profileSettings')}
                        </Link>
                      )}
                      <button type="button" onClick={handleLogout} className="btn-action-danger w-full gap-2">
                        <LogOut size={14} aria-hidden="true" /> {t('shell.logout')}
                      </button>
                    </div>
                    <div className="mt-3 pt-3 border-t border-koperasi-100 flex items-center justify-between">
                      <span className="text-xs font-medium text-koperasi-500">{t('shell.language')}</span>
                      <LanguageToggle />
                    </div>
                  </div>
                )}
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
