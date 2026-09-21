'use client';

import { useState } from 'react';
import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import { apiFetch, ApiError } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { StaffUser } from '@/lib/types';

const emptyForm = { name: '', email: '', password: '', phone: '', shift_label: '' };

export default function EmployeesPage() {
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<StaffUser | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [detail, setDetail] = useState<DetailTarget | null>(null);
  const { t } = useLanguage();

  function openCreate() {
    setEditing(null);
    setForm(emptyForm);
    setError(null);
    setShowForm(true);
  }

  function openEdit(u: StaffUser) {
    setEditing(u);
    setForm({ name: u.name, email: u.email, password: '', phone: u.phone ?? '', shift_label: u.shift_label ?? '' });
    setError(null);
    setShowForm(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      const body: Record<string, unknown> = { ...form, role: 'employee' };
      if (!body.password) delete body.password;

      if (editing) {
        await apiFetch(`/employees/${editing.id}`, { method: 'PUT', body });
      } else {
        await apiFetch('/employees', { method: 'POST', body });
      }
      setShowForm(false);
      setReloadKey((k) => k + 1);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('employees.saveFailed'));
    }
  }

  async function handleDeactivate(u: StaffUser) {
    if (!confirm(t('employees.deactivateConfirm', { name: u.name }))) return;
    await apiFetch(`/employees/${u.id}`, { method: 'DELETE' });
    setReloadKey((k) => k + 1);
  }

  const columns: Column<StaffUser>[] = [
    { header: t('employees.colName'), render: (u) => u.name },
    { header: t('employees.colEmail'), render: (u) => u.email },
    { header: t('employees.colShift'), render: (u) => u.shift_label ?? '—' },
    { header: t('employees.colPhone'), render: (u) => u.phone ?? '—' },
    {
      header: t('employees.colStatus'),
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
        <h1 className="text-2xl font-bold text-koperasi-800">{t('employees.title')}</h1>
        <button className="btn-primary" onClick={openCreate}>
          {t('employees.addEmployee')}
        </button>
      </div>

      {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}
      <DataTable
        endpoint="/employees"
        columns={columns}
        onRowClick={(u) => setDetail({ entity: 'user', id: u.id })}
        reloadKey={reloadKey}
        actions={(u) => (
          <div className="flex justify-end gap-1.5">
            <button className="btn-action-edit" onClick={() => openEdit(u)}>
              {t('employees.edit')}
            </button>
            <button className="btn-action-danger" onClick={() => handleDeactivate(u)}>
              {t('employees.deactivate')}
            </button>
          </div>
        )}
      />

      {showForm && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50 p-4">
          <div className="card w-full max-w-sm">
            <h2 className="font-semibold text-lg mb-4">{editing ? t('employees.editTitle') : t('employees.addTitle')}</h2>
            <form onSubmit={handleSubmit} className="space-y-3">
              <div>
                <label className="label">{t('employees.name')}</label>
                <input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
              </div>
              <div>
                <label className="label">{t('employees.email')}</label>
                <input className="input" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} required />
              </div>
              <div>
                <label className="label">{editing ? t('employees.passwordKeep') : t('employees.password')}</label>
                <input
                  className="input"
                  type="password"
                  value={form.password}
                  onChange={(e) => setForm({ ...form, password: e.target.value })}
                  {...(!editing ? { required: true } : {})}
                />
              </div>
              <div>
                <label className="label">{t('employees.shift')}</label>
                <input
                  className="input"
                  value={form.shift_label}
                  onChange={(e) => setForm({ ...form, shift_label: e.target.value })}
                  placeholder={t('employees.shiftPlaceholder')}
                />
              </div>
              <div>
                <label className="label">{t('employees.phone')}</label>
                <input className="input" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
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
