'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';
import { useLanguage, type TKey, type Params } from '@/lib/i18n/LanguageContext';
import type { Discount, RestockingRecord, Transaction } from '@/lib/types';
import type { DetailTarget } from './DetailModal';
import { Drill, EmptyNote, Field, Section, formatDate, formatDateTime, formatRp } from './ui';

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

export function TransactionDetails({
  id,
  endpointBase,
  navigate,
}: {
  id: number;
  endpointBase: string;
  navigate: (t: DetailTarget) => void;
}) {
  const [trx, setTrx] = useState<Transaction | null>(null);
  const [error, setError] = useState<string | null>(null);
  const { t, tx } = useLanguage();

  useEffect(() => {
    let cancelled = false;
    setTrx(null);
    setError(null);
    apiFetch<Transaction>(`${endpointBase}/${id}`)
      .then((res) => {
        if (!cancelled) setTrx(res);
      })
      .catch((e) => {
        if (!cancelled) setError(e.message || t('details.failedTransaction'));
      });
    return () => {
      cancelled = true;
    };
  }, [id, endpointBase, t]);

  if (error) return <Failed message={error} />;
  if (!trx) return <Loading />;

  return (
    <div>
      <div className="flex items-center gap-2">
        <div className="font-bold text-lg text-koperasi-800 font-mono">{trx.transaction_code}</div>
        <span
          className={`badge ml-auto shrink-0 ${
            trx.payment_status === 'paid'
              ? 'bg-green-100 text-green-700'
              : trx.payment_status === 'void'
                ? 'bg-red-100 text-red-700'
                : 'bg-yellow-100 text-yellow-700'
          }`}
        >
          {tx(`status.${trx.payment_status}`, trx.payment_status)}
        </span>
      </div>
      <div className="text-xs text-koperasi-400">{formatDateTime(trx.created_at)}</div>

      <Section title={t('details.lines')}>
        {!trx.items || trx.items.length === 0 ? (
          <EmptyNote>{t('details.noLines')}</EmptyNote>
        ) : (
          <ul className="divide-y divide-koperasi-50">
            {trx.items.map((line) => (
              <li key={line.id} className="py-2 flex justify-between gap-2 text-sm">
                <button
                  type="button"
                  className="text-left hover:underline min-w-0"
                  onClick={() => line.item_id && navigate({ entity: 'item', id: line.item_id })}
                >
                  <span className="font-medium text-koperasi-800 block truncate">{line.item?.name ?? t('details.itemHash', { id: line.item_id })}</span>
                  <span className="text-xs text-koperasi-400">
                    {line.quantity} × {formatRp(line.unit_price)}
                  </span>
                </button>
                <span className="tabular-nums shrink-0">{formatRp(line.subtotal)}</span>
              </li>
            ))}
          </ul>
        )}
      </Section>

      <Section title={t('details.totals')}>
        <Field label={t('details.subtotal')}>{formatRp(trx.subtotal)}</Field>
        <Field label={trx.discount ? t('details.discountWith', { name: trx.discount.name }) : t('details.discount')}>
          − {formatRp(trx.discount_amount)}
        </Field>
        <Field label={t('details.tax')}>+ {formatRp(trx.tax_amount)}</Field>
        <Field label={t('details.total')}>
          <span className="font-bold">{formatRp(trx.total)}</span>
        </Field>
      </Section>

      <Section title={t('details.parties')}>
        <Field label={t('details.cashier')}>{trx.cashier?.name ?? t('details.staffHash', { id: trx.cashier_id })}</Field>
        <Field label={t('details.member')}>
          {trx.member ? (
            <Drill onOpen={() => trx.member_id && navigate({ entity: 'member', id: trx.member_id })}>{trx.member.name}</Drill>
          ) : (
            t('details.walkIn')
          )}
        </Field>
        <Field label={t('details.payment')}>
          <span className="capitalize">{tx(`pay.${trx.payment_method}`, trx.payment_method.replace('_', ' '))}</span>
        </Field>
        {trx.discount && (
          <Field label={t('details.discountRule')}>
            <Drill onOpen={() => trx.discount_id && navigate({ entity: 'discount', id: trx.discount_id })}>
              {trx.discount.name}
            </Drill>
          </Field>
        )}
      </Section>
    </div>
  );
}

function describeDiscount(d: Discount, t: (key: TKey, params?: Params) => string): string {
  const cat = d.category ? t('details.onCategory', { name: d.category.name }) : '';
  switch (d.type) {
    case 'percentage':
      return t('details.discountPct', { value: Number(d.value), cat });
    case 'flat':
      return t('details.discountFlat', { value: formatRp(d.value), cat });
    case 'buy_x_get_y':
      return t('details.discountBxgy', {
        x: d.buy_qty ?? 0,
        y: d.get_qty ?? 0,
        cat: cat ? ` — ${t('details.catOnly', { name: d.category!.name })}` : '',
      });
  }
}

