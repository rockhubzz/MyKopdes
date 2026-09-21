'use client';

import { useEffect, useRef, useState } from 'react';
import { apiFetch, storageUrl, ApiError } from '@/lib/api';
import { useLanguage, normalizeLocale } from '@/lib/i18n/LanguageContext';
import type { StaffUser } from '@/lib/types';

/** Fired after a profile save so shells refresh the displayed name/photo. */
export const PROFILE_UPDATED_EVENT = 'koperasi:profile-updated';

/**
 * Self-service profile settings for staff (all tiers): photo, name, email,
 * phone, shift (employees), and password. Role and active flag are never
 * editable here — those stay admin-only.
 */
export default function StaffProfileForm() {
  const { t, tx, setLang } = useLanguage();
  const [user, setUser] = useState<StaffUser | null>(null);
  const [form, setForm] = useState({ name: '', email: '', phone: '', shift_label: '', password: '' });
  const [avatarFile, setAvatarFile] = useState<File | null>(null);
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null);
  const [removeAvatar, setRemoveAvatar] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    apiFetch<StaffUser>('/auth/staff/profile')
      .then((u) => {
        setUser(u);
        setForm({ name: u.name, email: u.email, phone: u.phone ?? '', shift_label: u.shift_label ?? '', password: '' });
      })
      .catch(() => {});
  }, []);

  function pickFile(file: File | null) {
    if (avatarPreview) URL.revokeObjectURL(avatarPreview);
    if (file) {
      setAvatarFile(file);
      setAvatarPreview(URL.createObjectURL(file));
      setRemoveAvatar(false);
    } else {
      setAvatarFile(null);
      setAvatarPreview(null);
    }
  }

  // Avoid leaking the blob URL.
  useEffect(() => {
    return () => {
      if (avatarPreview) URL.revokeObjectURL(avatarPreview);
    };
  }, [avatarPreview]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSuccess(null);
    setSaving(true);
    try {
      const fd = new FormData();
      fd.append('name', form.name);
      fd.append('email', form.email);
      fd.append('phone', form.phone);
      if (user?.role === 'employee') fd.append('shift_label', form.shift_label);
      if (form.password) fd.append('password', form.password);
      if (avatarFile) {
        fd.append('avatar', avatarFile);
      } else if (removeAvatar) {
        fd.append('remove_avatar', '1');
      }
      // POST (not PUT): the avatar goes out as multipart/form-data.
      const updated = await apiFetch<StaffUser>('/auth/staff/profile', { method: 'POST', body: fd, isFormData: true });
      setUser(updated);
      setForm((f) => ({ ...f, password: '' }));
      pickFile(null);
      setRemoveAvatar(false);
      setSuccess(t('profile.updated'));
      setLang(normalizeLocale(updated.locale));
      window.dispatchEvent(new CustomEvent(PROFILE_UPDATED_EVENT));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('profile.updateFailed'));
    } finally {
      setSaving(false);
    }
  }

  if (!user) return <p className="text-koperasi-400">{t('common.loading')}</p>;

  const photoSrc = avatarPreview ?? (!removeAvatar && user.avatar_path ? storageUrl(user.avatar_path) : null);
  const initial = (form.name.trim().charAt(0) || user.email.charAt(0)).toUpperCase();

  return (
    <div className="max-w-md space-y-4">
      <h1 className="text-2xl font-bold text-koperasi-800">{t('profile.title')}</h1>

      <form onSubmit={handleSubmit} className="card space-y-4">
        <div className="flex items-center gap-4">
          {photoSrc ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={photoSrc} alt={form.name} width={96} height={96} className="w-24 h-24 rounded-full object-cover shrink-0 bg-koperasi-100" />
          ) : (
            <span aria-hidden="true" className="w-24 h-24 rounded-full flex items-center justify-center text-3xl font-bold shrink-0 bg-koperasi-600 text-white">
              {initial}
            </span>
          )}
          <div className="min-w-0">
            <div className="text-sm font-medium text-koperasi-800">{t('profile.photo')}</div>
            <div className="text-xs text-koperasi-400 mb-2">{t('profile.photoHint')}</div>
            <div className="flex flex-wrap gap-2">
              <button type="button" className="btn-secondary px-3 py-1 text-sm" onClick={() => fileRef.current?.click()}>
                {t('profile.changePhoto')}
              </button>
              {(photoSrc || avatarFile) && (
                <button
                  type="button"
                  className="btn-action-danger"
                  onClick={() => {
                    pickFile(null);
                    setRemoveAvatar(true);
                  }}
                >
                  {t('profile.removePhoto')}
                </button>
              )}
            </div>
            <input
              ref={fileRef}
              type="file"
              accept="image/*"
              className="hidden"
              aria-label={t('profile.photo')}
              onChange={(e) => pickFile(e.target.files?.[0] ?? null)}
            />
          </div>
        </div>

        <div>
          <label className="label">{t('profile.role')}</label>
          <div>
            <span className="badge bg-koperasi-100 text-koperasi-700 capitalize">
              {tx(`roleValue.${user.role}`, user.role.replace('_', ' '))}
            </span>
          </div>
        </div>
        <div>
          <label className="label">{t('profile.name')}</label>
          <input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
        </div>
        <div>
          <label className="label">{t('profile.email')}</label>
          <input
            className="input"
            type="email"
            value={form.email}
            onChange={(e) => setForm({ ...form, email: e.target.value })}
            required
          />
        </div>
        <div>
          <label className="label">{t('profile.phone')}</label>
          <input className="input" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
        </div>
        {user.role === 'employee' && (
          <div>
            <label className="label">{t('profile.shift')}</label>
            <input
              className="input"
              value={form.shift_label}
              onChange={(e) => setForm({ ...form, shift_label: e.target.value })}
              placeholder={t('profile.shiftPlaceholder')}
            />
          </div>
        )}
        <div>
          <label className="label">{t('profile.newPassword')}</label>
          <input
            className="input"
            type="password"
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
            autoComplete="new-password"
          />
        </div>

        {error && <p className="text-sm text-red-600">{error}</p>}
        {success && <p className="text-sm text-green-600">{success}</p>}

        <button type="submit" className="btn-primary w-full" disabled={saving}>
          {t('profile.saveChanges')}
        </button>
      </form>
    </div>
  );
}
