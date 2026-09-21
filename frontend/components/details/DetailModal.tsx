'use client';

import { useEffect, useState } from 'react';
import { ArrowLeft, X } from 'lucide-react';
import { CategoryDetails, ItemDetails } from './catalog';
import { MemberDetails, SupplierDetails, UserDetails } from './people';
import { DiscountDetails, RestockDetails, TransactionDetails } from './sales';
import { AuditLogDetails } from './AuditLogDetails';
import { useLanguage, type TKey } from '@/lib/i18n/LanguageContext';

export type DetailEntity =
  | 'item'
  | 'category'
  | 'supplier'
  | 'member'
  | 'transaction'
  | 'discount'
  | 'user'
  | 'restock'
  | 'auditlog';

export interface DetailTarget {
  entity: DetailEntity;
  id: number;
  /** API base for transactions — staff pages use `/transactions`, the member portal uses `/member/transactions`. */
  endpointBase?: string;
}

const TITLE_KEYS: Record<DetailEntity, TKey> = {
  item: 'details.titleItem',
  category: 'details.titleCategory',
  supplier: 'details.titleSupplier',
  member: 'details.titleMember',
  transaction: 'details.titleTransaction',
  discount: 'details.titleDiscount',
  user: 'details.titleUser',
  restock: 'details.titleRestock',
  auditlog: 'details.titleAuditlog',
};

function Body({ target, navigate }: { target: DetailTarget; navigate: (t: DetailTarget) => void }) {
  switch (target.entity) {
    case 'item':
      return <ItemDetails id={target.id} navigate={navigate} />;
    case 'category':
      return <CategoryDetails id={target.id} navigate={navigate} />;
    case 'supplier':
      return <SupplierDetails id={target.id} navigate={navigate} />;
    case 'member':
      return <MemberDetails id={target.id} navigate={navigate} />;
    case 'transaction':
      return <TransactionDetails id={target.id} endpointBase={target.endpointBase ?? '/transactions'} navigate={navigate} />;
    case 'discount':
      return <DiscountDetails id={target.id} navigate={navigate} />;
    case 'user':
      return <UserDetails id={target.id} />;
    case 'restock':
      return <RestockDetails id={target.id} navigate={navigate} />;
    case 'auditlog':
      return <AuditLogDetails id={target.id} />;
  }
}

/**
 * Slide-over detail viewer with drill-down navigation (e.g. category → an
 * item in it → its supplier). Render once per page; open by setting `target`.
 */
export default function DetailModal({ target, onClose }: { target: DetailTarget | null; onClose: () => void }) {
  const [stack, setStack] = useState<DetailTarget[]>([]);
  const { t } = useLanguage();

  // A new target from the page replaces the whole stack.
  useEffect(() => {
    setStack(target ? [target] : []);
  }, [target]);

  // Esc closes (or steps back), and the background page stays put.
  useEffect(() => {
    if (!target) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key !== 'Escape') return;
      setStack((s) => {
        if (s.length > 1) return s.slice(0, -1);
        onClose();
        return s;
      });
    };
    document.addEventListener('keydown', onKey);
    document.body.style.overflow = 'hidden';
    return () => {
      document.removeEventListener('keydown', onKey);
      document.body.style.overflow = '';
    };
  }, [target, onClose]);

  if (!target) return null;
  const current = stack[stack.length - 1] ?? target;
  const title = t(TITLE_KEYS[current.entity]);

  // Drilling stays inside the modal stack; the page's `target` only opens
  // the viewer and closes it, so Back history is never clobbered.
  const navigate = (t: DetailTarget) => setStack((s) => [...s, t]);

  return (
    <div className="fixed inset-0 z-50 flex justify-end" role="dialog" aria-modal="true" aria-label={title}>
      <button type="button" aria-label={t('details.close')} className="absolute inset-0 bg-black/40" onClick={onClose} />
      <div className="relative bg-white w-full max-w-lg h-full shadow-xl flex flex-col">
        <div className="flex items-center gap-2 px-5 py-4 border-b border-koperasi-100 shrink-0">
          {stack.length > 1 && (
            <button
              type="button"
              onClick={() => setStack((s) => s.slice(0, -1))}
              className="p-1.5 -ml-1.5 rounded-lg text-koperasi-600 hover:bg-koperasi-50"
              aria-label={t('details.back')}
            >
              <ArrowLeft size={18} />
            </button>
          )}
          <h2 className="font-semibold text-koperasi-800 flex-1">{title}</h2>
          <button
            type="button"
            onClick={onClose}
            autoFocus
            className="p-1.5 rounded-lg text-koperasi-500 hover:bg-koperasi-50"
            aria-label={t('details.close')}
          >
            <X size={18} />
          </button>
        </div>
        <div className="flex-1 overflow-y-auto px-5 py-4">
          <Body key={`${current.entity}-${current.id}-${current.endpointBase ?? ''}`} target={current} navigate={navigate} />
        </div>
      </div>
    </div>
  );
}
