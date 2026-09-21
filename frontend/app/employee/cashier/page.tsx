'use client';

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { TriangleAlert } from 'lucide-react';
import { apiFetch, ApiError } from '@/lib/api';
import { useDebouncedValue } from '@/lib/useDebouncedValue';
import { useLanguage, type TKey, type Params } from '@/lib/i18n/LanguageContext';
import ReceiptView from '@/components/ReceiptView';
import type { CheckoutPreview, Discount, Item, Member, PaymentMethod, Transaction } from '@/lib/types';

interface CartLine {
  item: Item;
  quantity: number;
}

function formatRp(n: number) {
  return 'Rp ' + n.toLocaleString('id-ID');
}

/** One-line explanation of what the previewed discount does. */
function describePreviewDiscount(d: Discount, t: (key: TKey, params?: Params) => string): string {
  switch (d.type) {
    case 'percentage':
      return t('cashier.pctOff', { value: Number(d.value) });
    case 'flat':
      return t('cashier.flatOff', { value: formatRp(Number(d.value)) });
    case 'buy_x_get_y':
      return t('cashier.bxgy', { x: d.buy_qty ?? 0, y: d.get_qty ?? 0, cat: d.category ? ` (${d.category.name})` : '' });
  }
}

export default function CashierPage() {
  const [scanValue, setScanValue] = useState('');
  const [searchValue, setSearchValue] = useState('');
  const [searchResults, setSearchResults] = useState<Item[]>([]);
  const [cart, setCart] = useState<CartLine[]>([]);
  const [memberQuery, setMemberQuery] = useState('');
  const [member, setMember] = useState<Member | null>(null);
  const [memberResults, setMemberResults] = useState<Member[]>([]);
  const [memberError, setMemberError] = useState<string | null>(null);
  const [activeDiscounts, setActiveDiscounts] = useState<Discount[]>([]);
  const [explicitDiscountId, setExplicitDiscountId] = useState<string>('');
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>('cash');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [receipt, setReceipt] = useState<Transaction | null>(null);
  const [preview, setPreview] = useState<CheckoutPreview | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);
  const [previewError, setPreviewError] = useState<string | null>(null);
  const { t } = useLanguage();

  const scanInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    scanInputRef.current?.focus();
  }, [receipt]);

  useEffect(() => {
    apiFetch<Discount[]>(`/discounts/active?has_member=${member ? 1 : 0}`).then(setActiveDiscounts).catch(() => {});
  }, [member]);

  const subtotal = useMemo(
    () => cart.reduce((sum, l) => sum + Number(l.item.unit_price) * l.quantity, 0),
    [cart]
  );

  // Live price preview: same server-side math as checkout (discount rules,
  // tax rate), but read-only. Debounced so it refreshes once per pause while
  // the cashier edits the cart, and aborted when superseded.
  const previewRequest = JSON.stringify({
    items: cart.map((l) => ({ item_id: l.item.id, quantity: l.quantity })),
    member_identifier: member?.membership_id ?? null,
    discount_id: explicitDiscountId ? Number(explicitDiscountId) : null,
  });
  const debouncedPreviewRequest = useDebouncedValue(previewRequest, 400);

  useEffect(() => {
    const body = JSON.parse(debouncedPreviewRequest) as {
      items: { item_id: number; quantity: number }[];
      member_identifier: string | null;
      discount_id: number | null;
    };
    if (body.items.length === 0) {
      setPreview(null);
      setPreviewError(null);
      setPreviewLoading(false);
      return;
    }
    const controller = new AbortController();
    setPreviewLoading(true);
    apiFetch<CheckoutPreview>('/transactions/preview', {
      method: 'POST',
      body,
      signal: controller.signal,
    })
      .then((res) => {
        setPreview(res);
        setPreviewError(null);
      })
      .catch((err) => {
        if (controller.signal.aborted) return;
        setPreviewError(err instanceof ApiError ? err.message : t('cashier.previewFailed'));
      })
      .finally(() => {
        if (!controller.signal.aborted) setPreviewLoading(false);
      });
    return () => controller.abort();
  }, [debouncedPreviewRequest, t]);

  // Item search fires once per pause in typing (not per keystroke), and a
  // superseded request is aborted so a slow earlier response can never
  // overwrite newer results.
  const debouncedSearchValue = useDebouncedValue(searchValue, 250);

  useEffect(() => {
    const query = debouncedSearchValue.trim();
    if (query.length < 2) {
      setSearchResults([]);
      return;
    }
    const controller = new AbortController();
    apiFetch<{ data: Item[] }>(`/items?search=${encodeURIComponent(query)}&per_page=8`, {
      signal: controller.signal,
    })
      .then((res) => setSearchResults(res.data))
      .catch(() => {
        if (!controller.signal.aborted) setSearchResults([]);
      });
    return () => controller.abort();
  }, [debouncedSearchValue]);

  const addItemToCart = useCallback((item: Item) => {
    setCart((prev) => {
      const existing = prev.find((l) => l.item.id === item.id);
      if (existing) {
        return prev.map((l) => (l.item.id === item.id ? { ...l, quantity: l.quantity + 1 } : l));
      }
      return [...prev, { item, quantity: 1 }];
    });
    setSearchResults([]);
    setSearchValue('');
  }, []);

  async function handleScan(e: React.FormEvent) {
    e.preventDefault();
    if (!scanValue.trim()) return;
    try {
      const item = await apiFetch<Item>(`/items/scan?code=${encodeURIComponent(scanValue.trim())}`);
      addItemToCart(item);
      setScanValue('');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('cashier.scanFailed'));
    }
  }

  const updateQty = useCallback((itemId: number, quantity: number) => {
    if (quantity <= 0) {
      setCart((prev) => prev.filter((l) => l.item.id !== itemId));
    } else {
      setCart((prev) => prev.map((l) => (l.item.id === itemId ? { ...l, quantity } : l)));
    }
  }, []);

  async function handleMemberLookup(e: React.FormEvent) {
    e.preventDefault();
    setMemberError(null);
    const q = memberQuery.trim();
    if (!q) {
      setMember(null);
      setMemberResults([]);
      return;
    }
    try {
      const list = await apiFetch<Member[]>(`/members/lookup?query=${encodeURIComponent(q)}`);
      if (list.length === 1) {
        // Exact phone/ID/QR hit (or an unambiguous name) — attach directly.
        setMember(list[0]);
        setMemberResults([]);
      } else if (list.length > 1) {
        // Several members share the name — let the cashier pick.
        setMember(null);
        setMemberResults(list);
      } else {
        setMemberError(t('cashier.memberNotFound'));
        setMember(null);
        setMemberResults([]);
      }
    } catch {
      setMemberError(t('cashier.memberNotFound'));
      setMember(null);
      setMemberResults([]);
    }
  }

  async function handleCheckout() {
    if (cart.length === 0) return;
    setBusy(true);
    setError(null);
    try {
      const transaction = await apiFetch<Transaction>('/transactions/checkout', {
        method: 'POST',
        body: {
          items: cart.map((l) => ({ item_id: l.item.id, quantity: l.quantity })),
          member_identifier: member?.membership_id ?? null,
          discount_id: explicitDiscountId ? Number(explicitDiscountId) : null,
          payment_method: paymentMethod,
        },
      });
      setReceipt(transaction);
      setCart([]);
      setMember(null);
      setMemberResults([]);
      setMemberQuery('');
      setExplicitDiscountId('');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('cashier.checkoutFailed'));
    } finally {
      setBusy(false);
    }
  }

  if (receipt) {
    return <ReceiptView transaction={receipt} onNewSale={() => setReceipt(null)} />;
  }

  return (
    <div className="grid lg:grid-cols-3 gap-6">
      <div className="lg:col-span-2 space-y-4">
        <h1 className="text-2xl font-bold text-koperasi-800">{t('cashier.title')}</h1>

        <div className="card space-y-3">
          <form onSubmit={handleScan} className="flex gap-2">
            <input
              ref={scanInputRef}
              className="input"
              aria-label={t('cashier.scanAria')}
              placeholder={t('cashier.scanPlaceholder')}
              value={scanValue}
              onChange={(e) => setScanValue(e.target.value)}
              autoFocus
            />
            <button type="submit" className="btn-primary shrink-0">
              {t('cashier.add')}
            </button>
          </form>

          <div className="relative">
            <input
              className="input"
              aria-label={t('cashier.searchAria')}
              placeholder={t('cashier.searchPlaceholder')}
              value={searchValue}
              onChange={(e) => setSearchValue(e.target.value)}
            />
            {searchResults.length > 0 && (
              <div className="absolute z-10 bg-white border border-koperasi-200 rounded-lg shadow-lg w-full mt-1 max-h-64 overflow-y-auto">
                {searchResults.map((item) => (
                  <button
                    key={item.id}
                    className="w-full text-left px-3 py-2 text-sm hover:bg-koperasi-50 flex justify-between"
                    onClick={() => addItemToCart(item)}
                  >
                    <span>{item.name}</span>
                    <span className="text-koperasi-400">
                      {formatRp(Number(item.unit_price))} · {t('cashier.stockOf', { count: item.current_stock })}
                    </span>
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>

        <div className="card p-0 overflow-x-auto">
          <table className="data-table">
            <thead>
              <tr>
                <th>{t('cashier.colItem')}</th>
                <th>{t('cashier.colPrice')}</th>
                <th>{t('cashier.colQty')}</th>
                <th>{t('cashier.colSubtotal')}</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {cart.length === 0 && (
                <tr>
                  <td colSpan={5} className="text-center py-8 text-koperasi-400">
                    {t('cashier.cartEmpty')}
                  </td>
                </tr>
              )}
              {cart.map((l) => (
                <tr key={l.item.id}>
                  <td>{l.item.name}</td>
                  <td>{formatRp(Number(l.item.unit_price))}</td>
                  <td>
                    <div className="flex items-center gap-1">
                      <button
                        type="button"
                        aria-label={t('cashier.decreaseQty', { name: l.item.name })}
                        className="btn-secondary px-2 py-0.5"
                        onClick={() => updateQty(l.item.id, l.quantity - 1)}
                      >
                        −
                      </button>
                      <span className="w-8 text-center tabular-nums">{l.quantity}</span>
                      <button
                        type="button"
                        aria-label={t('cashier.increaseQty', { name: l.item.name })}
                        className="btn-secondary px-2 py-0.5"
                        onClick={() => updateQty(l.item.id, l.quantity + 1)}
                      >
                        +
                      </button>
                    </div>
                  </td>
                  <td className="tabular-nums">{formatRp(Number(l.item.unit_price) * l.quantity)}</td>
                  <td>
                    <button
                      type="button"
                      className="text-red-500 text-xs hover:underline"
                      onClick={() => updateQty(l.item.id, 0)}
                    >
                      {t('cashier.remove')}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="space-y-4">
        <div className="card space-y-3">
          <h2 className="font-semibold text-koperasi-800">{t('cashier.member')}</h2>
          <form onSubmit={handleMemberLookup} className="flex gap-2">
            <input
              className="input"
              aria-label={t('cashier.memberAria')}
              placeholder={t('cashier.memberPlaceholder')}
              value={memberQuery}
              onChange={(e) => setMemberQuery(e.target.value)}
            />
            <button type="submit" className="btn-secondary shrink-0">
              {t('cashier.find')}
            </button>
          </form>
          {memberError && (
            <p className="text-xs text-red-600" role="alert">
              {memberError}
            </p>
          )}
          {!member && memberResults.length > 0 && (
            <div className="space-y-1">
              <p className="text-xs font-medium text-koperasi-500">{t('cashier.selectMember')}</p>
              <div className="max-h-48 overflow-y-auto rounded-lg border border-koperasi-200 divide-y divide-koperasi-50">
                {memberResults.map((m) => (
                  <button
                    key={m.id}
                    type="button"
                    className="w-full text-left px-3 py-2 text-sm hover:bg-koperasi-50 flex justify-between gap-2"
                    onClick={() => {
                      setMember(m);
                      setMemberResults([]);
                    }}
                  >
                    <span className="font-medium min-w-0 truncate">{m.name}</span>
                    <span className="text-xs text-koperasi-500 shrink-0">{m.membership_id}</span>
                  </button>
                ))}
              </div>
            </div>
          )}
          {member && (
            <div className="bg-koperasi-50 rounded-lg p-3 text-sm flex justify-between items-center">
              <div>
                <div className="font-medium">{member.name}</div>
                <div className="text-xs text-koperasi-500">{member.membership_id}</div>
              </div>
              <button
                type="button"
                className="text-xs text-red-500 hover:underline"
                onClick={() => {
                  setMember(null);
                  setMemberResults([]);
                  setMemberQuery('');
                }}
              >
                {t('cashier.clear')}
              </button>
            </div>
          )}
        </div>

        <div className="card space-y-2">
          <h2 className="font-semibold text-koperasi-800">{t('cashier.activeDiscounts')}</h2>
          <select className="input" value={explicitDiscountId} onChange={(e) => setExplicitDiscountId(e.target.value)}>
            <option value="">{t('cashier.autoBest')}</option>
            {activeDiscounts.map((d) => (
              <option key={d.id} value={d.id}>
                {d.name} {d.scope === 'member' ? t('cashier.memberOnlySuffix') : ''}
              </option>
            ))}
          </select>
          {activeDiscounts.length === 0 && <p className="text-xs text-koperasi-400">{t('cashier.noPromos')}</p>}
        </div>

        <div className="card space-y-3">
          <h2 className="font-semibold text-koperasi-800">{t('cashier.payment')}</h2>
          <div className="grid grid-cols-3 gap-2">
            {(['cash', 'qris', 'bank_transfer'] as const).map((m) => (
              <button
                key={m}
                type="button"
                aria-pressed={paymentMethod === m}
                className={`py-2 rounded-lg text-sm border capitalize transition-colors ${
                  paymentMethod === m ? 'bg-koperasi-600 text-white border-koperasi-600' : 'border-koperasi-200 text-koperasi-600 hover:bg-koperasi-50'
                }`}
                onClick={() => setPaymentMethod(m)}
              >
                {t(`pay.${m === 'bank_transfer' ? 'bankTransfer' : m}` as TKey)}
              </button>
            ))}
          </div>

          <div className="flex justify-between text-lg font-bold pt-2 border-t border-koperasi-100 tabular-nums">
            <span>{t('cashier.subtotal')}</span>
            <span>{formatRp(subtotal)}</span>
          </div>

          {previewLoading && !preview && cart.length > 0 && <div className="skeleton h-24 rounded-lg" aria-label={t('cashier.loadingPreview')} />}

          {previewError && (
            <p className="text-xs text-red-600" role="alert">
              {previewError}
            </p>
          )}

          {preview && cart.length > 0 && (
            <div className="rounded-lg bg-koperasi-50 p-3 text-sm space-y-1 tabular-nums" aria-live="polite" aria-label={t('cashier.previewLabel')}>
              <div className="flex justify-between">
                <span>{t('cashier.subtotal')}</span>
                <span>{formatRp(preview.subtotal)}</span>
              </div>
              {preview.discount ? (
                <>
                  <div className="flex justify-between text-koperasi-700 font-medium">
                    <span>{t('cashier.discountWith', { name: preview.discount.name })}</span>
                    <span>− {formatRp(preview.discount_amount)}</span>
                  </div>
                  <p className="text-xs text-koperasi-500">{describePreviewDiscount(preview.discount, t)}</p>
                </>
              ) : (
                <div className="text-xs text-koperasi-400">{t('cashier.noDiscount')}</div>
              )}
              <div className="flex justify-between">
                <span>{preview.tax_rate ? t('cashier.taxWith', { rate: preview.tax_rate }) : t('cashier.taxPlain')}</span>
                <span>+ {formatRp(preview.tax_amount)}</span>
              </div>
              <div className="flex justify-between font-bold text-base border-t border-koperasi-200 pt-1">
                <span>{t('cashier.total')}</span>
                <span>{formatRp(preview.total)}</span>
              </div>
              {preview.warnings.map((w) => (
                <p key={w} className="text-xs text-amber-700 flex items-center gap-1" role="alert">
                  <TriangleAlert size={12} aria-hidden="true" className="shrink-0" /> {w}
                </p>
              ))}
            </div>
          )}
          <p className="text-xs text-koperasi-400">{t('cashier.liveNote')}</p>

          {error && (
            <p className="text-sm text-red-600" role="alert">
              {error}
            </p>
          )}

          <button type="button" className="btn-primary w-full tabular-nums" onClick={handleCheckout} disabled={busy || cart.length === 0}>
            {busy ? t('cashier.processing') : t('cashier.charge', { amount: formatRp(subtotal) })}
          </button>
        </div>
      </div>
    </div>
  );
}

