'use client';

import { useEffect, useState } from 'react';
import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import { apiFetch } from '@/lib/api';
import { useDebouncedValue } from '@/lib/useDebouncedValue';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { Item, Paginated, RestockingRecord, Supplier } from '@/lib/types';

/**
 * Searchable picker: text input with a server-searched dropdown. Once an
 * option is picked it collapses to a chip with a clear button.
 */
function SearchSelect<T extends { id: number }>({
  inputId,
  placeholder,
  ariaLabel,
  query,
  onQueryChange,
  results,
  renderResult,
  selected,
  renderSelected,
  onSelect,
  onClear,
  clearLabel,
  emptyText,
}: {
  inputId: string;
  placeholder: string;
  ariaLabel: string;
  query: string;
  onQueryChange: (q: string) => void;
  results: T[];
  renderResult: (item: T) => React.ReactNode;
  selected: T | null;
  renderSelected: (item: T) => React.ReactNode;
  onSelect: (item: T) => void;
  onClear: () => void;
  clearLabel: string;
  emptyText: string;
}) {
  const [open, setOpen] = useState(false);

  if (selected) {
    return (
      <div className="input flex items-center justify-between gap-2">
        <span className="text-sm truncate">{renderSelected(selected)}</span>
        <button type="button" onClick={onClear} className="text-xs text-red-500 hover:underline shrink-0">
          {clearLabel}
        </button>
      </div>
    );
  }

  return (
    <div className="relative">
      <input
        id={inputId}
        className="input"
        role="combobox"
        aria-expanded={open}
        aria-autocomplete="list"
        aria-label={ariaLabel}
        placeholder={placeholder}
        autoComplete="off"
        value={query}
        onChange={(e) => {
          onQueryChange(e.target.value);
          setOpen(true);
        }}
        onFocus={() => setOpen(true)}
        onBlur={() => setOpen(false)}
        onKeyDown={(e) => {
          if (e.key === 'Escape') setOpen(false);
        }}
      />
      {open && (
        <div className="absolute z-10 bg-white border border-koperasi-200 rounded-lg shadow-lg w-full mt-1 max-h-64 overflow-y-auto" role="listbox">
          {results.length === 0 ? (
            <div className="px-3 py-2 text-sm text-koperasi-400">{emptyText}</div>
          ) : (
            results.map((item) => (
              <button
                key={item.id}
                type="button"
                role="option"
                aria-selected="false"
                className="w-full text-left px-3 py-2 text-sm hover:bg-koperasi-50 flex justify-between gap-2"
                // Keep focus (and the dropdown) alive until the click registers.
                onMouseDown={(e) => e.preventDefault()}
                onClick={() => {
                  onSelect(item);
                  setOpen(false);
                }}
              >
                {renderResult(item)}
              </button>
            ))
          )}
        </div>
      )}
    </div>
  );
}

interface QueuedRestock {
  key: number;
  item: Item;
  supplier: Supplier | null;
  quantity: number;
  costPerUnit: number;
  notes: string;
}

let queueKey = 0;

