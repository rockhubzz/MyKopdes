'use client';

import { useState } from 'react';
import DataTable, { Column } from '@/components/DataTable';
import { apiFetch, ApiError } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { LoanProduct } from '@/lib/types';

const emptyForm = {
  name: '',
  description: '',
  interest_rate: '',
  interest_period: 'monthly',
  min_term_months: 1,
  max_term_months: 6,
  min_amount: '',
  max_amount: '',
  processing_fee_rate: '0',
  eligibility_rule: '',
  is_active: true,
};

function rateLabel(p: LoanProduct): string {
  const pct = Number(p.interest_rate) * 100;
  const label = Number.isInteger(pct) ? pct.toString() : pct.toFixed(2);
  return `${label}%/${p.interest_period === 'annual' ? 'yr' : 'mo'}`;
}

export default function LoanProductsPage() {
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<LoanProduct | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [showInactive, setShowInactive] = useState(false);
  const { t } = useLanguage();

  function openCreate() {
    setEditing(null);
    setForm(emptyForm);
    setError(null);
    setShowForm(true);
  }

  function openEdit(p: LoanProduct) {
    setEditing(p);
    setForm({
      name: p.name,
      description: p.description ?? '',
      interest_rate: String(Number(p.interest_rate) * 100), // store as percentage for editing
      interest_period: p.interest_period,
      min_term_months: p.min_term_months,
      max_term_months: p.max_term_months,
      min_amount: String(Number(p.min_amount)),
      max_amount: p.max_amount ? String(Number(p.max_amount)) : '',
      processing_fee_rate: String(Number(p.processing_fee_rate) * 100),
      eligibility_rule: p.eligibility_rule ?? '',
      is_active: p.is_active,
    });
    setError(null);
    setShowForm(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      // Convert percentage inputs back to decimal fractions for the API.
      const body: Record<string, unknown> = {
        ...form,
        interest_rate: String(Number(form.interest_rate) / 100),
        processing_fee_rate: String(Number(form.processing_fee_rate) / 100),
        min_amount: form.min_amount,
        min_term_months: form.min_term_months,
        max_term_months: form.max_term_months,
      };
      if (body.max_amount === '') delete body.max_amount;

      if (editing) {
        await apiFetch(`/loan-products/${editing.id}`, { method: 'PUT', body });
      } else {
        await apiFetch('/loan-products', { method: 'POST', body });
      }
      setShowForm(false);
      setReloadKey((k) => k + 1);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('loanProducts.saveFailed'));
    }
  }

  async function handleDeactivate(p: LoanProduct) {
    if (!confirm(t('loanProducts.deactivateConfirm', { name: p.name }))) return;
    try {
      await apiFetch(`/loan-products/${p.id}`, { method: 'DELETE' });
      setReloadKey((k) => k + 1);
    } catch (err) {
      alert(err instanceof ApiError ? err.message : t('loanProducts.deleteFailed'));
    }
  }

  async function handleActivate(p: LoanProduct) {
    if (!confirm(t('loanProducts.activateConfirm', { name: p.name }))) return;
    try {
      await apiFetch(`/loan-products/${p.id}/activate`, { method: 'POST' });
      setReloadKey((k) => k + 1);
    } catch (err) {
      alert(err instanceof ApiError ? err.message : t('loanProducts.saveFailed'));
    }
  }

  const columns: Column<LoanProduct>[] = [
    { header: t('loanProducts.colName'), render: (p) => p.name },
    { header: t('loanProducts.colRate'), render: (p) => rateLabel(p) },
    { header: t('loanProducts.colMinAmount'), render: (p) => `Rp ${Number(p.min_amount).toLocaleString('id-ID')}` },
    {
      header: t('loanProducts.colStatus'),
      render: (p) => (
        <span className={`badge ${p.is_active ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
          {p.is_active ? t('common.active') : t('common.inactive')}
        </span>
      ),
    },
    {
      header: t('table.actions'),
      render: (p) => (
        <div className="flex justify-end gap-1.5">
          {p.is_active ? (
            <button className="btn-action-danger" onClick={() => handleDeactivate(p)}>
              {t('loanProducts.deactivate')}
            </button>
          ) : (
            <button className="btn-action-edit" onClick={() => handleActivate(p)}>
              {t('loanProducts.activate')}
            </button>
          )}
          <button className="btn-action-edit" onClick={() => openEdit(p)}>
            {t('common.edit')}
          </button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-koperasi-800">{t('loanProducts.title')}</h1>
        <label className="text-sm flex items-center gap-1"><input type="checkbox" checked={showInactive} onChange={(e) => setShowInactive(e.target.checked)} />{t('loanProducts.showInactive')}</label>
          <button className="btn-primary" onClick={openCreate}>
          {t('loanProducts.newProduct')}
        </button>
      </div>

      <DataTable
        endpoint="/loan-products"
        columns={columns}
        searchable={false}
        reloadKey={reloadKey}
        extraParams={`${showInactive ? '&include_inactive=1' : ''}`}
      />

      {showForm && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50 p-4">
          <div className="card w-full max-w-lg">
            <h2 className="font-semibold text-lg mb-4">
              {editing ? t('loanProducts.editTitle') : t('loanProducts.addTitle')}
            </h2>
            <form onSubmit={handleSubmit} className="space-y-3">
              <div>
                <label className="label">{t('loanProducts.name')}</label>
                <input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
              </div>
              <div>
                <label className="label">{t('loanProducts.description')}</label>
                <textarea className="input" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="label">{t('loanProducts.interestRate')} (% per {form.interest_period === 'annual' ? t('memberLoans.annual') : t('memberLoans.monthly')})</label>
                  <input
                    className="input"
                    type="number"
                    step="0.01"
                    min="0"
                    value={form.interest_rate}
                    onChange={(e) => setForm({ ...form, interest_rate: e.target.value })}
                    required
                  />
                </div>
                <div>
                  <label className="label">{t('loanProducts.interestPeriod')}</label>
                  <select className="input" value={form.interest_period} onChange={(e) => setForm({ ...form, interest_period: e.target.value as 'monthly' | 'annual' })}>
                    <option value="monthly">{t('memberLoans.monthly')}</option>
                    <option value="annual">{t('memberLoans.annual')}</option>
                  </select>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="label">{t('loanProducts.minTerm')}</label>
                  <input className="input" type="number" min="1" value={form.min_term_months} onChange={(e) => setForm({ ...form, min_term_months: parseInt(e.target.value) || 1 })} required />
                </div>
                <div>
                  <label className="label">{t('loanProducts.maxTerm')}</label>
                  <input className="input" type="number" min="1" value={form.max_term_months} onChange={(e) => setForm({ ...form, max_term_months: parseInt(e.target.value) || 1 })} required />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="label">{t('loanProducts.minAmount')}</label>
                  <input className="input" type="number" step="1000" min="0" value={form.min_amount} onChange={(e) => setForm({ ...form, min_amount: e.target.value })} required />
                </div>
                <div>
                  <label className="label">{t('loanProducts.maxAmount')}</label>
                  <input className="input" type="number" step="1000" min="0" value={form.max_amount} onChange={(e) => setForm({ ...form, max_amount: e.target.value })} placeholder="Kosongkan untuk tak terbatas" />
                </div>
              </div>
              <div>
                <label className="label">{t('loanProducts.processingFeeRate')} (%)</label>
                <input className="input" type="number" step="0.01" min="0" value={form.processing_fee_rate} onChange={(e) => setForm({ ...form, processing_fee_rate: e.target.value })} />
              </div>
              <div>
                <label className="label">{t('loanProducts.eligibilityRule')}</label>
                <input className="input" value={form.eligibility_rule} onChange={(e) => setForm({ ...form, eligibility_rule: e.target.value })} placeholder="cth. Anggota aktif minimal 3 bulan" />
              </div>
              {error && <p className="text-sm text-red-600">{error}</p>}
              <div className="flex justify-end gap-2 pt-2">
                <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>
                  {t('common.cancel')}
                </button>
                <button type="submit" className="btn-primary">
                  {t('common.save')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
