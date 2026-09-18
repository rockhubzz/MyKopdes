'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';
import type { AuditLog, Member, Paginated, RestockingRecord, StaffUser, Supplier, Transaction } from '@/lib/types';
import type { DetailTarget } from './DetailModal';
import { EmptyNote, Field, Section, formatDate, formatDateTime, formatRp } from './ui';

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

type SupplierWithRecords = Supplier & { restockingRecords?: RestockingRecord[] };

export function SupplierDetails({ id, navigate }: { id: number; navigate: (t: DetailTarget) => void }) {
  const [supplier, setSupplier] = useState<SupplierWithRecords | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setSupplier(null);
    setError(null);
    apiFetch<SupplierWithRecords>(`/suppliers/${id}`)
      .then((res) => {
        if (!cancelled) setSupplier(res);
      })
      .catch((e) => {
        if (!cancelled) setError(e.message || 'Failed to load supplier.');
      });
    return () => {
      cancelled = true;
    };
  }, [id]);

  if (error) return <Failed message={error} />;
  if (!supplier) return <Loading />;

  return (
    <div>
      <div className="font-bold text-lg text-koperasi-800">{supplier.name}</div>
      {supplier.contact_person && <div className="text-sm text-koperasi-500">Contact: {supplier.contact_person}</div>}

      <Section title="Contact">
        <Field label="Phone">{supplier.phone ?? '—'}</Field>
        <Field label="Email">{supplier.email ?? '—'}</Field>
        <Field label="Address">{supplier.address ?? '—'}</Field>
        {supplier.notes && <Field label="Notes">{supplier.notes}</Field>}
      </Section>

      <Section title="Recent restocks from this supplier">
        {!supplier.restockingRecords || supplier.restockingRecords.length === 0 ? (
          <EmptyNote>No restock records from this supplier yet.</EmptyNote>
        ) : (
          <ul className="divide-y divide-koperasi-50">
            {supplier.restockingRecords.map((r) => (
              <li key={r.id} className="py-2 flex justify-between gap-2 text-sm">
                <button type="button" className="text-left hover:underline min-w-0" onClick={() => navigate({ entity: 'restock', id: r.id })}>
                  <span className="font-medium text-koperasi-800">
                    {r.item ? r.item.name : `Item #${r.item_id}`}
                  </span>{' '}
                  <span className="text-koperasi-500">× {r.quantity}</span>
                  <span className="text-xs text-koperasi-400 block">{formatDate(r.restocked_at)}</span>
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

type MemberWithTransactions = Member & { transactions?: Transaction[] };

export function MemberDetails({ id, navigate }: { id: number; navigate: (t: DetailTarget) => void }) {
  const [member, setMember] = useState<MemberWithTransactions | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setMember(null);
    setError(null);
    apiFetch<MemberWithTransactions>(`/members/${id}`)
      .then((res) => {
        if (!cancelled) setMember(res);
      })
      .catch((e) => {
        if (!cancelled) setError(e.message || 'Failed to load member.');
      });
    return () => {
      cancelled = true;
    };
  }, [id]);

  if (error) return <Failed message={error} />;
  if (!member) return <Loading />;

  return (
    <div>
      <div className="flex items-center gap-2">
        <div className="font-bold text-lg text-koperasi-800">{member.name}</div>
        <span className={`badge ml-auto shrink-0 ${member.is_active ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
          {member.is_active ? 'Active' : 'Inactive'}
        </span>
      </div>
      <div className="text-xs text-koperasi-400">{member.membership_id}</div>

      <Section title="Profile">
        <Field label="Phone">{member.phone ?? '—'}</Field>
        <Field label="Email">{member.email ?? '—'}</Field>
        <Field label="Address">{member.address ?? '—'}</Field>
        <Field label="Joined">{formatDate(member.join_date)}</Field>
        <Field label="SHU balance">{formatRp(member.shu_balance)}</Field>
      </Section>

      <Section title="Recent purchases">
        {!member.transactions || member.transactions.length === 0 ? (
          <EmptyNote>No recorded purchases yet.</EmptyNote>
        ) : (
          <ul className="divide-y divide-koperasi-50">
            {member.transactions.map((t) => (
              <li key={t.id}>
                <button
                  type="button"
                  className="w-full py-2 flex justify-between gap-2 text-sm text-left hover:bg-koperasi-50/50 rounded px-1 -mx-1"
                  onClick={() => navigate({ entity: 'transaction', id: t.id })}
                >
                  <span className="min-w-0">
                    <span className="font-medium text-koperasi-800 block truncate">{t.transaction_code}</span>
                    <span className="text-xs text-koperasi-400">{formatDateTime(t.created_at)}</span>
                  </span>
                  <span className="tabular-nums shrink-0">
                    {formatRp(t.total)}{' '}
                    <span className={`badge ml-1 ${t.payment_status === 'paid' ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                      {t.payment_status}
                    </span>
                  </span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </Section>
    </div>
  );
}

export function UserDetails({ id }: { id: number }) {
  const [user, setUser] = useState<StaffUser | null>(null);
  const [activity, setActivity] = useState<AuditLog[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setUser(null);
    setError(null);
    apiFetch<StaffUser>(`/users/${id}`)
      .then((res) => {
        if (cancelled) return;
        setUser(res);
        return apiFetch<Paginated<AuditLog>>(`/audit-logs?actor_id=${id}&per_page=10`);
      })
      .then((res) => {
        if (!cancelled && res) setActivity(res.data);
      })
      .catch((e) => {
        if (!cancelled) setError(e.message || 'Failed to load staff details.');
      });
    return () => {
      cancelled = true;
    };
  }, [id]);

  if (error) return <Failed message={error} />;
  if (!user) return <Loading />;

  return (
    <div>
      <div className="flex items-center gap-2">
        <div className="font-bold text-lg text-koperasi-800">{user.name}</div>
        <span className={`badge ml-auto shrink-0 ${user.is_active ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
          {user.is_active ? 'Active' : 'Inactive'}
        </span>
      </div>

      <Section title="Account">
        <Field label="Email">{user.email}</Field>
        <Field label="Role">
          <span className="badge bg-koperasi-100 text-koperasi-700">{user.role.replace('_', ' ')}</span>
        </Field>
        <Field label="Phone">{user.phone ?? '—'}</Field>
        <Field label="Shift">{user.shift_label ?? '—'}</Field>
      </Section>

      <Section title="Recent activity">
        {activity.length === 0 ? (
          <EmptyNote>No logged activity for this account yet.</EmptyNote>
        ) : (
          <ul className="divide-y divide-koperasi-50">
            {activity.map((a) => (
              <li key={a.id} className="py-2 text-sm">
                <span className="font-medium text-koperasi-800 capitalize">{a.action.replace(/_/g, ' ')}</span>{' '}
                <span className="text-koperasi-500">
                  {a.auditable_type ? `· ${a.auditable_type.split('\\').pop()} #${a.auditable_id}` : ''}
                </span>
                <span className="text-xs text-koperasi-400 block">{formatDateTime(a.created_at)}</span>
              </li>
            ))}
          </ul>
        )}
      </Section>

    </div>
  );
}
