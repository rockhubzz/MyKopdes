'use client';

import { useEffect, useRef, useState } from 'react';
import { apiFetch, storageUrl, ApiError } from '@/lib/api';
import { useLanguage, normalizeLocale } from '@/lib/i18n/LanguageContext';
import { PROFILE_UPDATED_EVENT } from '@/components/StaffProfileForm';
import type { Member } from '@/lib/types';

export default function MemberProfilePage() {
  const [member, setMember] = useState<Member | null>(null);
  const [form, setForm] = useState({ name: '', phone: '', email: '', address: '', password: '' });
  const [avatarFile, setAvatarFile] = useState<File | null>(null);
  const [avatarPreview, setAvatarPreview] = useState<string | null>(null);
  const [removeAvatar, setRemoveAvatar] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const { t, setLang } = useLanguage();
  const fileRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    apiFetch<Member>('/member/profile').then((m) => {
      setMember(m);
      setForm({ name: m.name, phone: m.phone ?? '', email: m.email ?? '', address: m.address ?? '', password: '' });
    }).catch(() => {});
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

  useEffect(() => {
    return () => {
      if (avatarPreview) URL.revokeObjectURL(avatarPreview);
    };
  }, [avatarPreview]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSuccess(null);
    try {
      const fd = new FormData();
      fd.append('name', form.name);
      fd.append('phone', form.phone);
      fd.append('email', form.email);
      fd.append('address', form.address);
      if (form.password) fd.append('password', form.password);
      if (avatarFile) {
        fd.append('avatar', avatarFile);
      } else if (removeAvatar) {
        fd.append('remove_avatar', '1');
      }
      // POST (not PUT): the avatar goes out as multipart/form-data.
      const updated = await apiFetch<Member>('/member/profile', { method: 'POST', body: fd, isFormData: true });
      setMember(updated);
      setForm((f) => ({ ...f, password: '' }));
      pickFile(null);
      setRemoveAvatar(false);
      setSuccess(t('memberProfile.updated'));
      setLang(normalizeLocale(updated.locale));
      window.dispatchEvent(new CustomEvent(PROFILE_UPDATED_EVENT));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('memberProfile.updateFailed'));
    }
  }

  if (!member) return <p className="text-koperasi-400">{t('common.loading')}</p>;

  const photoSrc = avatarPreview ?? (!removeAvatar && member.avatar_path ? storageUrl(member.avatar_path) : null);
  const initial = (form.name.trim().charAt(0) || member.membership_id.charAt(0)).toUpperCase();

  return (
    <div className="max-w-md space-y-4">
      <h1 className="text-2xl font-bold text-koperasi-800">{t('memberProfile.title')}</h1>

      <div className="card space-y-1 text-sm">
        <div className="flex justify-between">
          <span className="text-koperasi-400">{t('memberProfile.memberId')}</span>
          <span className="font-mono">{member.membership_id}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-koperasi-400">{t('memberProfile.joined')}</span>
          <span>{new Date(member.join_date).toLocaleDateString('id-ID')}</span>
        </div>
        <p className="text-xs text-koperasi-400 pt-2">
          {t('memberProfile.managedNote')}
        </p>
      </div>

      <form onSubmit={handleSubmit} className="card space-y-3">
        <div className="flex items-center gap-4">
          {photoSrc ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={photoSrc} alt={form.name} width={80} height={80} className="w-20 h-20 rounded-full object-cover shrink-0 bg-koperasi-100" />
          ) : (
            <span aria-hidden="true" className="w-20 h-20 rounded-full flex items-center justify-center text-2xl font-bold shrink-0 bg-harvest-600 text-white">
              {initial}
            </span>
          )}
          <div className="min-w-0">
            <div className="text-sm font-medium text-koperasi-800">{t('memberProfile.photo')}</div>
            <div className="text-xs text-koperasi-400 mb-2">{t('memberProfile.photoHint')}</div>
            <div className="flex flex-wrap gap-2">
              <button type="button" className="btn-secondary px-3 py-1 text-sm" onClick={() => fileRef.current?.click()}>
                {t('memberProfile.changePhoto')}
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
                  {t('memberProfile.removePhoto')}
                </button>
              )}
            </div>
            <input
              ref={fileRef}
              type="file"
              accept="image/*"
              className="hidden"
              aria-label={t('memberProfile.photo')}
              onChange={(e) => pickFile(e.target.files?.[0] ?? null)}
            />
          </div>
        </div>

        <div>
          <label className="label">{t('memberProfile.name')}</label>
          <input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
        </div>
        <div>
          <label className="label">{t('memberProfile.phone')}</label>
          <input className="input" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
        </div>
        <div>
          <label className="label">{t('memberProfile.email')}</label>
          <input className="input" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
        </div>
        <div>
          <label className="label">{t('memberProfile.address')}</label>
          <textarea className="input" value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
        </div>
        <div>
          <label className="label">{t('memberProfile.newPassword')}</label>
          <input className="input" type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} autoComplete="new-password" />
        </div>

        {error && <p className="text-sm text-red-600">{error}</p>}
        {success && <p className="text-sm text-green-600">{success}</p>}

        <button type="submit" className="btn-primary w-full">
          {t('memberProfile.saveChanges')}
        </button>
      </form>
    </div>
  );
}
