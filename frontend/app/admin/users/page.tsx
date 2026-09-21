'use client';

import { useState } from 'react';
import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import { apiFetch, ApiError } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { StaffRole, StaffUser } from '@/lib/types';

const emptyForm = { name: '', email: '', password: '', role: 'employee' as StaffRole, phone: '', shift_label: '' };

export default function UsersPage() {
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<StaffUser | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [detail, setDetail] = useState<DetailTarget | null>(null);
  const { t, tx } = useLanguage();

  function openCreate() {
    setEditing(null);
    setForm(emptyForm);
    setError(null);
    setShowForm(true);
  }

  function openEdit(u: StaffUser) {
    setEditing(u);
    setForm({ name: u.name, email: u.email, password: '', role: u.role, phone: u.phone ?? '', shift_label: u.shift_label ?? '' });
    setError(null);
    setShowForm(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      const body: Record<string, unknown> = { ...form };
      if (!body.password) delete body.password;

      if (editing) {
        await apiFetch(`/users/${editing.id}`, { method: 'PUT', body });
      } else {
        await apiFetch('/users', { method: 'POST', body });
      }
      setShowForm(false);
      setReloadKey((k) => k + 1);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('users.saveFailed'));
    }
  }

  async function handleDeactivate(u: StaffUser) {
    if (!confirm(t('users.deactivateConfirm', { name: u.name }))) return;
    await apiFetch(`/users/${u.id}`, { method: 'DELETE' });
    setReloadKey((k) => k + 1);
  }

  const columns: Column<StaffUser>[] = [
    { header: t('users.colName'), render: (u) => u.name },
    { header: t('users.colEmail'), render: (u) => u.email },
    {
      header: t('users.colRole'),
      render: (u) => (
        <span className="badge bg-koperasi-100 text-koperasi-700 capitalize">{tx(`roleValue.${u.role}`, u.role.replace('_', ' '))}</span>
      ),
    },
    { header: t('users.colPhone'), render: (u) => u.phone ?? '—' },
    {
      header: t('users.colStatus'),
      render: (u) => (
        <span className={`badge ${u.is_active ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
          {u.is_active ? t('common.active') : t('common.inactive')}
        </span>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-koperasi-800">{t('users.title')}</h1>
        <button className="btn-primary" onClick={openCreate}>
          {t('users.addStaff')}
        </button>
      </div>

      {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}
      <DataTable
        endpoint="/users"
        columns={columns}
        onRowClick={(u) => setDetail({ entity: 'user', id: u.id })}
        reloadKey={reloadKey}
        actions={(u) => (
          <div className="flex justify-end gap-1.5">
            <button className="btn-action-edit" onClick={() => openEdit(u)}>
              {t('users.edit')}
            </button>
            <button className="btn-action-danger" onClick={() => handleDeactivate(u)}>
              {t('users.deactivate')}
            </button>
          </div>
        )}
      />

      {showForm && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50 p-4">
          <div className="card w-full max-w-md">
            <h2 className="font-semibold text-lg mb-4">{editing ? t('users.editTitle') : t('users.addTitle')}</h2>
            <form onSubmit={handleSubmit} className="space-y-3">
              <div>
                <label className="label">{t('users.name')}</label>
                <input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
              </div>
              <div>
                <label className="label">{t('users.email')}</label>
                <input
                  className="input"
                  type="email"
                  value={form.email}
                  onChange={(e) => setForm({ ...form, email: e.target.value })}
                  required
                />
              </div>
              <div>
                <label className="label">{editing ? t('users.passwordKeep') : t('users.password')}</label>
                <input
                  className="input"
                  type="password"
                  value={form.password}
                  onChange={(e) => setForm({ ...form, password: e.target.value })}
                  {...(!editing ? { required: true } : {})}
                />
              </div>
              <div>
                <label className="label">{t('users.role')}</label>
                <select className="input" value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value as StaffRole })}>
                  <option value="admin">{t('roleValue.admin')}</option>
                  <option value="shop_owner">{t('roleValue.shopOwner')}</option>
                  <option value="employee">{t('roleValue.employee')}</option>
                </select>
              </div>
              <div>
                <label className="label">{t('users.phone')}</label>
                <input className="input" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
              </div>
              <div>
                <label className="label">{t('users.shiftLabel')}</label>
                <input
                  className="input"
                  value={form.shift_label}
                  onChange={(e) => setForm({ ...form, shift_label: e.target.value })}
                  placeholder={t('users.shiftPlaceholder')}
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
          </div>
        </div>
      )}
    </div>
  );
}
