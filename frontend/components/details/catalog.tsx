'use client';

import { useEffect, useState } from 'react';
import { apiFetch, storageUrl } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { Item, ItemCategory, Paginated, RestockingRecord } from '@/lib/types';
import type { DetailTarget } from './DetailModal';
import { Drill, EmptyNote, Field, Section, formatDate, formatRp } from './ui';

function Loading() {
  const { t } = useLanguage();
  return (
    <div className="space-y-2" aria-label={t('details.loading')}>
      {Array.from({ length: 6 }).map((_, i) => (
        <div key={i} className="skeleton h-5 rounded" />
      ))}
    </div>
  );
}

function Failed({ message }: { message: string }) {
  return (
    <p className="text-sm text-red-600" role="alert">
      {message}
    </p>
  );
}

export function ItemDetails({ id, navigate }: { id: number; navigate: (t: DetailTarget) => void }) {
  const [item, setItem] = useState<Item | null>(null);
  const [restocks, setRestocks] = useState<RestockingRecord[]>([]);
  const [error, setError] = useState<string | null>(null);
  const { t } = useLanguage();

  useEffect(() => {
    let cancelled = false;
    setItem(null);
    setError(null);
    apiFetch<Item>(`/items/${id}`)
      .then((res) => {
        if (cancelled) return;
        setItem(res);
        return apiFetch<Paginated<RestockingRecord>>(`/restocking-records?item_id=${id}&per_page=5`);
      })
      .then((res) => {
        if (!cancelled && res) setRestocks(res.data);
      })
      .catch((e) => {
        if (!cancelled) setError(e.message || t('details.failedItem'));
      });
    return () => {
      cancelled = true;
    };
  }, [id, t]);

  if (error) return <Failed message={error} />;
  if (!item) return <Loading />;

  const lowStock = item.current_stock <= item.min_stock_threshold;

  return (
    <div>
      <div className="flex items-center gap-3">
        {item.image_path && (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={storageUrl(item.image_path)}
            alt={item.name}
            width={56}
            height={56}
            loading="lazy"
            className="w-14 h-14 rounded-lg object-cover shrink-0"
          />
        )}
        <div className="min-w-0">
          <div className="font-bold text-lg text-koperasi-800 truncate">{item.name}</div>
          <div className="text-xs text-koperasi-400">
            {item.sku}
            {item.barcode ? ` · ${item.barcode}` : ''}
          </div>
        </div>
        <span className={`badge ml-auto shrink-0 ${item.is_active ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
          {item.is_active ? t('details.active') : t('details.inactive')}
        </span>
      </div>

      <Section title={t('details.pricingStock')}>
        <Field label={t('details.sellingPrice')}>{formatRp(item.unit_price)}</Field>
        <Field label={t('details.costPrice')}>{formatRp(item.cost_price)}</Field>
        <Field label={t('details.unit')}>{item.unit_of_measure}</Field>
        <Field label={t('details.currentStock')}>
          <span className={lowStock ? 'text-red-600 font-bold' : ''}>
            {item.current_stock}
            {lowStock ? ` ${t('details.lowSuffix')}` : ''}
          </span>
        </Field>
        <Field label={t('details.lowThreshold')}>{item.min_stock_threshold}</Field>
        <Field label={t('details.expiry')}>{formatDate(item.expiry_date)}</Field>
      </Section>

      <Section title={t('details.categorySection')}>
        {item.category ? (
          <Drill onOpen={() => navigate({ entity: 'category', id: item.category!.id })}>{item.category.name}</Drill>
        ) : (
          <EmptyNote>{t('details.uncategorized')}</EmptyNote>
        )}
      </Section>

      <Section title={t('details.recentRestocks')}>
        {restocks.length === 0 ? (
          <EmptyNote>{t('details.noRestocksItem')}</EmptyNote>
        ) : (
          <ul className="divide-y divide-koperasi-50">
            {restocks.map((r) => (
              <li key={r.id} className="py-2 flex justify-between gap-2 text-sm">
                <button
                  type="button"
                  className="text-left hover:underline"
                  onClick={() => navigate({ entity: 'restock', id: r.id })}
                >
                  <span className="font-medium text-koperasi-800">+{r.quantity}</span>{' '}
                  <span className="text-koperasi-500">· {new Date(r.restocked_at).toLocaleDateString('id-ID')}</span>
                </button>
                <span className="text-koperasi-500 tabular-nums shrink-0">{formatRp(r.total_cost)}</span>
              </li>
            ))}
          </ul>
        )}
      </Section>
    </div>
  );
}

export function CategoryDetails({ id, navigate }: { id: number; navigate: (t: DetailTarget) => void }) {
  const [category, setCategory] = useState<(ItemCategory & { items_count?: number }) | null>(null);
  const [items, setItems] = useState<Item[]>([]);
  const [itemsTotal, setItemsTotal] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const { t } = useLanguage();

  useEffect(() => {
    let cancelled = false;
    setCategory(null);
    setError(null);
    apiFetch<ItemCategory & { items_count?: number }>(`/item-categories/${id}`)
      .then((res) => {
        if (cancelled) return;
        setCategory(res);
        return apiFetch<Paginated<Item>>(`/items?category_id=${id}&per_page=50`);
      })
      .then((res) => {
        if (!cancelled && res) {
          setItems(res.data);
          setItemsTotal(res.total);
        }
      })
      .catch((e) => {
        if (!cancelled) setError(e.message || t('details.failedCategory'));
      });
    return () => {
      cancelled = true;
    };
  }, [id, t]);

  if (error) return <Failed message={error} />;
  if (!category) return <Loading />;

  return (
    <div>
      <div className="font-bold text-lg text-koperasi-800">{category.name}</div>
      {category.description && <p className="text-sm text-koperasi-500 mt-1">{category.description}</p>}

      <Section title={t('details.itemsInCategory', { count: itemsTotal })}>
        {items.length === 0 ? (
          <EmptyNote>{t('details.noItemsCategory')}</EmptyNote>
        ) : (
          <ul className="divide-y divide-koperasi-50">
            {items.map((i) => (
              <li key={i.id}>
                <button type="button" className="w-full py-2 flex justify-between gap-2 text-sm text-left hover:bg-koperasi-50/50 rounded px-1 -mx-1" onClick={() => navigate({ entity: 'item', id: i.id })}>
                  <span className="min-w-0">
                    <span className="font-medium text-koperasi-800 block truncate">{i.name}</span>
                    <span className="text-xs text-koperasi-400">
                      {i.sku} · {t('details.stockOf', { count: i.current_stock })}
                    </span>
                  </span>
                  <span className="text-koperasi-600 tabular-nums shrink-0">{formatRp(i.unit_price)}</span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </Section>
    </div>
  );
}
