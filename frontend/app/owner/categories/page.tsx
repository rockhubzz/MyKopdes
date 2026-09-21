'use client';

import { useState } from 'react';
import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import { apiFetch, ApiError } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { ItemCategory } from '@/lib/types';

export default function CategoriesPage() {
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<ItemCategory | null>(null);
  const [form, setForm] = useState({ name: '', description: '' });
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [detail, setDetail] = useState<DetailTarget | null>(null);
  const { t } = useLanguage();

  function openCreate() {
    setEditing(null);
    setForm({ name: '', description: '' });
    setError(null);
    setShowForm(true);
  }

  function openEdit(c: ItemCategory) {
    setEditing(c);
    setForm({ name: c.name, description: c.description ?? '' });
    setError(null);
    setShowForm(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      if (editing) {
        await apiFetch(`/item-categories/${editing.id}`, { method: 'PUT', body: form });
      } else {
        await apiFetch('/item-categories', { method: 'POST', body: form });
      }
      setShowForm(false);
      setReloadKey((k) => k + 1);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('categories.saveFailed'));
    }
  }

  async function handleDelete(c: ItemCategory) {
    if (!confirm(t('categories.deleteConfirm', { name: c.name }))) return;
    try {
      await apiFetch(`/item-categories/${c.id}`, { method: 'DELETE' });
      setReloadKey((k) => k + 1);
    } catch (err) {
      alert(err instanceof ApiError ? err.message : t('categories.deleteFailed'));
    }
  }

  const columns: Column<ItemCategory>[] = [
    { header: t('categories.colName'), render: (c) => c.name },
    { header: t('categories.colDesc'), render: (c) => c.description ?? '—' },
    { header: t('categories.colItems'), render: (c) => c.items_count ?? 0 },
  ];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-koperasi-800">{t('categories.title')}</h1>
        <button className="btn-primary" onClick={openCreate}>
          {t('categories.addCategory')}
        </button>
      </div>

      {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}
      <DataTable
        endpoint="/item-categories"
        columns={columns}
        onRowClick={(c) => setDetail({ entity: 'category', id: c.id })}
        reloadKey={reloadKey}
        actions={(c) => (
          <div className="flex justify-end gap-1.5">
            <button className="btn-action-edit" onClick={() => openEdit(c)}>
              {t('categories.edit')}
            </button>
            <button className="btn-action-danger" onClick={() => handleDelete(c)}>
              {t('categories.delete')}
            </button>
          </div>
        )}
      />

      {showForm && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50 p-4">
          <div className="card w-full max-w-sm">
            <h2 className="font-semibold text-lg mb-4">{editing ? t('categories.editTitle') : t('categories.addTitle')}</h2>
            <form onSubmit={handleSubmit} className="space-y-3">
              <div>
                <label className="label">{t('categories.name')}</label>
                <input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
              </div>
              <div>
                <label className="label">{t('categories.description')}</label>
                <textarea className="input" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
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
