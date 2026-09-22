'use client';

import { useState } from 'react';
import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import { apiFetch, ApiError } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { Member } from '@/lib/types';

const emptyForm = { name: '', email: '', phone: '', address: '', password: '' };

export default function MembersPage() {
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Member | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [detail, setDetail] = useState<DetailTarget | null>(null);
  const [created, setCreated] = useState<Member | null>(null);
  const { t } = useLanguage();

  function openCreate() {
    setEditing(null);
    setForm(emptyForm);
    setError(null);
    setCreated(null);
    setShowForm(true);
  }

  function openEdit(m: Member) {
    setEditing(m);
    setForm({ name: m.name, email: m.email ?? '', phone: m.phone ?? '', address: m.address ?? '', password: '' });
    setError(null);
    setShowForm(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      if (editing) {
        const body: Record<string, unknown> = { ...form };
        if (!body.password) delete body.password;
        await apiFetch(`/members/${editing.id}`, { method: 'PUT', body });
        setShowForm(false);
      } else {
        const member = await apiFetch<Member>('/members', { method: 'POST', body: form });
        setCreated(member); // show the generated membership ID before closing
      }
      setReloadKey((k) => k + 1);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('members.saveFailed'));
    }
  }

  async function handleDeactivate(m: Member) {
    if (!confirm(t('members.deactivateConfirm', { name: m.name }))) return;
    await apiFetch(`/members/${m.id}`, { method: 'DELETE' });
    setReloadKey((k) => k + 1);
  }

  async function handleActivate(m: Member) {
    if (!confirm(t('members.activateConfirm', { name: m.name }))) return;
    await apiFetch(`/members/${m.id}`, { method: 'PUT', body: { is_active: true } });
    setReloadKey((k) => k + 1);
  }

  async function handleDelete(m: Member) {
    if (!confirm(t('members.deleteConfirm', { name: m.name }))) return;
    try {
      await apiFetch(`/members/${m.id}?force=1`, { method: 'DELETE' });
      setReloadKey((k) => k + 1);
    } catch (err) {
      alert(err instanceof ApiError ? err.message : t('members.deleteFailed'));
    }
  }

  const columns: Column<Member>[] = [
    { header: t('members.colId'), render: (m) => <code className="text-xs">{m.membership_id}</code> },
    { header: t('members.colName'), render: (m) => m.name },
    { header: t('members.colPhone'), render: (m) => m.phone ?? '—' },
    { header: t('members.colShu'), render: (m) => `Rp ${Number(m.shu_balance).toLocaleString('id-ID')}` },
    { header: t('members.colJoined'), render: (m) => new Date(m.join_date).toLocaleDateString('id-ID') },
    {
      header: t('members.colStatus'),
      render: (m) => (
        <span className={`badge ${m.is_active ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
          {m.is_active ? t('common.active') : t('common.inactive')}
        </span>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-koperasi-800">{t('members.title')}</h1>
        <button className="btn-primary" onClick={openCreate}>
          {t('members.enrollMember')}
        </button>
      </div>

      {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}
      <DataTable
        endpoint="/members"
        columns={columns}
        onRowClick={(m) => setDetail({ entity: 'member', id: m.id })}
        reloadKey={reloadKey}
        actions={(m) => (
          <div className="flex justify-end gap-1.5">
            <button className="btn-action-edit" onClick={() => openEdit(m)}>
              {t('members.edit')}
            </button>
            {m.is_active ? (
              <button className="btn-action-danger" onClick={() => handleDeactivate(m)}>
                {t('members.deactivate')}
              </button>
            ) : (
              <button className="btn-action-edit" onClick={() => handleActivate(m)}>
                {t('members.activate')}
              </button>
            )}
            <button className="btn-action-danger" onClick={() => handleDelete(m)}>
              {t('members.delete')}
            </button>
          </div>
        )}
      />

      {showForm && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50 p-4">
          <div className="card w-full max-w-sm">
            <h2 className="font-semibold text-lg mb-4">{editing ? t('members.editTitle') : t('members.enrollTitle')}</h2>

            {created ? (
              <div className="space-y-3 text-sm">
                <p className="text-green-700">
                  {t('members.enrolledMsg')}
                </p>
                <p className="text-lg font-mono bg-koperasi-50 rounded-lg px-3 py-2 text-center">{created.membership_id}</p>
                <button className="btn-primary w-full" onClick={() => setShowForm(false)}>
                  {t('members.done')}
                </button>
              </div>
            ) : (
              <form onSubmit={handleSubmit} className="space-y-3">
                <div>
                  <label className="label">{t('members.name')}</label>
                  <input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
                </div>
                <div>
                  <label className="label">{t('members.phoneLookup')}</label>
                  <input className="input" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
                </div>
                <div>
                  <label className="label">{t('members.emailOpt')}</label>
                  <input className="input" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
                </div>
                <div>
                  <label className="label">{t('members.address')}</label>
                  <textarea className="input" value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
                </div>
                <div>
                  <label className="label">{editing ? t('members.passwordKeep') : t('members.password')}</label>
                  <input
                    className="input"
                    type="password"
                    value={form.password}
                    onChange={(e) => setForm({ ...form, password: e.target.value })}
                    {...(!editing ? { required: true } : {})}
                  />
                </div>
                {error && <p className="text-sm text-red-600">{error}</p>}
                <div className="flex justify-end gap-2 pt-2">
                  <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>
                    {t('common.cancel')}
                  </button>
                  <button type="submit" className="btn-primary">
                    {t('common.save')}
                  </button>
                </div>
              </form>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