export default function RestockPage() {
  // Initial pools double as the default dropdown contents; typing 2+ chars
  // switches to server-side search (abortable, like the cashier lookup).
  const [items, setItems] = useState<Item[]>([]);
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [itemQuery, setItemQuery] = useState('');
  const [supplierQuery, setSupplierQuery] = useState('');
  const [itemResults, setItemResults] = useState<Item[]>([]);
  const [supplierResults, setSupplierResults] = useState<Supplier[]>([]);
  const [selectedItem, setSelectedItem] = useState<Item | null>(null);
  const [selectedSupplier, setSelectedSupplier] = useState<Supplier | null>(null);
  const [form, setForm] = useState({ quantity: '', cost_per_unit: '', notes: '' });
  const [queue, setQueue] = useState<QueuedRestock[]>([]);
  const [entryError, setEntryError] = useState<string | null>(null);
  const [queueError, setQueueError] = useState<string | null>(null);
  const [queueSuccess, setQueueSuccess] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [reloadKey, setReloadKey] = useState(0);
  const [detail, setDetail] = useState<DetailTarget | null>(null);
  const { t } = useLanguage();

  useEffect(() => {
    apiFetch<Paginated<Item>>('/items?per_page=200').then((res) => setItems(res.data));
    apiFetch<Paginated<Supplier>>('/suppliers?per_page=100').then((res) => setSuppliers(res.data));
  }, []);

  const debouncedItemQuery = useDebouncedValue(itemQuery, 250);
  const debouncedSupplierQuery = useDebouncedValue(supplierQuery, 250);

  useEffect(() => {
    const q = debouncedItemQuery.trim();
    if (q.length < 2) {
      setItemResults(items);
      return;
    }
    const controller = new AbortController();
    apiFetch<Paginated<Item>>(`/items?search=${encodeURIComponent(q)}&per_page=8`, {
      signal: controller.signal,
    })
      .then((res) => setItemResults(res.data))
      .catch(() => {
        // Fall back to filtering the preloaded pool so search never dead-ends.
        if (!controller.signal.aborted) {
          const ql = q.toLowerCase();
          setItemResults(items.filter((i) => i.name.toLowerCase().includes(ql) || i.sku.toLowerCase().includes(ql)));
        }
      });
    return () => controller.abort();
  }, [debouncedItemQuery, items]);

  useEffect(() => {
    const q = debouncedSupplierQuery.trim();
    if (q.length < 2) {
      setSupplierResults(suppliers);
      return;
    }
    const controller = new AbortController();
    apiFetch<Paginated<Supplier>>(`/suppliers?search=${encodeURIComponent(q)}&per_page=8`, {
      signal: controller.signal,
    })
      .then((res) => setSupplierResults(res.data))
      .catch(() => {
        if (!controller.signal.aborted) {
          const ql = q.toLowerCase();
          setSupplierResults(suppliers.filter((s) => s.name.toLowerCase().includes(ql)));
        }
      });
    return () => controller.abort();
  }, [debouncedSupplierQuery, suppliers]);

  function handleAddToQueue(e: React.FormEvent) {
    e.preventDefault();
    setEntryError(null);
    if (!selectedItem) {
      setEntryError(t('restock.itemRequired'));
      return;
    }
    const quantity = Number(form.quantity);
    const costPerUnit = Number(form.cost_per_unit);
    if (!Number.isInteger(quantity) || quantity < 1 || form.cost_per_unit === '' || Number.isNaN(costPerUnit) || costPerUnit < 0) {
      setEntryError(t('restock.qtyInvalid'));
      return;
    }
    setQueue((prev) => [
      ...prev,
      {
        key: ++queueKey,
        item: selectedItem,
        supplier: selectedSupplier,
        quantity,
        costPerUnit,
        notes: form.notes.trim(),
      },
    ]);
    // Reset the entry but keep the supplier — consecutive lines usually come
    // from the same supplier.
    setSelectedItem(null);
    setItemQuery('');
    setForm({ quantity: '', cost_per_unit: '', notes: '' });
  }

  async function handleSubmitQueue() {
    if (queue.length === 0 || submitting) return;
    setSubmitting(true);
    setQueueError(null);
    setQueueSuccess(null);
    const snapshot = queue;
    const failedKeys = new Set<number>();
    const failedNames: string[] = [];
    let ok = 0;
    // Sequential posts: each one updates stock immediately, and a failure
    // never blocks the remaining lines.
    for (const line of snapshot) {
      try {
        await apiFetch('/restocking-records', {
          method: 'POST',
          body: {
            item_id: line.item.id,
            supplier_id: line.supplier ? line.supplier.id : null,
            quantity: line.quantity,
            cost_per_unit: line.costPerUnit,
            notes: line.notes || null,
          },
        });
        ok += 1;
      } catch (err) {
        failedKeys.add(line.key);
        failedNames.push(line.item.name);
      }
    }
    // Succeeded lines leave the queue; failed ones stay for retry/removal.
    setQueue((prev) => prev.filter((line) => failedKeys.has(line.key)));
    if (failedNames.length === 0) {
      setQueueSuccess(t('restock.queueSuccess', { count: ok }));
    } else {
      setQueueError(t('restock.queuePartial', { ok, count: snapshot.length, names: failedNames.join(', ') }));
    }
    setReloadKey((k) => k + 1);
    setSubmitting(false);
  }

  const queueTotal = queue.reduce((sum, line) => sum + line.quantity * line.costPerUnit, 0);

  const columns: Column<RestockingRecord>[] = [
    { header: t('restock.colItem'), render: (r) => r.item?.name ?? `#${r.item_id}` },
    { header: t('restock.colQty'), render: (r) => `+${r.quantity}` },
    { header: t('restock.colCost'), render: (r) => `Rp ${Number(r.cost_per_unit).toLocaleString('id-ID')}` },
    { header: t('restock.colSupplier'), render: (r) => r.supplier?.name ?? '—' },
    { header: t('restock.colDate'), render: (r) => new Date(r.restocked_at).toLocaleString('id-ID') },
  ];

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-koperasi-800">{t('restock.title')}</h1>

      <div className="grid lg:grid-cols-2 gap-6 items-start">
        <div className="card">
          <h2 className="font-semibold text-koperasi-800 mb-3">{t('restock.entryTitle')}</h2>
          <form onSubmit={handleAddToQueue} className="space-y-3">
            <div>
              <label className="label" htmlFor="restock-item-search">{t('restock.item')}</label>
              <SearchSelect<Item>
                inputId="restock-item-search"
                placeholder={t('restock.searchItem')}
                ariaLabel={t('restock.item')}
                query={itemQuery}
                onQueryChange={setItemQuery}
                results={itemResults}
                renderResult={(i) => (
                  <>
                    <span className="min-w-0 truncate">{i.name}</span>
                    <span className="text-koperasi-400 shrink-0 tabular-nums">
                      Rp {Number(i.unit_price).toLocaleString('id-ID')} · {t('details.stockOf', { count: i.current_stock })}
                    </span>
                  </>
                )}
                selected={selectedItem}
                renderSelected={(i) => t('restock.currentStock', { name: i.name, count: i.current_stock, unit: i.unit_of_measure })}
                onSelect={(i) => {
                  setSelectedItem(i);
                  setItemQuery('');
                }}
                onClear={() => setSelectedItem(null)}
                clearLabel={t('common.clear')}
                emptyText={t('restock.noItemsFound')}
              />
            </div>
            <div>
              <label className="label" htmlFor="restock-supplier-search">{t('restock.supplier')}</label>
              <SearchSelect<Supplier>
                inputId="restock-supplier-search"
                placeholder={t('restock.searchSupplier')}
                ariaLabel={t('restock.supplier')}
                query={supplierQuery}
                onQueryChange={setSupplierQuery}
                results={supplierResults}
                renderResult={(s) => (
                  <>
                    <span className="min-w-0 truncate">{s.name}</span>
                    <span className="text-koperasi-400 shrink-0">{s.contact_person ?? s.phone ?? ''}</span>
                  </>
                )}
                selected={selectedSupplier}
                renderSelected={(s) => s.name}
                onSelect={(s) => {
                  setSelectedSupplier(s);
                  setSupplierQuery('');
                }}
                onClear={() => setSelectedSupplier(null)}
                clearLabel={t('common.clear')}
                emptyText={t('restock.noSuppliersFound')}
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="label">{t('restock.qtyReceived')}</label>
                <input
                  className="input"
                  type="number"
                  min={1}
                  step={1}
                  value={form.quantity}
                  onChange={(e) => setForm({ ...form, quantity: e.target.value })}
                  required
                />
              </div>
              <div>
                <label className="label">{t('restock.costPerUnit')}</label>
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
              <label className="label">{t('restock.notesOpt')}</label>
              <textarea className="input" value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
            </div>

            {entryError && <p className="text-sm text-red-600">{entryError}</p>}

            <button type="submit" className="btn-secondary w-full">
              {t('restock.queueAdd')}
            </button>
          </form>
        </div>

        <div className="card">
          <h2 className="font-semibold text-koperasi-800 mb-3">{t('restock.queueTitle', { count: queue.length })}</h2>
          {queue.length === 0 ? (
            <p className="text-sm text-koperasi-400">{t('restock.queueEmpty')}</p>
          ) : (
            <ul className="divide-y divide-koperasi-50">
              {queue.map((line) => (
                <li key={line.key} className="py-2 flex justify-between gap-3 text-sm">
                  <div className="min-w-0">
                    <div className="font-medium text-koperasi-800 truncate">{line.item.name}</div>
                    <div className="text-xs text-koperasi-500 tabular-nums">
                      +{line.quantity} · Rp {line.costPerUnit.toLocaleString('id-ID')}/{t('details.unit').toLowerCase()} ·{' '}
                      {line.supplier?.name ?? '—'}
                      {line.notes && <span className="block truncate text-koperasi-400">{line.notes}</span>}
                    </div>
                  </div>
                  <div className="text-right shrink-0">
                    <div className="font-medium tabular-nums">Rp {(line.quantity * line.costPerUnit).toLocaleString('id-ID')}</div>
                    <button
                      type="button"
                      aria-label={t('restock.queueRemoveAria', { name: line.item.name })}
                      className="text-xs text-red-500 hover:underline"
                      onClick={() => setQueue((prev) => prev.filter((l) => l.key !== line.key))}
                    >
                      {t('restock.queueRemove')}
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          )}

          {queue.length > 0 && (
            <div className="flex justify-between text-sm font-medium pt-2 mt-1 border-t border-koperasi-100 tabular-nums">
              <span>{t('restock.queueTotal')}</span>
              <span>Rp {queueTotal.toLocaleString('id-ID')}</span>
            </div>
          )}

          {queueError && <p className="text-sm text-red-600 mt-2">{queueError}</p>}
          {queueSuccess && <p className="text-sm text-green-600 mt-2">{queueSuccess}</p>}

          <button
            type="button"
            className="btn-primary w-full mt-3"
            onClick={handleSubmitQueue}
            disabled={queue.length === 0 || submitting}
          >
            {submitting ? t('restock.submitting') : t('restock.queueSubmit', { count: queue.length })}
          </button>
          <p className="text-xs text-koperasi-400 mt-2">
            {t('restock.footnote')}
          </p>
        </div>
      </div>

      <div>
        <h2 className="font-semibold text-koperasi-800 mb-2">{t('restock.recentTitle')}</h2>
        {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}
        <DataTable endpoint="/restocking-records" columns={columns} onRowClick={(r) => setDetail({ entity: 'restock', id: r.id })} searchable={false} extraParams="&mine_only=1" reloadKey={reloadKey} />
      </div>
    </div>
  );
}
