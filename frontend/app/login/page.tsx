'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Wheat } from 'lucide-react';
import { apiFetch, ApiError } from '@/lib/api';
import { dashboardPathFor, setSession } from '@/lib/auth';
import { setCachedMember, setCachedStaffUser } from '@/lib/session-cache';
import { useLanguage, normalizeLocale } from '@/lib/i18n/LanguageContext';
import LanguageToggle from '@/components/LanguageToggle';
import type { Member, Role, StaffUser } from '@/lib/types';

export default function LoginPage() {
  const router = useRouter();
  const { t, setLang } = useLanguage();
  const [mode, setMode] = useState<'staff' | 'member'>('staff');
  const [identifier, setIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      if (mode === 'staff') {
        const res = await apiFetch<{ token: string; user: StaffUser }>('/auth/staff/login', {
          method: 'POST',
          body: { email: identifier, password },
        });
        const role: Role = res.user.role;
        setSession(res.token, role);
        // Seed the shell's header cache so the dashboard lands with the
        // name/avatar already painted (revalidated in the background).
        setCachedStaffUser(res.user);
        // Adopt the account's saved language (falls back to English).
        setLang(normalizeLocale(res.user.locale));
        router.push(dashboardPathFor(role));
      } else {
        const res = await apiFetch<{ token: string; member: Member }>('/auth/member/login', {
          method: 'POST',
          body: { identifier, password },
        });
        setSession(res.token, 'member');
        setCachedMember(res.member);
        setLang(normalizeLocale(res.member.locale));
        router.push(dashboardPathFor('member'));
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('login.fallbackError'));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-koperasi-900 via-koperasi-800 to-koperasi-600 px-4 py-8 relative">
      <div className="absolute top-4 right-4">
        <LanguageToggle dark />
      </div>
      <div className="w-full max-w-md">
        <div className="text-center mb-6">
          <div className="mb-2 flex justify-center text-harvest-400" aria-hidden="true">
            <Wheat size={40} />
          </div>
          <h1 className="text-2xl font-bold text-white tracking-tight">Koperasi Store</h1>
          <p className="text-koperasi-100/90 text-sm mt-1">{t('login.tagline')}</p>
        </div>

        <div className="card shadow-xl">
          <div className="grid grid-cols-2 gap-1 mb-5 bg-koperasi-100 rounded-lg p-1">
            {(['staff', 'member'] as const).map((m) => (
              <button
                key={m}
                type="button"
                onClick={() => {
                  setMode(m);
                  setError(null);
                }}
                className={`py-2 rounded-md text-sm font-medium transition-colors ${
                  mode === m ? 'bg-white shadow text-koperasi-800' : 'text-koperasi-600'
                }`}
              >
                {m === 'staff' ? t('login.staffLogin') : t('login.memberLogin')}
              </button>
            ))}
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label htmlFor="login-identifier" className="label">
                {mode === 'staff' ? t('login.email') : t('login.memberIdOrPhone')}
              </label>
              <input
                id="login-identifier"
                className="input"
                type={mode === 'staff' ? 'email' : 'text'}
                value={identifier}
                onChange={(e) => setIdentifier(e.target.value)}
                placeholder={mode === 'staff' ? t('login.emailPlaceholder') : t('login.memberPlaceholder')}
                autoComplete={mode === 'staff' ? 'email' : 'username'}
                required
              />
            </div>
            <div>
              <label htmlFor="login-password" className="label">
                {t('login.password')}
              </label>
              <input
                id="login-password"
                className="input"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete="current-password"
                required
              />
            </div>

            {error && (
              <p className="text-sm text-red-600" role="alert">
                {error}
              </p>
            )}

            <button type="submit" className="btn-primary w-full" disabled={loading}>
              {loading ? t('login.signingIn') : t('login.signIn')}
            </button>
          </form>

          <p className="text-xs text-koperasi-400 mt-4 text-center">
            {t('login.demoHint')}
          </p>
        </div>
      </div>
    </div>
  );
}
