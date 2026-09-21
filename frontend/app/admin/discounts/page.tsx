'use client';

import { useState } from 'react';
import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import { apiFetch, ApiError } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { Discount, DiscountScope, DiscountType } from '@/lib/types';

const emptyForm = {
  name: '', description: '', type: 'percentage' as DiscountType, value: '0',
  buy_qty: '', get_qty: '', scope: 'general' as DiscountScope, min_purchase: '',
  starts_at: '', ends_at: '',
};

export default function DiscountsPage() {
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<Discount | null>(null);
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

  function openEdit(d: Discount) {
    setEditing(d);
    setForm({
      name: d.name, description: d.description ?? '', type: d.type, value: d.value,
      buy_qty: d.buy_qty?.toString() ?? '', get_qty: d.get_qty?.toString() ?? '', scope: d.scope,
      min_purchase: d.min_purchase ?? '', starts_at: d.starts_at?.slice(0, 16) ?? '', ends_at: d.ends_at?.slice(0, 16) ?? '',
    });
    setError(null);
    setShowForm(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      const body = {
        ...form,
        value: Number(form.value || 0),
        buy_qty: form.buy_qty ? Number(form.buy_qty) : null,
        get_qty: form.get_qty ? Number(form.get_qty) : null,
        min_purchase: form.min_purchase ? Number(form.min_purchase) : null,
        starts_at: form.starts_at || null,
        ends_at: form.ends_at || null,
      };
      if (editing) {
        await apiFetch(`/discounts/${editing.id}`, { method: 'PUT', body });
      } else {
        await apiFetch('/discounts', { method: 'POST', body });
      }
      setShowForm(false);
      setReloadKey((k) => k + 1);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('discounts.saveFailed'));
    }
  }

  async function handleDeactivate(d: Discount) {
    if (!confirm(t('discounts.deactivateConfirm', { name: d.name }))) return;
    await apiFetch(`/discounts/${d.id}`, { method: 'DELETE' });
    setReloadKey((k) => k + 1);
  }

  const columns: Column<Discount>[] = [
    { header: t('discounts.colName'), render: (d) => d.name },
    { header: t('discounts.colType'), render: (d) => <span className="capitalize">{tx(`dtype.${d.type}`, d.type.replace(/_/g, ' '))}</span> },
    {
      header: t('discounts.colValue'),
      render: (d) =>
        d.type === 'percentage'
          ? t('discounts.valuePct', { value: d.value })
          : d.type === 'flat'
            ? t('discounts.valueFlat', { value: Number(d.value).toLocaleString('id-ID') })
            : t('discounts.valueBxgy', { x: d.buy_qty ?? 0, y: d.get_qty ?? 0 }),
    },
    { header: t('discounts.colScope'), render: (d) => <span className="badge bg-koperasi-100 text-koperasi-700 capitalize">{tx(`scope.${d.scope}`, d.scope)}</span> },
    {
      header: t('discounts.colStatus'),
      render: (d) => (
        <span className={`badge ${d.is_active ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
          {d.is_active ? t('common.active') : t('common.inactive')}
        </span>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-koperasi-800">{t('discounts.title')}</h1>
        <button className="btn-primary" onClick={openCreate}>
          {t('discounts.newDiscount')}
        </button>
      </div>

      {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}
      <DataTable
        endpoint="/discounts"
        columns={columns}
        onRowClick={(d) => setDetail({ entity: 'discount', id: d.id })}
        reloadKey={reloadKey}
        actions={(d) => (
          <div className="flex justify-end gap-1.5">
            <button className="btn-action-edit" onClick={() => openEdit(d)}>
              {t('discounts.edit')}
            </button>
            <button className="btn-action-danger" onClick={() => handleDeactivate(d)}>
              {t('discounts.deactivate')}
            </button>
          </div>
        )}
      />

      {showForm && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50 p-4 overflow-y-auto">
          <div className="card w-full max-w-md my-8">
            <h2 className="font-semibold text-lg mb-4">{editing ? t('discounts.editTitle') : t('discounts.newTitle')}</h2>
            <form onSubmit={handleSubmit} className="space-y-3">
              <div>
                <label className="label">{t('discounts.name')}</label>
                <input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
              </div>
              <div>
                <label className="label">{t('discounts.description')}</label>
                <input className="input" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="label">{t('discounts.type')}</label>
                  <select className="input" value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value as DiscountType })}>
                    <option value="percentage">{t('discounts.percentage')}</option>
                    <option value="flat">{t('discounts.flatAmount')}</option>
                    <option value="buy_x_get_y">{t('discounts.buyXGetY')}</option>
                  </select>
                </div>
                <div>
                  <label className="label">{t('discounts.scope')}</label>
                  <select className="input" value={form.scope} onChange={(e) => setForm({ ...form, scope: e.target.value as DiscountScope })}>
                    <option value="general">{t('discounts.general')}</option>
                    <option value="member">{t('discounts.memberOnlyOpt')}</option>
                  </select>
                </div>
              </div>

              {form.type !== 'buy_x_get_y' ? (
                <div>
                  <label className="label">{form.type === 'percentage' ? t('discounts.pctLabel') : t('discounts.flatLabel')}</label>
                  <input className="input" type="number" value={form.value} onChange={(e) => setForm({ ...form, value: e.target.value })} />
                </div>
              ) : (
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="label">{t('discounts.buyQty')}</label>
                    <input className="input" type="number" value={form.buy_qty} onChange={(e) => setForm({ ...form, buy_qty: e.target.value })} />
                  </div>
                  <div>
                    <label className="label">{t('discounts.getQtyFree')}</label>
                    <input className="input" type="number" value={form.get_qty} onChange={(e) => setForm({ ...form, get_qty: e.target.value })} />
                  </div>
                </div>
              )}

              <div>
                <label className="label">{t('discounts.minPurchaseOpt')}</label>
                <input className="input" type="number" value={form.min_purchase} onChange={(e) => setForm({ ...form, min_purchase: e.target.value })} />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="label">{t('discounts.startsAtOpt')}</label>
                  <input className="input" type="datetime-local" value={form.starts_at} onChange={(e) => setForm({ ...form, starts_at: e.target.value })} />
                </div>
                <div>
                  <label className="label">{t('discounts.endsAtOpt')}</label>
                  <input className="input" type="datetime-local" value={form.ends_at} onChange={(e) => setForm({ ...form, ends_at: e.target.value })} />
                </div>
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
