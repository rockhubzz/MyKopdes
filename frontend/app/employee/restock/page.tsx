'use client';

import { useEffect, useState } from 'react';
import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import { apiFetch, ApiError } from '@/lib/api';
import type { Item, Paginated, RestockingRecord, Supplier } from '@/lib/types';

export default function RestockPage() {
  const [items, setItems] = useState<Item[]>([]);
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [form, setForm] = useState({ item_id: '', supplier_id: '', quantity: '', cost_per_unit: '', notes: '' });
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [detail, setDetail] = useState<DetailTarget | null>(null);

  useEffect(() => {
    apiFetch<Paginated<Item>>('/items?per_page=200').then((res) => setItems(res.data));
    apiFetch<Paginated<Supplier>>('/suppliers?per_page=100').then((res) => setSuppliers(res.data));
  }, []);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSuccess(null);
    try {
      await apiFetch('/restocking-records', {
        method: 'POST',
        body: {
          item_id: Number(form.item_id),
          supplier_id: form.supplier_id ? Number(form.supplier_id) : null,
          quantity: Number(form.quantity),
          cost_per_unit: Number(form.cost_per_unit),
          notes: form.notes || null,
        },
      });
      setSuccess('Restock submitted — stock updated immediately.');
      setForm({ item_id: '', supplier_id: '', quantity: '', cost_per_unit: '', notes: '' });
      setReloadKey((k) => k + 1);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to submit.');
    }
  }

  const columns: Column<RestockingRecord>[] = [
    { header: 'Item', render: (r) => r.item?.name ?? `#${r.item_id}` },
    { header: 'Qty', render: (r) => `+${r.quantity}` },
    { header: 'Cost/Unit', render: (r) => `Rp ${Number(r.cost_per_unit).toLocaleString('id-ID')}` },
    { header: 'Supplier', render: (r) => r.supplier?.name ?? '—' },
    { header: 'Date', render: (r) => new Date(r.restocked_at).toLocaleString('id-ID') },
  ];

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-koperasi-800">Submit Restock</h1>

      <div className="card max-w-lg">
        <form onSubmit={handleSubmit} className="space-y-3">
          <div>
            <label className="label">Item</label>
            <select className="input" value={form.item_id} onChange={(e) => setForm({ ...form, item_id: e.target.value })} required>
              <option value="">Select an item...</option>
              {items.map((i) => (
                <option key={i.id} value={i.id}>
                  {i.name} (current: {i.current_stock} {i.unit_of_measure})
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="label">Supplier</label>
            <select className="input" value={form.supplier_id} onChange={(e) => setForm({ ...form, supplier_id: e.target.value })}>
              <option value="">— None —</option>
              {suppliers.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ))}
            </select>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="label">Quantity Received</label>
              <input
                className="input"
                type="number"
                min={1}
                value={form.quantity}
                onChange={(e) => setForm({ ...form, quantity: e.target.value })}
                required
              />
            </div>
            <div>
              <label className="label">Cost per Unit (Rp)</label>
              <input
                className="input"
                type="number"
                min={0}
                value={form.cost_per_unit}
                onChange={(e) => setForm({ ...form, cost_per_unit: e.target.value })}
                required
              />
            </div>
          </div>
          <div>
            <label className="label">Notes (optional)</label>
            <textarea className="input" value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
          </div>

          {error && <p className="text-sm text-red-600">{error}</p>}
          {success && <p className="text-sm text-green-600">{success}</p>}

          <button type="submit" className="btn-primary w-full">
            Submit Restock
          </button>
          <p className="text-xs text-koperasi-400">
            This is logged under your name and timestamp for accountability, and adds directly to inventory.
          </p>
        </form>
      </div>

      <div>
        <h2 className="font-semibold text-koperasi-800 mb-2">My Recent Restocks</h2>
        {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}
        <DataTable endpoint="/restocking-records" columns={columns} onRowClick={(r) => setDetail({ entity: 'restock', id: r.id })} searchable={false} extraParams="&mine_only=1" reloadKey={reloadKey} />
      </div>
    </div>
  );
}
