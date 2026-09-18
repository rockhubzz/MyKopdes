'use client';

import { memo } from 'react';
import type { Transaction } from '@/lib/types';

function ReceiptView({ transaction, onNewSale }: { transaction: Transaction; onNewSale: () => void }) {
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
          <span>Subtotal</span>
          <span className="tabular-nums">Rp {Number(transaction.subtotal).toLocaleString('id-ID')}</span>
        </div>
        {Number(transaction.discount_amount) > 0 && (
          <div className="flex justify-between">
            <span>Discount{transaction.discount ? ` (${transaction.discount.name})` : ''}</span>
            <span className="tabular-nums">- Rp {Number(transaction.discount_amount).toLocaleString('id-ID')}</span>
          </div>
        )}
        {Number(transaction.tax_amount) > 0 && (
          <div className="flex justify-between">
            <span>Tax</span>
            <span className="tabular-nums">Rp {Number(transaction.tax_amount).toLocaleString('id-ID')}</span>
          </div>
        )}
        <div className="flex justify-between font-bold text-base mt-1">
          <span>Total</span>
          <span className="tabular-nums">Rp {Number(transaction.total).toLocaleString('id-ID')}</span>
        </div>
        <div className="border-t border-dashed border-koperasi-300 my-2" />
        <div>Payment: {transaction.payment_method.replace('_', ' ').toUpperCase()}</div>
        {transaction.member && <div>Member: {transaction.member.name}</div>}
        <div className="text-center text-xs text-koperasi-400 mt-3">Terima kasih atas kunjungan Anda!</div>
      </div>

      <div className="flex gap-2 print:hidden">
        <button type="button" className="btn-secondary flex-1" onClick={() => window.print()}>
          🖨️ Print Receipt
        </button>
        <button type="button" className="btn-primary flex-1" onClick={onNewSale}>
          New Sale
        </button>
      </div>
    </div>
  );
}

// Shown once per completed sale — memo keeps the completed ticket from
// re-rendering while the cashier keeps working around it.
export default memo(ReceiptView);
