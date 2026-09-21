'use client';

import { useState } from 'react';
import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import { apiFetch, ApiError } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { Supplier } from '@/lib/types';

const emptyForm = { name: '', contact_person: '', phone: '', email: '', address: '', notes: '' };

export default function SuppliersPage() {
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Supplier | null>(null);
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

  function openEdit(s: Supplier) {
    setEditing(s);
    setForm({
      name: s.name, contact_person: s.contact_person ?? '', phone: s.phone ?? '',
      email: s.email ?? '', address: s.address ?? '', notes: s.notes ?? '',
    });
    setError(null);
    setShowForm(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      if (editing) {
        await apiFetch(`/suppliers/${editing.id}`, { method: 'PUT', body: form });
      } else {
        await apiFetch('/suppliers', { method: 'POST', body: form });
      }
      setShowForm(false);
      setReloadKey((k) => k + 1);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('suppliers.saveFailed'));
    }
  }

  async function handleDelete(s: Supplier) {
    if (!confirm(t('suppliers.deleteConfirm', { name: s.name }))) return;
    try {
      await apiFetch(`/suppliers/${s.id}`, { method: 'DELETE' });
      setReloadKey((k) => k + 1);
    } catch (err) {
      alert(err instanceof ApiError ? err.message : t('suppliers.deleteFailed'));
    }
  }

  const columns: Column<Supplier>[] = [
    { header: t('suppliers.colName'), render: (s) => s.name },
    { header: t('suppliers.colContact'), render: (s) => s.contact_person ?? '—' },
    { header: t('suppliers.colPhone'), render: (s) => s.phone ?? '—' },
    { header: t('suppliers.colEmail'), render: (s) => s.email ?? '—' },
  ];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-koperasi-800">{t('suppliers.title')}</h1>
        <button className="btn-primary" onClick={openCreate}>
          {t('suppliers.addSupplier')}
        </button>
      </div>

      {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}
      <DataTable
        endpoint="/suppliers"
        columns={columns}
        onRowClick={(s) => setDetail({ entity: 'supplier', id: s.id })}
        reloadKey={reloadKey}
        actions={(s) => (
          <div className="flex justify-end gap-1.5">
            <button className="btn-action-edit" onClick={() => openEdit(s)}>
              {t('suppliers.edit')}
            </button>
            <button className="btn-action-danger" onClick={() => handleDelete(s)}>
              {t('suppliers.delete')}
            </button>
          </div>
        )}
      />

      {showForm && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50 p-4">
          <div className="card w-full max-w-md">
            <h2 className="font-semibold text-lg mb-4">{editing ? t('suppliers.editTitle') : t('suppliers.addTitle')}</h2>
            <form onSubmit={handleSubmit} className="space-y-3">
              <div>
                <label className="label">{t('suppliers.name')}</label>
                <input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
              </div>
              <div>
                <label className="label">{t('suppliers.contactPerson')}</label>
                <input className="input" value={form.contact_person} onChange={(e) => setForm({ ...form, contact_person: e.target.value })} />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="label">{t('suppliers.phone')}</label>
                  <input className="input" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
                </div>
                <div>
                  <label className="label">{t('suppliers.email')}</label>
                  <input className="input" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
                </div>
              </div>
              <div>
                <label className="label">{t('suppliers.address')}</label>
                <textarea className="input" value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
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
