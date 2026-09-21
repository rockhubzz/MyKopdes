'use client';

import { useLanguage, type Language } from '@/lib/i18n/LanguageContext';
import { apiFetch } from '@/lib/api';
import { getRole, isStaffRole } from '@/lib/auth';

/**
 * Persist the choice to the account so it follows the user across devices.
 * Fire-and-forget: the local choice applies immediately regardless.
 */
function persistLocale(lang: Language) {
  const role = getRole();
  if (!role) return; // pre-login toggle (login page) — local only
  const endpoint = role === 'member' ? '/member/profile' : isStaffRole(role) ? '/auth/staff/profile' : null;
  if (!endpoint) return;
  const fd = new FormData();
  fd.append('locale', lang);
  apiFetch(endpoint, { method: 'POST', body: fd, isFormData: true }).catch(() => {});
}

/**
 * EN | ID segmented toggle. Renders everywhere it is mounted and switches
 * the whole app instantly (choice persists in localStorage + on the account).
 */
export default function LanguageToggle({ dark = false }: { dark?: boolean }) {
  const { lang, setLang } = useLanguage();

  const options: { value: Language; label: string }[] = [
    { value: 'en', label: 'EN' },
    { value: 'id', label: 'ID' },
  ];

  return (
    <div
      role="group"
      aria-label="Language / Bahasa"
      className={`inline-flex items-center rounded-lg p-0.5 text-xs font-semibold border shrink-0 ${
        dark ? 'bg-white/10 border-white/20' : 'bg-koperasi-50 border-koperasi-200'
      }`}
    >
      {options.map((o) => {
        const selected = lang === o.value;
        return (
          <button
            key={o.value}
            type="button"
            aria-pressed={selected}
            onClick={() => {
              setLang(o.value);
              persistLocale(o.value);
            }}
            className={`px-2.5 py-1 rounded-md transition-colors ${
              selected
                ? dark
                  ? 'bg-white text-koperasi-800 shadow-sm'
                  : 'bg-koperasi-600 text-white shadow-sm'
                : dark
                  ? 'text-white/70 hover:text-white'
                  : 'text-koperasi-500 hover:text-koperasi-700'
            }`}
          >
            {o.label}
          </button>
        );
      })}
    </div>
  );
}
