'use client';

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { apiFetch, ApiError } from '@/lib/api';
import { useDebouncedValue } from '@/lib/useDebouncedValue';
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
function describePreviewDiscount(d: Discount): string {
  switch (d.type) {
    case 'percentage':
      return `${Number(d.value)}% off the subtotal`;
    case 'flat':
      return `${formatRp(Number(d.value))} off the subtotal`;
    case 'buy_x_get_y':
      return `Buy ${d.buy_qty}, get ${d.get_qty} free${d.category ? ` (${d.category.name})` : ''}`;
  }
}

export default function CashierPage() {
  const [scanValue, setScanValue] = useState('');
  const [searchValue, setSearchValue] = useState('');
  const [searchResults, setSearchResults] = useState<Item[]>([]);
  const [cart, setCart] = useState<CartLine[]>([]);
  const [memberQuery, setMemberQuery] = useState('');
  const [member, setMember] = useState<Member | null>(null);
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
        setPreviewError(err instanceof ApiError ? err.message : 'Could not preview totals.');
      })
      .finally(() => {
        if (!controller.signal.aborted) setPreviewLoading(false);
      });
    return () => controller.abort();
  }, [debouncedPreviewRequest]);

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
      setError(err instanceof ApiError ? err.message : 'Item not found.');
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
    if (!memberQuery.trim()) {
      setMember(null);
      return;
    }
    try {
      const m = await apiFetch<Member>(`/members/lookup?query=${encodeURIComponent(memberQuery.trim())}`);
      setMember(m);
    } catch {
      setMemberError('No active member found for that phone/ID.');
      setMember(null);
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
      setMemberQuery('');
      setExplicitDiscountId('');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Checkout failed.');
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
        <h1 className="text-2xl font-bold text-koperasi-800">Cashier Mode</h1>

        <div className="card space-y-3">
          <form onSubmit={handleScan} className="flex gap-2">
            <input
              ref={scanInputRef}
              className="input"
              aria-label="Scan barcode or SKU"
              placeholder="Scan barcode or type SKU, then press Enter..."
              value={scanValue}
              onChange={(e) => setScanValue(e.target.value)}
              autoFocus
            />
            <button type="submit" className="btn-primary shrink-0">
              Add
            </button>
          </form>

          <div className="relative">
            <input
              className="input"
              aria-label="Search items by name"
              placeholder="Or search by item name..."
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
                      {formatRp(Number(item.unit_price))} · stock {item.current_stock}
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
                <th>Item</th>
                <th>Price</th>
                <th>Qty</th>
                <th>Subtotal</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {cart.length === 0 && (
                <tr>
                  <td colSpan={5} className="text-center py-8 text-koperasi-400">
                    Cart is empty. Scan or search for an item to begin.
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
                        aria-label={`Decrease quantity of ${l.item.name}`}
                        className="btn-secondary px-2 py-0.5"
                        onClick={() => updateQty(l.item.id, l.quantity - 1)}
                      >
                        −
                      </button>
                      <span className="w-8 text-center tabular-nums">{l.quantity}</span>
                      <button
                        type="button"
                        aria-label={`Increase quantity of ${l.item.name}`}
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
                      Remove
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
          <h2 className="font-semibold text-koperasi-800">Member</h2>
          <form onSubmit={handleMemberLookup} className="flex gap-2">
            <input
              className="input"
              aria-label="Member phone, ID, or QR"
              placeholder="Phone / Membership ID / QR"
              value={memberQuery}
              onChange={(e) => setMemberQuery(e.target.value)}
            />
            <button type="submit" className="btn-secondary shrink-0">
              Find
            </button>
          </form>
          {memberError && (
            <p className="text-xs text-red-600" role="alert">
              {memberError}
            </p>
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
                  setMemberQuery('');
                }}
              >
                Clear
              </button>
            </div>
          )}
        </div>

        <div className="card space-y-2">
          <h2 className="font-semibold text-koperasi-800">Active Discounts</h2>
          <select className="input" value={explicitDiscountId} onChange={(e) => setExplicitDiscountId(e.target.value)}>
            <option value="">Auto-apply best discount</option>
            {activeDiscounts.map((d) => (
              <option key={d.id} value={d.id}>
                {d.name} {d.scope === 'member' ? '(member-only)' : ''}
              </option>
            ))}
          </select>
          {activeDiscounts.length === 0 && <p className="text-xs text-koperasi-400">No active promotions right now.</p>}
        </div>

        <div className="card space-y-3">
          <h2 className="font-semibold text-koperasi-800">Payment</h2>
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
                {m.replace('_', ' ')}
              </button>
            ))}
          </div>

          <div className="flex justify-between text-lg font-bold pt-2 border-t border-koperasi-100 tabular-nums">
            <span>Subtotal</span>
            <span>{formatRp(subtotal)}</span>
          </div>

          {previewLoading && !preview && cart.length > 0 && <div className="skeleton h-24 rounded-lg" aria-label="Loading price preview" />}

          {previewError && (
            <p className="text-xs text-red-600" role="alert">
              {previewError}
            </p>
          )}

          {preview && cart.length > 0 && (
            <div className="rounded-lg bg-koperasi-50 p-3 text-sm space-y-1 tabular-nums" aria-live="polite" aria-label="Price preview">
              <div className="flex justify-between">
                <span>Subtotal</span>
                <span>{formatRp(preview.subtotal)}</span>
              </div>
              {preview.discount ? (
                <>
                  <div className="flex justify-between text-koperasi-700 font-medium">
                    <span>Discount · {preview.discount.name}</span>
                    <span>− {formatRp(preview.discount_amount)}</span>
                  </div>
                  <p className="text-xs text-koperasi-500">{describePreviewDiscount(preview.discount)}</p>
                </>
              ) : (
                <div className="text-xs text-koperasi-400">No discount applies to this sale.</div>
              )}
              <div className="flex justify-between">
                <span>Tax{preview.tax_rate ? ` (${preview.tax_rate}%)` : ''}</span>
                <span>+ {formatRp(preview.tax_amount)}</span>
              </div>
              <div className="flex justify-between font-bold text-base border-t border-koperasi-200 pt-1">
                <span>Total</span>
                <span>{formatRp(preview.total)}</span>
              </div>
              {preview.warnings.map((w) => (
                <p key={w} className="text-xs text-amber-700" role="alert">
                  ⚠ {w}
                </p>
              ))}
            </div>
          )}
          <p className="text-xs text-koperasi-400">Live server estimate — the printed receipt shows this same breakdown.</p>

          {error && (
            <p className="text-sm text-red-600" role="alert">
              {error}
            </p>
          )}

          <button type="button" className="btn-primary w-full tabular-nums" onClick={handleCheckout} disabled={busy || cart.length === 0}>
            {busy ? 'Processing...' : `Charge ${formatRp(subtotal)}`}
          </button>
        </div>
      </div>
    </div>
  );
}

