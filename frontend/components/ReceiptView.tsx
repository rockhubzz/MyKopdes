'use client';

import { memo } from 'react';
import { Printer } from 'lucide-react';
import type { Transaction } from '@/lib/types';
import { useLanguage } from '@/lib/i18n/LanguageContext';

function ReceiptView({ transaction, onNewSale }: { transaction: Transaction; onNewSale: () => void }) {
  const { t } = useLanguage();
  return (
    <div className="max-w-md mx-auto space-y-4">
      <div className="card font-mono text-sm receipt-ticket" id="receipt-print-area">
        <div className="text-center mb-3">
          <div className="font-bold">KOPERASI SEJAHTERA BERSAMA</div>
          <div className="text-xs text-koperasi-500">{transaction.transaction_code}</div>
          <div className="text-xs text-koperasi-500">{new Date(transaction.created_at).toLocaleString('id-ID')}</div>
        </div>
        <div className="border-t border-dashed border-koperasi-300 my-2" />
        {transaction.items?.map((line) => (
          <div key={line.id} className="flex justify-between gap-2">
            <span>
              {line.item?.name} x{line.quantity}
            </span>
            <span className="tabular-nums">Rp {Number(line.subtotal).toLocaleString('id-ID')}</span>
          </div>
        ))}
        <div className="border-t border-dashed border-koperasi-300 my-2" />
        <div className="flex justify-between">
          <span>{t('receipt.subtotal')}</span>
          <span className="tabular-nums">Rp {Number(transaction.subtotal).toLocaleString('id-ID')}</span>
        </div>
        {Number(transaction.discount_amount) > 0 && (
          <div className="flex justify-between">
            <span>{transaction.discount ? t('receipt.discountWith', { name: transaction.discount.name }) : t('receipt.discount')}</span>
            <span className="tabular-nums">- Rp {Number(transaction.discount_amount).toLocaleString('id-ID')}</span>
          </div>
        )}
        {Number(transaction.tax_amount) > 0 && (
          <div className="flex justify-between">
            <span>{t('receipt.tax')}</span>
            <span className="tabular-nums">Rp {Number(transaction.tax_amount).toLocaleString('id-ID')}</span>
          </div>
        )}
        <div className="flex justify-between font-bold text-base mt-1">
          <span>{t('receipt.total')}</span>
          <span className="tabular-nums">Rp {Number(transaction.total).toLocaleString('id-ID')}</span>
        </div>
        <div className="border-t border-dashed border-koperasi-300 my-2" />
        <div>{t('receipt.payment', { method: transaction.payment_method.replace('_', ' ').toUpperCase() })}</div>
        {transaction.member && <div>{t('receipt.member', { name: transaction.member.name })}</div>}
        <div className="text-center text-xs text-koperasi-400 mt-3">{t('receipt.thanks')}</div>
      </div>

      <div className="flex gap-2 print:hidden">
        <button type="button" className="btn-secondary flex-1 flex items-center justify-center gap-2" onClick={() => window.print()}>
          <Printer size={16} aria-hidden="true" />
          {t('receipt.printReceipt')}
        </button>
        <button type="button" className="btn-primary flex-1" onClick={onNewSale}>
          {t('receipt.newSale')}
        </button>
      </div>
    </div>
  );
}

// Shown once per completed sale — memo keeps the completed ticket from
// re-rendering while the cashier keeps working around it.
export default memo(ReceiptView);
