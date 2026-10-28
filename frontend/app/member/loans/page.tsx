'use client';

import { useEffect, useState } from 'react';
import StatCard from '@/components/StatCard';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import { apiFetch, ApiError } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import { CreditCard } from 'lucide-react';
import type { LoanProduct, MemberLoanIndex, Loan } from '@/lib/types';

const STATUS_LABELS: Record<string, { key: string; color: string }> = {
  draft: { key: 'loans.draft', color: '' },
  pending: { key: 'loans.pending', color: 'bg-yellow-100 text-yellow-700' },
  approved: { key: 'loans.approved', color: 'bg-blue-100 text-blue-700' },
  rejected: { key: 'loans.rejected', color: 'bg-red-100 text-red-700' },
  active: { key: 'loans.active', color: 'bg-green-100 text-green-700' },
  paid_off: { key: 'loans.paidOff', color: 'bg-green-100 text-green-700' },
  written_off: { key: 'loans.writtenOff', color: 'bg-gray-100 text-gray-700' },
};

export default function MemberLoansPage() {
  const [loans, setLoans] = useState<Loan[]>([]);
  const [products, setProducts] = useState<LoanProduct[]>([]);
  const [showApply, setShowApply] = useState(false);
  const [applyProduct, setApplyProduct] = useState<LoanProduct | null>(null);
  const [principal, setPrincipal] = useState('');
  const [term, setTerm] = useState(6);
  const [notes, setNotes] = useState('');
  const [applyError, setApplyError] = useState<string | null>(null);
  const [applySuccess, setApplySuccess] = useState<string | null>(null);
  const [applying, setApplying] = useState(false);
  const [detail, setDetail] = useState<DetailTarget | null>(null);
  const { t, tx } = useLanguage();

  // What the member still owes — server-computed remaining with a fallback.
  function remainingOf(l: Loan): number {
    if (l.remaining_amount !== undefined && l.remaining_amount !== null)
      return Number(l.remaining_amount);
    return Number(l.outstanding_balance) - Number(l.total_paid ?? 0);
  }

  function load() {
    apiFetch<MemberLoanIndex>('/member/loans')
      .then((res) => {
        setLoans(res.loans.data);
        setProducts(res.products);
      })
      .catch(() => {});
  }

  // Load on mount.
  useEffect(() => { load(); }, []);

  function statValue(): string {
    const total = loans
      .filter((l) => l.status !== 'paid_off' && l.status !== 'written_off' && l.status !== 'rejected' && l.status !== 'draft')
      .reduce((sum, l) => sum + remainingOf(l), 0);
    return `Rp ${total.toLocaleString('id-ID')}`;
  }

  function activeCount(): number {
    return loans.filter((l) => l.status === 'active').length;
  }

  function pendingCount(): number {
    return loans.filter((l) => l.status === 'pending').length;
  }

  function rateLabel(p: LoanProduct): string {
    const pct = Number(p.interest_rate) * 100;
    const label = Number.isInteger(pct) ? pct.toString() : pct.toFixed(2);
    const period = p.interest_period === 'annual' ? 'yr' : 'mo';
    return `${label}%/${period}`;
  }

  async function handleApply(e: React.FormEvent) {
    e.preventDefault();
    setApplyError(null);
    setApplySuccess(null);
    setApplying(true);
    try {
      await apiFetch('/member/loans/apply', {
        method: 'POST',
        body: {
          loan_product_id: applyProduct!.id,
          principal: Number(principal),
          term_months: term,
          notes: notes || undefined,
        },
      });
      setApplySuccess(t('memberLoans.applySuccess'));
      setShowApply(false);
      setApplyProduct(null);
      setPrincipal('');
      setTerm(applyProduct?.min_term_months ?? 6);
      setNotes('');
      load();
    } catch (err) {
      setApplyError(err instanceof ApiError ? err.message : t('memberLoans.applyFailed'));
    } finally {
      setApplying(false);
    }
  }

  const activeLoans = loans.filter((l) => l.status === 'active' || l.status === 'approved');

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-koperasi-800">{t('memberLoans.title')}</h1>
        {products.length > 0 && (
          <div className="flex gap-2 items-center">
            <span className="text-sm text-koperasi-500">
              {products.length} product{products.length > 1 ? 's' : ''} available
            </span>
          </div>
        )}
      </div>

      {/* Summary stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <StatCard label={t('loans.totalOutstanding')} value={statValue()} accent="harvest" />
        <StatCard label={t('loans.activeCount')} value={activeCount()} />
        <StatCard label={t('loans.pendingCount')} value={pendingCount()} />
      </div>

      {/* Active / pending loans */}
      <div className="card">
        <h2 className="font-semibold text-koperasi-800 mb-3">{t('memberLoans.myLoans')}</h2>
        {loans.length === 0 ? (
          <p className="text-koperasi-400">{t('loans.noLoans')}</p>
        ) : (
          <ul className="divide-y divide-koperasi-50">
            {loans.map((l) => (
              <li
                key={l.id}
                className="py-3 flex justify-between gap-3 text-sm cursor-pointer hover:bg-koperasi-25 rounded"
                onClick={() => setDetail({ entity: 'loan', id: l.id, endpointBase: '/member/loans' })}
              >
                <div className="min-w-0">
                  <div className="font-medium text-koperasi-800">{l.loan_code}</div>
                  <div className="text-xs text-koperasi-400">
                    {l.product?.name ?? '—'} · {t('loans.colPrincipal')}: Rp {Number(l.principal).toLocaleString('id-ID')}
                  </div>
                  <span className={`badge ml-1 text-xs ${STATUS_LABELS[l.status]?.color ?? ''}`}>
                    {tx(STATUS_LABELS[l.status]?.key ?? 'loans.pending', l.status)}
                  </span>
                </div>
                <div className="text-right shrink-0">
                  <div className="font-medium">Rp {remainingOf(l).toLocaleString('id-ID')}</div>
                  {l.due_at && (
                    <div className="text-xs text-koperasi-400">
                      {t('memberLoans.dueDate')}: {new Date(l.due_at).toLocaleDateString('id-ID')}
                    </div>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>

      {/* Active loan products */}
      <div className="card">
        <h2 className="font-semibold text-koperasi-800 mb-3">{t('memberLoans.applyForLoan')}</h2>
        {products.length === 0 ? (
          <p className="text-koperasi-400">{t('loans.noActiveProducts')}</p>
        ) : (
          <ul className="divide-y divide-koperasi-50">
            {products.map((p) => (
              <li key={p.id} className="py-3 flex justify-between gap-3 text-sm">
                <div className="min-w-0">
                  <div className="font-medium text-koperasi-800">{p.name}</div>
                  {p.description && <div className="text-xs text-koperasi-400">{p.description}</div>}
                  {p.eligibility_rule && (
                    <div className="text-xs text-koperasi-400 mt-1">{t('memberLoans.eligibility')}: {p.eligibility_rule}</div>
                  )}
                </div>
                <div className="text-right shrink-0">
                  <div className="font-medium">{rateLabel(p)}</div>
                  <div className="text-xs text-koperasi-400">
                    {t('memberLoans.minAmount')}: Rp {Number(p.min_amount).toLocaleString('id-ID')}
                  </div>
                  <button
                    className="btn-primary btn-sm mt-2 w-full"
                    onClick={() => { setApplyProduct(p); setShowApply(true); setPrincipal(''); setTerm(p.min_term_months ?? 6); setNotes(''); }}
                  >
                    {t('memberLoans.submit')}
                  </button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>

      {/* Apply form modal */}
      {showApply && applyProduct && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50 p-4">
          <div className="card w-full max-w-md">
            <h2 className="font-semibold text-lg mb-4">{t('memberLoans.applyTitle')}</h2>
            <div className="text-sm text-koperasi-500 mb-3">
              {applyProduct.name} · {rateLabel(applyProduct)}
            </div>
            <form onSubmit={handleApply} className="space-y-3">
              <div>
                <label className="label">{t('memberLoans.principal')}</label>
                <input
                  className="input"
                  type="number"
                  step="1000"
                  min={applyProduct.min_amount}
                  max={applyProduct.max_amount ?? undefined}
                  value={principal}
                  onChange={(e) => setPrincipal(e.target.value)}
                  required
                />
                <div className="text-xs text-koperasi-400 mt-1">
                  {t('memberLoans.minAmount')}: Rp {Number(applyProduct.min_amount).toLocaleString('id-ID')}
                  {applyProduct.max_amount && ` · ${t('memberLoans.maxAmount')}: Rp ${Number(applyProduct.max_amount).toLocaleString('id-ID')}`}
                </div>
              </div>
              <div>
                <label className="label">{t('memberLoans.term')}</label>
                <input
                  className="input"
                  type="number"
                  min={applyProduct.min_term_months}
                  max={applyProduct.max_term_months}
                  value={term}
                  onChange={(e) => setTerm(parseInt(e.target.value) || applyProduct.min_term_months)}
                  required
                />
                <div className="text-xs text-koperasi-400 mt-1">
                  {t('memberLoans.minTerm')}: {applyProduct.min_term_months} {applyProduct.min_term_months > 1 ? t('common.months') : t('common.month')}
                  {' · '}
                  {t('memberLoans.maxTerm')}: {applyProduct.max_term_months} {applyProduct.max_term_months > 1 ? t('common.months') : t('common.month')}
                </div>
              </div>
              <div>
                <label className="label">{t('memberLoans.notesOpt')}</label>
                <textarea className="input" value={notes} onChange={(e) => setNotes(e.target.value)} />
              </div>
              {applyError && <p className="text-sm text-red-600">{applyError}</p>}
              {applySuccess && <p className="text-sm text-green-600">{applySuccess}</p>}
              <div className="flex justify-end gap-2 pt-2">
                <button type="button" className="btn-secondary" onClick={() => setShowApply(false)}>
                  {t('common.cancel')}
                </button>
                <button type="submit" className="btn-primary" disabled={applying}>
                  {applying ? t('memberLoans.submitting') : t('memberLoans.submit')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}
    </div>
  );
}
