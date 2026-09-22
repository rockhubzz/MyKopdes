'use client';

import { useEffect, useState } from 'react';
import { apiFetch, storageUrl } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { AuditLog, Member, Paginated, RestockingRecord, StaffUser, Supplier, Transaction } from '@/lib/types';
import type { DetailTarget } from './DetailModal';
import { EmptyNote, Field, Section, formatDate, formatDateTime, formatRp } from './ui';

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

type SupplierWithRecords = Supplier & { restockingRecords?: RestockingRecord[] };

export function SupplierDetails({ id, navigate }: { id: number; navigate: (t: DetailTarget) => void }) {
  const [supplier, setSupplier] = useState<SupplierWithRecords | null>(null);
  const [error, setError] = useState<string | null>(null);
  const { t } = useLanguage();

  useEffect(() => {
    let cancelled = false;
    setSupplier(null);
    setError(null);
    apiFetch<SupplierWithRecords>(`/suppliers/${id}`)
      .then((res) => {
        if (!cancelled) setSupplier(res);
      })
      .catch((e) => {
        if (!cancelled) setError(e.message || t('details.failedSupplier'));
      });
    return () => {
      cancelled = true;
    };
  }, [id, t]);

  if (error) return <Failed message={error} />;
  if (!supplier) return <Loading />;

  return (
    <div>
      <div className="font-bold text-lg text-koperasi-800">{supplier.name}</div>
      {supplier.contact_person && <div className="text-sm text-koperasi-500">{t('details.contactWith', { name: supplier.contact_person })}</div>}

      <Section title={t('details.contact')}>
        <Field label={t('details.phone')}>{supplier.phone ?? '—'}</Field>
        <Field label={t('details.email')}>{supplier.email ?? '—'}</Field>
        <Field label={t('details.address')}>{supplier.address ?? '—'}</Field>
        {supplier.notes && <Field label={t('details.notes')}>{supplier.notes}</Field>}
      </Section>

      <Section title={t('details.recentRestocksSupplier')}>
        {!supplier.restockingRecords || supplier.restockingRecords.length === 0 ? (
          <EmptyNote>{t('details.noRestocksSupplier')}</EmptyNote>
        ) : (
          <ul className="divide-y divide-koperasi-50">
            {supplier.restockingRecords.map((r) => (
              <li key={r.id} className="py-2 flex justify-between gap-2 text-sm">
                <button type="button" className="text-left hover:underline min-w-0" onClick={() => navigate({ entity: 'restock', id: r.id })}>
                  <span className="font-medium text-koperasi-800">
                    {r.item ? r.item.name : t('details.itemHash', { id: r.item_id })}
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
  const { t, tx } = useLanguage();

  useEffect(() => {
    let cancelled = false;
    setMember(null);
    setError(null);
    apiFetch<MemberWithTransactions>(`/members/${id}`)
      .then((res) => {
        if (!cancelled) setMember(res);
      })
      .catch((e) => {
        if (!cancelled) setError(e.message || t('details.failedMember'));
      });
    return () => {
      cancelled = true;
    };
  }, [id, t]);

  if (error) return <Failed message={error} />;
  if (!member) return <Loading />;

  return (
    <div>
      <div className="flex items-center gap-2">
        {member.avatar_path ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={storageUrl(member.avatar_path)} alt={member.name} width={40} height={40} className="w-10 h-10 rounded-full object-cover shrink-0" />
        ) : null}
        <div className="font-bold text-lg text-koperasi-800">{member.name}</div>
        <span className={`badge ml-auto shrink-0 ${member.is_active ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
          {member.is_active ? t('details.active') : t('details.inactive')}
        </span>
      </div>
      <div className="text-xs text-koperasi-400">{member.membership_id}</div>

      <Section title={t('details.profile')}>
        <Field label={t('details.phone')}>{member.phone ?? '—'}</Field>
        <Field label={t('details.email')}>{member.email ?? '—'}</Field>
        <Field label={t('details.address')}>{member.address ?? '—'}</Field>
        <Field label={t('details.joined')}>{formatDate(member.join_date)}</Field>
        <Field label={t('details.shuBalance')}>{formatRp(member.shu_balance)}</Field>
      </Section>

      <Section title={t('details.recentPurchases')}>
        {!member.transactions || member.transactions.length === 0 ? (
          <EmptyNote>{t('details.noPurchases')}</EmptyNote>
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
                      {tx(`status.${t.payment_status}`, t.payment_status)}
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
  const { t, tx } = useLanguage();

  useEffect(() => {
    let cancelled = false;
    setUser(null);
    setError(null);
    Promise.all([
      apiFetch<StaffUser>(`/users/${id}`),
      apiFetch<Paginated<AuditLog>>(`/audit-logs?actor_id=${id}&per_page=10`).catch(() => null),
    ])
      .then(([userRes, logsRes]) => {
        if (cancelled) return;
        setUser(userRes);
        if (logsRes) setActivity(logsRes.data);
      })
      .catch((e) => {
        if (!cancelled) setError(e.message || t('details.failedUser'));
      });
    return () => {
      cancelled = true;
    };
  }, [id, t]);

  if (error) return <Failed message={error} />;
  if (!user) return <Loading />;

  return (
    <div>
      <div className="flex items-center gap-2">
        {user.avatar_path ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={storageUrl(user.avatar_path)} alt={user.name} width={40} height={40} className="w-10 h-10 rounded-full object-cover shrink-0" />
        ) : null}
        <div className="font-bold text-lg text-koperasi-800">{user.name}</div>
        <span className={`badge ml-auto shrink-0 ${user.is_active ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
          {user.is_active ? t('details.active') : t('details.inactive')}
        </span>
      </div>

      <Section title={t('details.account')}>
        <Field label={t('details.email')}>{user.email}</Field>
        <Field label={t('details.role')}>
          <span className="badge bg-koperasi-100 text-koperasi-700">{tx(`roleValue.${user.role}`, user.role.replace('_', ' '))}</span>
        </Field>
        <Field label={t('details.phone')}>{user.phone ?? '—'}</Field>
        <Field label={t('details.shift')}>{user.shift_label ?? '—'}</Field>
      </Section>

      <Section title={t('details.recentActivity')}>
        {activity.length === 0 ? (
          <EmptyNote>{t('details.noActivity')}</EmptyNote>
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