export function DiscountDetails({ id, navigate }: { id: number; navigate: (t: DetailTarget) => void }) {
  const [discount, setDiscount] = useState<Discount | null>(null);
  const [error, setError] = useState<string | null>(null);
  const { t, tx } = useLanguage();

  useEffect(() => {
    let cancelled = false;
    setDiscount(null);
    setError(null);
    apiFetch<Discount>(`/discounts/${id}`)
      .then((res) => {
        if (!cancelled) setDiscount(res);
      })
      .catch((e) => {
        if (!cancelled) setError(e.message || t('details.failedDiscount'));
      });
    return () => {
      cancelled = true;
    };
  }, [id, t]);

  if (error) return <Failed message={error} />;
  if (!discount) return <Loading />;

  return (
    <div>
      <div className="flex items-center gap-2">
        <div className="font-bold text-lg text-koperasi-800">{discount.name}</div>
        <span className={`badge ml-auto shrink-0 ${discount.is_active ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
          {discount.is_active ? t('details.active') : t('details.inactive')}
        </span>
      </div>
      {discount.description && <p className="text-sm text-koperasi-500 mt-1">{discount.description}</p>}

      <Section title={t('details.howItApplies')}>
        <p className="text-sm text-koperasi-800 bg-koperasi-50 rounded-lg p-3">{describeDiscount(discount, t)}</p>
      </Section>

      <Section title={t('details.rules')}>
        <Field label={t('details.type')}>
          <span className="capitalize">{tx(`dtype.${discount.type}`, discount.type.replace(/_/g, ' '))}</span>
        </Field>
        <Field label={t('details.scope')}>
          <span className="badge bg-koperasi-100 text-koperasi-700">
            {discount.scope === 'member' ? t('details.membersOnly') : t('details.everyone')}
          </span>
        </Field>
        {discount.min_purchase != null && Number(discount.min_purchase) > 0 && (
          <Field label={t('details.minPurchase')}>{formatRp(discount.min_purchase)}</Field>
        )}
        {discount.category && (
          <Field label={t('details.category')}>
            <Drill onOpen={() => discount.category_id && navigate({ entity: 'category', id: discount.category_id })}>
              {discount.category.name}
            </Drill>
          </Field>
        )}
        <Field label={t('details.validFrom')}>{formatDate(discount.starts_at)}</Field>
        <Field label={t('details.validUntil')}>{formatDate(discount.ends_at)}</Field>
      </Section>

      <Section title={t('details.checkoutBehavior')}>
        <p className="text-xs text-koperasi-400">
          {t('details.checkoutBehaviorText')}
        </p>
      </Section>
    </div>
  );
}

export function RestockDetails({ id, navigate }: { id: number; navigate: (t: DetailTarget) => void }) {
  const [record, setRecord] = useState<RestockingRecord | null>(null);
  const [error, setError] = useState<string | null>(null);
  const { t } = useLanguage();

  useEffect(() => {
    let cancelled = false;
    setRecord(null);
    setError(null);
    apiFetch<RestockingRecord>(`/restocking-records/${id}`)
      .then((res) => {
        if (!cancelled) setRecord(res);
      })
      .catch((e) => {
        if (!cancelled) setError(e.message || t('details.failedRestock'));
      });
    return () => {
      cancelled = true;
    };
  }, [id, t]);

  if (error) return <Failed message={error} />;
  if (!record) return <Loading />;

  // See the `submitted_by` note in lib/types.ts: the staff object may arrive
  // under either key depending on how the backend serialized the relation.
  const submitter =
    record.submittedBy ??
    (typeof record.submitted_by === 'object' && record.submitted_by !== null ? record.submitted_by : null);

  return (
    <div>
      <div className="font-bold text-lg text-koperasi-800">
        +{record.quantity} × {record.item?.name ?? t('details.itemHash', { id: record.item_id })}
      </div>
      <div className="text-xs text-koperasi-400">{formatDateTime(record.restocked_at)}</div>

      <Section title={t('details.detailsSection')}>
        <Field label={t('details.item')}>
          <Drill onOpen={() => navigate({ entity: 'item', id: record.item_id })}>{record.item?.name ?? `#${record.item_id}`}</Drill>
        </Field>
        <Field label={t('details.supplier')}>
          {record.supplier ? (
            <Drill onOpen={() => record.supplier_id && navigate({ entity: 'supplier', id: record.supplier_id })}>
              {record.supplier.name}
            </Drill>
          ) : (
            '—'
          )}
        </Field>
        <Field label={t('details.costPerUnit')}>{formatRp(record.cost_per_unit)}</Field>
        <Field label={t('details.totalCost')}>{formatRp(record.total_cost)}</Field>
        <Field label={t('details.submittedBy')}>
          {submitter?.name ?? (typeof record.submitted_by === 'number' ? t('details.staffHash', { id: record.submitted_by }) : '—')}
        </Field>
        {record.notes && <Field label={t('details.notes')}>{record.notes}</Field>}
      </Section>
    </div>
  );
}
