'use client';

import { useEffect, useState } from 'react';
import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import { apiFetch, storageUrl, ApiError } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { Item, ItemCategory, Paginated } from '@/lib/types';

const emptyForm = {
  name: '', sku: '', barcode: '', category_id: '', unit_price: '', cost_price: '',
  unit_of_measure: 'pcs', min_stock_threshold: '10', expiry_date: '',
};

export default function ItemsPage() {
  const [categories, setCategories] = useState<ItemCategory[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Item | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [detail, setDetail] = useState<DetailTarget | null>(null);
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const { t } = useLanguage();

  useEffect(() => {
    apiFetch<Paginated<ItemCategory>>('/item-categories?per_page=100').then((res) => setCategories(res.data));
  }, []);

  function openCreate() {
    setEditing(null);
    setForm(emptyForm);
    setImageFile(null);
    setError(null);
    setShowForm(true);
  }

  function openEdit(item: Item) {
    setEditing(item);
    setForm({
      name: item.name, sku: item.sku, barcode: item.barcode ?? '', category_id: item.category_id?.toString() ?? '',
      unit_price: item.unit_price, cost_price: item.cost_price, unit_of_measure: item.unit_of_measure,
      min_stock_threshold: item.min_stock_threshold.toString(), expiry_date: item.expiry_date ?? '',
    });
    setImageFile(null);
    setError(null);
    setShowForm(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      const fd = new FormData();
      Object.entries(form).forEach(([k, v]) => v && fd.append(k, v));
      if (imageFile) fd.append('image', imageFile);
      if (editing) fd.append('_method', 'PUT');

      await apiFetch(editing ? `/items/${editing.id}` : '/items', {
        method: 'POST', // Laravel form-data PUT workaround via _method spoofing
        body: fd,
        isFormData: true,
      });
      setShowForm(false);
      setReloadKey((k) => k + 1);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('items.saveFailed'));
    }
  }

  async function handleDeactivate(item: Item) {
    if (!confirm(t('items.deactivateConfirm', { name: item.name }))) return;
    await apiFetch(`/items/${item.id}`, { method: 'DELETE' });
    setReloadKey((k) => k + 1);
  }

  const columns: Column<Item>[] = [
    {
      header: t('items.colItem'),
      render: (i) => (
        <div className="flex items-center gap-2">
          {i.image_path && (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={storageUrl(i.image_path)} alt={i.name} width={32} height={32} loading="lazy" className="w-8 h-8 rounded object-cover shrink-0" />
          )}
          <div>
            <div className="font-medium">{i.name}</div>
            <div className="text-xs text-koperasi-400">{i.sku}</div>
          </div>
        </div>
      ),
    },
    { header: t('items.colCategory'), render: (i) => i.category?.name ?? '—' },
    { header: t('items.colPrice'), render: (i) => `Rp ${Number(i.unit_price).toLocaleString('id-ID')}` },
    {
      header: t('items.colStock'),
      render: (i) => (
        <span className={i.current_stock <= i.min_stock_threshold ? 'text-red-600 font-medium' : ''}>
          {i.current_stock} {i.unit_of_measure}
        </span>
      ),
    },
    {
      header: t('items.colStatus'),
      render: (i) => (
        <span className={`badge ${i.is_active ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
          {i.is_active ? t('common.active') : t('common.inactive')}
        </span>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-koperasi-800">{t('items.title')}</h1>
        <div className="flex items-center gap-3">
          <label className="text-sm flex items-center gap-1">
            <input type="checkbox" checked={lowStockOnly} onChange={(e) => setLowStockOnly(e.target.checked)} />
            {t('items.lowStockOnly')}
          </label>
          <button className="btn-primary" onClick={openCreate}>
            {t('items.addItem')}
          </button>
        </div>
      </div>

      {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}
      <DataTable
        endpoint="/items"
        columns={columns}
        onRowClick={(i) => setDetail({ entity: 'item', id: i.id })}
        reloadKey={reloadKey}
        extraParams={lowStockOnly ? '&low_stock=1' : ''}
        actions={(i) => (
          <div className="flex justify-end gap-1.5">
            <button className="btn-action-edit" onClick={() => openEdit(i)}>
              {t('items.edit')}
            </button>
            <button className="btn-action-danger" onClick={() => handleDeactivate(i)}>
              {t('items.deactivate')}
            </button>
          </div>
        )}
      />

      {showForm && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50 p-4 overflow-y-auto">
          <div className="card w-full max-w-md my-8">
            <h2 className="font-semibold text-lg mb-4">{editing ? t('items.editTitle') : t('items.addTitle')}</h2>
            <form onSubmit={handleSubmit} className="space-y-3">
              <div>
                <label className="label">{t('items.name')}</label>
                <input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="label">{t('items.sku')}</label>
                  <input className="input" value={form.sku} onChange={(e) => setForm({ ...form, sku: e.target.value })} required />
                </div>
                <div>
                  <label className="label">{t('items.barcode')}</label>
                  <input className="input" value={form.barcode} onChange={(e) => setForm({ ...form, barcode: e.target.value })} />
                </div>
              </div>
              <div>
                <label className="label">{t('items.category')}</label>
                <select className="input" value={form.category_id} onChange={(e) => setForm({ ...form, category_id: e.target.value })}>
                  <option value="">{t('common.noneOption')}</option>
                  {categories.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="label">{t('items.unitPrice')}</label>
                  <input className="input" type="number" value={form.unit_price} onChange={(e) => setForm({ ...form, unit_price: e.target.value })} required />
                </div>
                <div>
                  <label className="label">{t('items.costPrice')}</label>
                  <input className="input" type="number" value={form.cost_price} onChange={(e) => setForm({ ...form, cost_price: e.target.value })} required />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="label">{t('items.unitMeasure')}</label>
                  <input className="input" value={form.unit_of_measure} onChange={(e) => setForm({ ...form, unit_of_measure: e.target.value })} />
                </div>
                <div>
                  <label className="label">{t('items.minThreshold')}</label>
                  <input
                    className="input"
                    type="number"
                    value={form.min_stock_threshold}
                    onChange={(e) => setForm({ ...form, min_stock_threshold: e.target.value })}
                  />
                </div>
              </div>
              <div>
                <label className="label">{t('items.expiryOpt')}</label>
                <input className="input" type="date" value={form.expiry_date} onChange={(e) => setForm({ ...form, expiry_date: e.target.value })} />
              </div>
              <div>
                <label className="label">{t('items.imageOpt')}</label>
                <input className="input" type="file" accept="image/*" onChange={(e) => setImageFile(e.target.files?.[0] ?? null)} />
              </div>
              {editing && (
                <p className="text-xs text-koperasi-400">
                  {t('items.stockNote', { stock: editing.current_stock })}
                </p>
              )}

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
