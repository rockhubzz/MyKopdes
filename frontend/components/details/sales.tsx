'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';
import type { Discount, RestockingRecord, Transaction } from '@/lib/types';
import type { DetailTarget } from './DetailModal';
import { Drill, EmptyNote, Field, Section, formatDate, formatDateTime, formatRp } from './ui';

function Loading() {
  return (
    <div className="space-y-2" aria-label="Loading details">
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

  useEffect(() => {
    let cancelled = false;
    setTrx(null);
    setError(null);
    apiFetch<Transaction>(`${endpointBase}/${id}`)
      .then((res) => {
        if (!cancelled) setTrx(res);
      })
      .catch((e) => {
        if (!cancelled) setError(e.message || 'Failed to load transaction.');
      });
    return () => {
      cancelled = true;
    };
  }, [id, endpointBase]);

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
          {trx.payment_status}
        </span>
      </div>
      <div className="text-xs text-koperasi-400">{formatDateTime(trx.created_at)}</div>

      <Section title="Lines">
        {!trx.items || trx.items.length === 0 ? (
          <EmptyNote>No line items recorded.</EmptyNote>
        ) : (
          <ul className="divide-y divide-koperasi-50">
            {trx.items.map((line) => (
              <li key={line.id} className="py-2 flex justify-between gap-2 text-sm">
                <button
                  type="button"
                  className="text-left hover:underline min-w-0"
                  onClick={() => line.item_id && navigate({ entity: 'item', id: line.item_id })}
                >
                  <span className="font-medium text-koperasi-800 block truncate">{line.item?.name ?? `Item #${line.item_id}`}</span>
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

      <Section title="Totals">
        <Field label="Subtotal">{formatRp(trx.subtotal)}</Field>
        <Field label={trx.discount ? `Discount (${trx.discount.name})` : 'Discount'}>
          − {formatRp(trx.discount_amount)}
        </Field>
        <Field label="Tax">+ {formatRp(trx.tax_amount)}</Field>
        <Field label="Total">
          <span className="font-bold">{formatRp(trx.total)}</span>
        </Field>
      </Section>

      <Section title="Parties">
        <Field label="Cashier">{trx.cashier?.name ?? `Staff #${trx.cashier_id}`}</Field>
        <Field label="Member">
          {trx.member ? (
            <Drill onOpen={() => trx.member_id && navigate({ entity: 'member', id: trx.member_id })}>{trx.member.name}</Drill>
          ) : (
            'Walk-in'
          )}
        </Field>
        <Field label="Payment">
          <span className="capitalize">{trx.payment_method.replace('_', ' ')}</span>
        </Field>
        {trx.discount && (
          <Field label="Discount rule">
            <Drill onOpen={() => trx.discount_id && navigate({ entity: 'discount', id: trx.discount_id })}>
              {trx.discount.name}
            </Drill>
          </Field>
        )}
      </Section>
    </div>
  );
}

function describeDiscount(d: Discount): string {
  const cat = d.category ? ` on ${d.category.name}` : '';
  switch (d.type) {
    case 'percentage':
      return `${Number(d.value)}% off the subtotal${cat}.`;
    case 'flat':
      return `${formatRp(d.value)} off the subtotal${cat} (never more than the subtotal itself).`;
    case 'buy_x_get_y':
      return `Buy ${d.buy_qty}, get ${d.get_qty} free${cat ? ` — ${d.category!.name} items only` : ''}. Applied per full group automatically.`;
  }
}

export function DiscountDetails({ id, navigate }: { id: number; navigate: (t: DetailTarget) => void }) {
  const [discount, setDiscount] = useState<Discount | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setDiscount(null);
    setError(null);
    apiFetch<Discount>(`/discounts/${id}`)
      .then((res) => {
        if (!cancelled) setDiscount(res);
      })
      .catch((e) => {
        if (!cancelled) setError(e.message || 'Failed to load discount.');
      });
    return () => {
      cancelled = true;
    };
  }, [id]);

  if (error) return <Failed message={error} />;
  if (!discount) return <Loading />;

  return (
    <div>
      <div className="flex items-center gap-2">
        <div className="font-bold text-lg text-koperasi-800">{discount.name}</div>
        <span className={`badge ml-auto shrink-0 ${discount.is_active ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
          {discount.is_active ? 'Active' : 'Inactive'}
        </span>
      </div>
      {discount.description && <p className="text-sm text-koperasi-500 mt-1">{discount.description}</p>}

      <Section title="How it applies">
        <p className="text-sm text-koperasi-800 bg-koperasi-50 rounded-lg p-3">{describeDiscount(discount)}</p>
      </Section>

      <Section title="Rules">
        <Field label="Type">
          <span className="capitalize">{discount.type.replace(/_/g, ' ')}</span>
        </Field>
        <Field label="Scope">
          <span className="badge bg-koperasi-100 text-koperasi-700">
            {discount.scope === 'member' ? 'Members only' : 'Everyone'}
          </span>
        </Field>
        {discount.min_purchase != null && Number(discount.min_purchase) > 0 && (
          <Field label="Minimum purchase">{formatRp(discount.min_purchase)}</Field>
        )}
        {discount.category && (
          <Field label="Category">
            <Drill onOpen={() => discount.category_id && navigate({ entity: 'category', id: discount.category_id })}>
              {discount.category.name}
            </Drill>
          </Field>
        )}
        <Field label="Valid from">{formatDate(discount.starts_at)}</Field>
        <Field label="Valid until">{formatDate(discount.ends_at)}</Field>
      </Section>

      <Section title="Checkout behavior">
        <p className="text-xs text-koperasi-400">
          When several discounts could apply, the register automatically picks whichever saves the customer the most —
          unless the cashier explicitly selects this one.
        </p>
      </Section>
    </div>
  );
}

export function RestockDetails({ id, navigate }: { id: number; navigate: (t: DetailTarget) => void }) {
  const [record, setRecord] = useState<RestockingRecord | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setRecord(null);
    setError(null);
    apiFetch<RestockingRecord>(`/restocking-records/${id}`)
      .then((res) => {
        if (!cancelled) setRecord(res);
      })
      .catch((e) => {
        if (!cancelled) setError(e.message || 'Failed to load restock record.');
      });
    return () => {
      cancelled = true;
    };
  }, [id]);

  if (error) return <Failed message={error} />;
  if (!record) return <Loading />;

  return (
    <div>
      <div className="font-bold text-lg text-koperasi-800">
        +{record.quantity} × {record.item?.name ?? `Item #${record.item_id}`}
      </div>
      <div className="text-xs text-koperasi-400">{formatDateTime(record.restocked_at)}</div>

      <Section title="Details">
        <Field label="Item">
          <Drill onOpen={() => navigate({ entity: 'item', id: record.item_id })}>{record.item?.name ?? `#${record.item_id}`}</Drill>
        </Field>
        <Field label="Supplier">
          {record.supplier ? (
            <Drill onOpen={() => record.supplier_id && navigate({ entity: 'supplier', id: record.supplier_id })}>
              {record.supplier.name}
            </Drill>
          ) : (
            '—'
          )}
        </Field>
        <Field label="Cost per unit">{formatRp(record.cost_per_unit)}</Field>
        <Field label="Total cost">{formatRp(record.total_cost)}</Field>
        <Field label="Submitted by">{record.submittedBy?.name ?? `Staff #${record.submitted_by}`}</Field>
        {record.notes && <Field label="Notes">{record.notes}</Field>}
      </Section>
    </div>
  );
}
