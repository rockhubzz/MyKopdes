'use client';

import { useEffect, useState } from 'react';
import { apiFetch, ApiError } from '@/lib/api';
import { useLanguage, type TKey, type Params } from '@/lib/i18n/LanguageContext';
import type { Loan } from '@/lib/types';
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

const STATUS_COLORS: Record<string, string> = {
  pending: 'bg-yellow-100 text-yellow-700',
  approved: 'bg-blue-100 text-blue-700',
  active: 'bg-green-100 text-green-700',
  rejected: 'bg-red-100 text-red-700',
  paid_off: 'bg-green-100 text-green-700',
  written_off: 'bg-gray-100 text-gray-700',
  draft: '',
};

function statusLabel(t: (key: TKey, params?: Params) => string, tx: (key: string, fallback: string, params?: Params) => string, loan: Loan): string {
  const key = `loans.${loan.status}`;
  const fallbackMap: Record<string, string> = {
    draft: 'Draft',
    pending: 'Pending Approval',
    approved: 'Approved',
    rejected: 'Rejected',
    active: 'Active',
    paid_off: 'Paid Off',
    written_off: 'Written Off',
  };
  return tx(key, fallbackMap[loan.status] ?? loan.status, {});
}

export function LoanDetails({ id, endpointBase = '/loans', navigate }: { id: number; endpointBase?: string; navigate: (t: DetailTarget) => void }) {
  const [loan, setLoan] = useState<Loan | null>(null);
  const [error, setError] = useState<string | null>(null);
  const { t, tx } = useLanguage();

  useEffect(() => {
    let cancelled = false;
    setLoan(null);
    setError(null);
    apiFetch<Loan>(`${endpointBase}/${id}`)
      .then((res) => {
        if (!cancelled) setLoan(res);
      })
      .catch((e) => {
        if (!cancelled) setError(e.message || t('loans.rejectFailed'));
      });
    return () => {
      cancelled = true;
    };
  }, [id, t]);

  if (error) return <Failed message={error} />;
  if (!loan) return <Loading />;

  const ratePct = Number(loan.interest_rate) * 100;
  const rateLabel = `${ratePct}%/${loan.interest_period === 'annual' ? 'yr' : 'mo'}`;
  const statusKey = `loans.${loan.status}` as const;
  const statusColor = STATUS_COLORS[loan.status] ?? '';

  return (
    <div>
      <div className="flex items-center gap-2">
        <div className="font-bold text-lg text-koperasi-800 font-mono">{loan.loan_code}</div>
        <span className={`badge ml-auto shrink-0 ${statusColor}`}>
          {tx(statusKey, loan.status.replace('_', ' '), {})}
        </span>
      </div>
      <div className="text-xs text-koperasi-400">{formatDateTime(loan.created_at)}</div>

      {loan.product && (
        <div className="text-sm text-koperasi-500 mt-1">
          {loan.product.name} · {rateLabel}
        </div>
      )}

      <Section title={t('details.totals')}>
        <Field label={t('loans.colPrincipal')}>{formatRp(loan.principal)}</Field>
        <Field label={t('memberLoans.processingFeeLabel')}>{formatRp(loan.processing_fee)}</Field>
        <Field label={t('memberLoans.accruedInterest')}>{formatRp(loan.accrued_interest)}</Field>
        <Field label={t('loans.colBalance')}>
          <span className="font-bold text-koperasi-800">{formatRp(loan.outstanding_balance)}</span>
        </Field>
        <Field label={t('memberLoans.totalPaid')}>{formatRp(loan.total_paid)}</Field>
      </Section>

      <Section title={t('details.parties')}>
        <Field label={t('details.member')}>
          {loan.member ? (
            <button
              type="button"
              className="text-koperasi-600 hover:underline"
              onClick={() => navigate({ entity: 'member', id: loan.member_id })}
            >
              {loan.member.name}
            </button>
          ) : (
            '—'
          )}
        </Field>
        {loan.approved_by && (
          <Field label={t('loans.approve')}>
            {loan.approvedBy?.name ?? `Staff #${loan.approved_by}`}
          </Field>
        )}
      </Section>

      <Section title={t('details.rules')}>
        <Field label={t('memberLoans.term')}>{loan.term_months} {t('common.months')}</Field>
        <Field label={t('loans.colDueDate')}>{formatDate(loan.due_at)}</Field>
        {loan.applied_at && <Field label={t('loans.pending')}>{formatDateTime(loan.applied_at)}</Field>}
        {loan.approved_at && <Field label={t('loans.approved')}>{formatDateTime(loan.approved_at)}</Field>}
        {loan.paid_off_at && <Field label={t('loans.paidOff')}>{formatDateTime(loan.paid_off_at)}</Field>}
      </Section>

      {loan.notes && (
        <Section title={t('details.notes')}>
          <p className="text-sm text-koperasi-700">{loan.notes}</p>
        </Section>
      )}

      {loan.payments && loan.payments.length > 0 && (
        <Section title={t('memberLoans.paymentHistory')}>
          <ul className="divide-y divide-koperasi-50">
            {loan.payments.map((p) => (
              <li key={p.id} className="py-2 flex justify-between gap-2 text-sm">
                <div>
                  <span className="font-medium text-koperasi-800">Rp {Number(p.amount).toLocaleString('id-ID')}</span>
                  <span className="text-xs text-koperasi-400 block">
                    {p.payment_method.replace('_', ' ')} · {formatDateTime(p.paid_at)}
                  </span>
                </div>
                {p.recorded_by && (
                  <span className="text-koperasi-400 text-xs">
                    {t('details.submittedBy')}: {p.recordedBy?.name ?? `#${p.recorded_by}`}
                  </span>
                )}
              </li>
            ))}
          </ul>
        </Section>
      )}

      {loan.payments && loan.payments.length === 0 && (
        <Section title={t('memberLoans.paymentHistory')}>
          <EmptyNote>{t('memberLoans.noPayments')}</EmptyNote>
        </Section>
      )}
    </div>
  );
}
