'use client';

import { useState } from 'react';
import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import { apiFetch, ApiError } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { Loan, LoanStatus } from '@/lib/types';

const STATUS_LABELS: Record<LoanStatus, { key: string; color: string }> = {
  draft: { key: 'loans.draft', color: '' },
  pending: { key: 'loans.pending', color: 'bg-yellow-100 text-yellow-700' },
  approved: { key: 'loans.approved', color: 'bg-blue-100 text-blue-700' },
  rejected: { key: 'loans.rejected', color: 'bg-red-100 text-red-700' },
  active: { key: 'loans.active', color: 'bg-green-100 text-green-700' },
  paid_off: { key: 'loans.paidOff', color: 'bg-green-100 text-green-700' },
  written_off: { key: 'loans.writtenOff', color: 'bg-gray-100 text-gray-700' },
};

export default function OwnerLoansPage() {
  const [reloadKey, setReloadKey] = useState(0);
  const [detail, setDetail] = useState<DetailTarget | null>(null);
  const [payLoan, setPayLoan] = useState<Loan | null>(null);
  const [payAmount, setPayAmount] = useState('');
  const [payMethod, setPayMethod] = useState<'cash' | 'qris' | 'bank_transfer'>('cash');
  const [payError, setPayError] = useState<string | null>(null);
  const [paySuccess, setPaySuccess] = useState<string | null>(null);
  const { t, tx } = useLanguage();

  // What the member actually still owes — the server sends the computed
  // remaining; fall back to outstanding − paid for older payloads.
  function remainingOf(l: Loan): number {
    if (l.remaining_amount !== undefined && l.remaining_amount !== null)
      return Number(l.remaining_amount);
    return Number(l.outstanding_balance) - Number(l.total_paid ?? 0);
  }

  async function handleAction(action: 'approve' | 'reject' | 'write-off' | 'activate', loan: Loan) {
    const label = action === 'approve'
      ? t('loans.approve')
      : action === 'reject'
      ? t('loans.reject')
      : action === 'activate'
      ? t('loans.activate')
      : t('loans.writeOff');
    const confirmMsg = action === 'approve'
      ? t('loans.approveConfirm', { code: loan.loan_code })
      : action === 'reject'
      ? t('loans.rejectConfirm', { code: loan.loan_code })
      : action === 'activate'
      ? t('loans.activateConfirm', { code: loan.loan_code })
      : `${t('loans.writeOff')} ${loan.loan_code}?`;

    if (!confirm(confirmMsg)) return;

    try {
      if (action === 'write-off') {
        const reason = prompt(t('loans.writeOffReason'));
        if (!reason) return;
        await apiFetch(`/loans/${loan.id}/write-off`, { method: 'POST', body: { reason } });
        t('loans.writeOffSuccess');
      } else {
        await apiFetch(`/loans/${loan.id}/${action}`, { method: 'POST' });
      }
      setReloadKey((k) => k + 1);
    } catch (err) {
      const failKey = action === 'approve'
        ? 'loans.approveFailed'
        : action === 'reject'
        ? 'loans.rejectFailed'
        : action === 'activate'
        ? 'loans.activateFailed'
        : 'loans.writeOffFailed';
      alert(err instanceof ApiError ? err.message : t(failKey));
    }
  }

  async function handlePay(e: React.FormEvent) {
    e.preventDefault();
    if (!payLoan) return;
    setPayError(null);
    setPaySuccess(null);
    try {
      const updated = await apiFetch<Loan>(`/loans/${payLoan.id}/pay`, {
        method: 'POST',
        body: { amount: payAmount, payment_method: payMethod },
      });
      setPaySuccess(t('loans.repaySuccess', { balance: remainingOf(updated).toLocaleString('id-ID') }));
      setPayLoan(null);
      setPayAmount('');
      setReloadKey((k) => k + 1);
    } catch (err) {
      setPayError(err instanceof ApiError ? err.message : t('loans.repayFailed'));
    }
  }

  const columns: Column<Loan>[] = [
    { header: t('loans.colCode'), render: (l) => <code className="text-xs">{l.loan_code}</code> },
    { header: t('loans.colMember'), render: (l) => l.member?.name ?? `—` },
    { header: t('loans.colProduct'), render: (l) => l.product?.name ?? `—` },
    { header: t('loans.colPrincipal'), render: (l) => `Rp ${Number(l.principal).toLocaleString('id-ID')}` },
    { header: t('loans.colBalance'), render: (l) => `Rp ${remainingOf(l).toLocaleString('id-ID')}` },
    {
      header: t('loans.colStatus'),
      render: (l) => (
        <span className={`badge ${STATUS_LABELS[l.status]?.color ?? ''}`}>
          {tx(STATUS_LABELS[l.status]?.key ?? 'loans.pending', l.status)}
        </span>
      ),
    },
    { header: t('loans.colDueDate'), render: (l) => (l.due_at ? new Date(l.due_at).toLocaleDateString('id-ID') : '—') },
  ];

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-koperasi-800">{t('loans.memberLoansTitle')}</h1>

      {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}

      <DataTable
        endpoint="/loans"
        columns={columns}
        onRowClick={(l) => setDetail({ entity: 'loan', id: l.id })}
        searchable={false}
        reloadKey={reloadKey}
        actions={(l) => {
          const is = l.status;
          const btns = [];
          if (is === 'pending') {
            btns.push(<button key="approve" className="btn-action-edit" onClick={() => handleAction('approve', l)}>{t('loans.approve')}</button>);
            btns.push(<button key="reject" className="btn-action-danger" onClick={() => handleAction('reject', l)}>{t('loans.reject')}</button>);
          }
          if (is === 'approved') {
            btns.push(<button key="activate" className="btn-action-edit" onClick={() => handleAction('activate', l)}>{t('loans.activate')}</button>);
          }
          if (is === 'active' || is === 'approved') {
            btns.push(<button key="write-off" className="btn-action-danger" onClick={() => handleAction('write-off', l)}>{t('loans.writeOff')}</button>);
          }
          if (is === 'active' || is === 'approved') {
            btns.push(<button key="repay" className="btn-action-edit" onClick={() => { setPayLoan(l); setPayAmount(''); setPayError(null); setPaySuccess(null); }}>
              {t('loans.repay')}
            </button>);
          }
          return <div className="flex justify-end gap-1.5">{btns}</div>;
        }}
      />

      {/* Repayment modal */}
      {payLoan && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50 p-4">
          <div className="card w-full max-w-md">
            <h2 className="font-semibold text-lg mb-4">{t('loans.repayTitle', { code: payLoan.loan_code })}</h2>
            <div className="text-sm text-koperasi-500 mb-3">
              {t('loans.repayRemaining', { balance: remainingOf(payLoan).toLocaleString('id-ID') })}
            </div>
            <form onSubmit={handlePay} className="space-y-3">
              <div>
                <label className="label">{t('loans.amount')}</label>
                <input
                  className="input"
                  type="number"
                  step="1000"
                  min="0"
                  max={remainingOf(payLoan).toFixed(2)}
                  value={payAmount}
                  onChange={(e) => setPayAmount(e.target.value)}
                  required
                />
              </div>
              <div>
                <label className="label">{t('loans.paymentMethod')}</label>
                <select className="input" value={payMethod} onChange={(e) => setPayMethod(e.target.value as 'cash' | 'qris' | 'bank_transfer')}>
                  <option value="cash">{t('pay.cash')}</option>
                  <option value="qris">{t('pay.qris')}</option>
                  <option value="bank_transfer">{t('pay.bankTransfer')}</option>
                </select>
              </div>
              {payError && <p className="text-sm text-red-600">{payError}</p>}
              {paySuccess && <p className="text-sm text-green-600">{paySuccess}</p>}
              <div className="flex justify-end gap-2 pt-2">
                <button type="button" className="btn-secondary" onClick={() => setPayLoan(null)}>
                  {t('common.cancel')}
                </button>
                <button type="submit" className="btn-primary">
                  {t('loans.repay')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
