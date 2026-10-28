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
  const [memberAuthMode, setMemberAuthMode] = useState<'login' | 'signup'>('login');
  const [identifier, setIdentifier] = useState('');
  const [memberName, setMemberName] = useState('');
  const [memberPhone, setMemberPhone] = useState('');
  const [memberEmail, setMemberEmail] = useState('');
  const [memberAddress, setMemberAddress] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
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
      } else if (memberAuthMode === 'signup') {
        if (password !== confirmPassword) {
          setError(t('login.passwordMismatch'));
          return;
        }
        const res = await apiFetch<{ token: string; member: Member }>('/auth/member/register', {
          method: 'POST',
          body: {
            name: memberName.trim(),
            phone: memberPhone.trim(),
            email: memberEmail.trim() || null,
            address: memberAddress.trim() || null,
            password,
          },
        });
        setSession(res.token, 'member');
        setCachedMember(res.member);
        setLang(normalizeLocale(res.member.locale));
        router.push(dashboardPathFor('member'));
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
                  if (m === 'staff') setMemberAuthMode('login');
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

          {mode === 'member' && (
            <div className="flex items-center justify-center gap-4 mb-5 text-sm" role="tablist" aria-label={t('login.memberAccessMode')}>
              <button
                type="button"
                role="tab"
                aria-selected={memberAuthMode === 'login'}
                className={memberAuthMode === 'login' ? 'font-semibold text-koperasi-800' : 'text-koperasi-400 hover:text-koperasi-700'}
                onClick={() => {
                  setMemberAuthMode('login');
                  setError(null);
                }}
              >
                {t('login.memberSignIn')}
              </button>
              <span className="text-koperasi-200" aria-hidden="true">|</span>
              <button
                type="button"
                role="tab"
                aria-selected={memberAuthMode === 'signup'}
                className={memberAuthMode === 'signup' ? 'font-semibold text-koperasi-800' : 'text-koperasi-400 hover:text-koperasi-700'}
                onClick={() => {
                  setMemberAuthMode('signup');
                  setError(null);
                }}
              >
                {t('login.createAccount')}
              </button>
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            {mode === 'member' && memberAuthMode === 'signup' ? (
              <>
                <div>
                  <label htmlFor="member-name" className="label">{t('login.fullName')}</label>
                  <input id="member-name" className="input" value={memberName} onChange={(e) => setMemberName(e.target.value)} autoComplete="name" required />
                </div>
                <div>
                  <label htmlFor="member-phone" className="label">{t('login.phone')}</label>
                  <input id="member-phone" className="input" type="tel" value={memberPhone} onChange={(e) => setMemberPhone(e.target.value)} placeholder={t('login.phonePlaceholder')} autoComplete="tel" required />
                </div>
                <div>
                  <label htmlFor="member-email" className="label">{t('login.emailOptional')}</label>
                  <input id="member-email" className="input" type="email" value={memberEmail} onChange={(e) => setMemberEmail(e.target.value)} autoComplete="email" />
                </div>
                <div>
                  <label htmlFor="member-address" className="label">{t('login.addressOptional')}</label>
                  <textarea id="member-address" className="input" value={memberAddress} onChange={(e) => setMemberAddress(e.target.value)} autoComplete="street-address" />
                </div>
              </>
            ) : (
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
            )}
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
                autoComplete={memberAuthMode === 'signup' ? 'new-password' : 'current-password'}
                minLength={memberAuthMode === 'signup' ? 8 : undefined}
                required
              />
            </div>

            {mode === 'member' && memberAuthMode === 'signup' && (
              <div>
                <label htmlFor="member-confirm-password" className="label">{t('login.confirmPassword')}</label>
                <input
                  id="member-confirm-password"
                  className="input"
                  type="password"
                  value={confirmPassword}
                  onChange={(e) => setConfirmPassword(e.target.value)}
                  autoComplete="new-password"
                  minLength={8}
                  required
                />
              </div>
            )}

            {error && (
              <p className="text-sm text-red-600" role="alert">
                {error}
              </p>
            )}

            <button type="submit" className="btn-primary w-full" disabled={loading}>
              {loading ? (memberAuthMode === 'signup' ? t('login.creatingAccount') : t('login.signingIn')) : memberAuthMode === 'signup' ? t('login.createAccount') : t('login.signIn')}
            </button>
          </form>

          {mode === 'member' && memberAuthMode === 'signup' ? (
            <p className="text-xs text-koperasi-400 mt-4 text-center">{t('login.signupHint')}</p>
          ) : (
            <p className="text-xs text-koperasi-400 mt-4 text-center">{t('login.demoHint')}</p>
          )}
        </div>
      </div>
    </div>
  );
}
