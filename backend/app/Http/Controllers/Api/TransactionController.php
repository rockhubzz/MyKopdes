<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\AuditLog;
use App\Models\Item;
use App\Models\Member;
use App\Models\Transaction;
use App\Models\TransactionItem;
use App\Services\DiscountService;
use App\Services\SettingsService;
use Illuminate\Http\Request;
use Illuminate\Support\Collection;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Str;
use Illuminate\Validation\Rule;
use Illuminate\Validation\ValidationException;

class TransactionController extends Controller
{
    public function __construct(
        private readonly DiscountService $discounts,
        private readonly SettingsService $settings,
    ) {
    }

    public function index(Request $request)
    {
        $query = Transaction::with(['member', 'cashier', 'discount', 'items.item']);

        // Employees see only their own shift's transactions unless they
        // pass their own cashier_id explicitly; Shop Owner/Admin can see
        // everything (route-gated combination, see routes/api.php).
        if ($request->user('staff')->isEmployee()) {
            $query->where('cashier_id', $request->user('staff')->id);
        }

        if ($request->filled('from')) {
            $query->whereDate('created_at', '>=', $request->date('from'));
        }
        if ($request->filled('to')) {
            $query->whereDate('created_at', '<=', $request->date('to'));
        }
        if ($request->filled('payment_method')) {
            $query->where('payment_method', $request->string('payment_method'));
        }
        if ($request->filled('member_id')) {
            $query->where('member_id', $request->integer('member_id'));
        }

        return $query->latest()->paginate($request->integer('per_page', 20));
    }

    public function show(Transaction $transaction)
    {
        return response()->json($transaction->load(['member', 'cashier', 'discount', 'items.item']));
    }

    /**
     * Price preview for Cashier Mode. Same body as checkout() minus
     * payment_method, same pricing math, but strictly read-only: no row
     * locks, no stock changes, no transaction/shu/audit writes. Lets the
     * cashier show the customer the discount breakdown before charging.
     */
    public function preview(Request $request)
    {
        $data = $request->validate([
            'items' => ['required', 'array', 'min:1'],
            'items.*.item_id' => ['required', 'exists:items,id'],
            'items.*.quantity' => ['required', 'integer', 'min:1'],
            'member_identifier' => ['nullable', 'string'],
            'discount_id' => ['nullable', 'exists:discounts,id'],
        ]);

        $itemIds = collect($data['items'])->pluck('item_id')->unique()->values();
        $items = Item::whereIn('id', $itemIds)->get()->keyBy('id');

        $warnings = [];
        $cartLines = collect($data['items'])->map(function ($line) use ($items, &$warnings) {
            $item = $items[$line['item_id']] ?? null;
            if (! $item || ! $item->is_active) {
                throw ValidationException::withMessages(['items' => ["Item #{$line['item_id']} is not available."]]);
            }
            if ($item->current_stock < $line['quantity']) {
                $warnings[] = "Only {$item->current_stock} × \"{$item->name}\" in stock.";
            }
            $subtotal = round((float) $item->unit_price * $line['quantity'], 2);

            return [
                'item_id' => $item->id,
                'name' => $item->name,
                'quantity' => $line['quantity'],
                'unit_price' => (float) $item->unit_price,
                'subtotal' => $subtotal,
            ];
        });

        $member = $this->resolveMember($data['member_identifier'] ?? null);

        $subtotal = round((float) $cartLines->sum('subtotal'), 2);
        ['discount' => $discount, 'amount' => $discountAmount] = $this->discounts->bestDiscountFor(
            // bestDiscountFor only reads item/category_id/quantity/subtotal
            // off each line, so map the Eloquent items back into its shape.
            $cartLines->map(fn ($l) => [
                'item' => $items[$l['item_id']],
                'quantity' => $l['quantity'],
                'subtotal' => $l['subtotal'],
            ]),
            hasMember: (bool) $member,
            explicitDiscountId: $data['discount_id'] ?? null,
        );

        $taxRate = (float) $this->settings->get('tax_rate', 0);
        $taxableBase = max($subtotal - $discountAmount, 0);
        $taxAmount = round($taxableBase * ($taxRate / 100), 2);
        $total = round($taxableBase + $taxAmount, 2);

        return response()->json([
            'lines' => $cartLines->values(),
            'subtotal' => $subtotal,
            'discount' => $discount?->load('category'),
            'discount_amount' => $discountAmount,
            'tax_rate' => $taxRate,
            'tax_amount' => $taxAmount,
            'total' => $total,
            'member' => $member ? $member->only(['id', 'name', 'membership_id']) : null,
            'warnings' => $warnings,
        ]);
    }

    /**
     * Checkout. Body:
     * {
     *   "items": [{"item_id": 1, "quantity": 2}, ...],
     *   "member_identifier": "KOP-26-ABCDE" | phone | null,
     *   "discount_id": null | int,   // explicit override; omit for auto-best-fit
     *   "payment_method": "cash" | "qris" | "bank_transfer"
     * }
     */
    public function checkout(Request $request)
    {
        $data = $request->validate([
            'items' => ['required', 'array', 'min:1'],
            'items.*.item_id' => ['required', 'exists:items,id'],
            'items.*.quantity' => ['required', 'integer', 'min:1'],
            'member_identifier' => ['nullable', 'string'],
            'discount_id' => ['nullable', 'exists:discounts,id'],
            'payment_method' => ['required', Rule::in([
                Transaction::PAYMENT_CASH, Transaction::PAYMENT_QRIS, Transaction::PAYMENT_BANK_TRANSFER,
            ])],
        ]);

        $transaction = DB::transaction(function () use ($data, $request) {
            // Lock every line's item row up front (ordered by id to avoid
            // deadlocks between concurrent cashiers) and verify stock.
            $itemIds = collect($data['items'])->pluck('item_id')->unique()->sort()->values();
            $items = Item::whereIn('id', $itemIds)->lockForUpdate()->get()->keyBy('id');

            $cartLines = collect($data['items'])->map(function ($line) use ($items) {
                $item = $items[$line['item_id']] ?? null;
                if (! $item || ! $item->is_active) {
                    throw ValidationException::withMessages(['items' => ["Item #{$line['item_id']} is not available."]]);
                }
                if ($item->current_stock < $line['quantity']) {
                    throw ValidationException::withMessages(['items' => ["Not enough stock for \"{$item->name}\" (have {$item->current_stock}, requested {$line['quantity']})."]]);
                }
                $subtotal = round((float) $item->unit_price * $line['quantity'], 2);

                return ['item' => $item, 'quantity' => $line['quantity'], 'unit_price' => $item->unit_price, 'subtotal' => $subtotal];
            });

            $member = $this->resolveMember($data['member_identifier'] ?? null);

            $subtotal = round((float) $cartLines->sum('subtotal'), 2);
            ['discount' => $discount, 'amount' => $discountAmount] = $this->discounts->bestDiscountFor(
                $cartLines, hasMember: (bool) $member, explicitDiscountId: $data['discount_id'] ?? null,
            );

            $taxRate = (float) $this->settings->get('tax_rate', 0);
            $taxableBase = max($subtotal - $discountAmount, 0);
            $taxAmount = round($taxableBase * ($taxRate / 100), 2);
            $total = round($taxableBase + $taxAmount, 2);

            $transaction = Transaction::create([
                'transaction_code' => 'TRX-'.now()->format('ymd').'-'.Str::upper(Str::random(6)),
                'member_id' => $member?->id,
                'cashier_id' => $request->user('staff')->id,
                'subtotal' => $subtotal,
                'discount_id' => $discount?->id,
                'discount_amount' => $discountAmount,
                'tax_amount' => $taxAmount,
                'total' => $total,
                'payment_method' => $data['payment_method'],
                'payment_status' => Transaction::STATUS_PAID,
                'paid_at' => now(),
            ]);

            foreach ($cartLines as $line) {
                TransactionItem::create([
                    'transaction_id' => $transaction->id,
                    'item_id' => $line['item']->id,
                    'quantity' => $line['quantity'],
                    'unit_price' => $line['unit_price'],
                    'subtotal' => $line['subtotal'],
                ]);

                // The ONLY place in the app where stock is decremented.
                $line['item']->decrement('current_stock', $line['quantity']);
            }

            if ($member) {
                $shuRate = (float) $this->settings->get('shu_rate', 0.02);
                $member->increment('shu_balance', round($total * $shuRate, 2));
            }

            AuditLog::create([
                'actor_type' => \App\Models\User::class,
                'actor_id' => $request->user('staff')->id,
                'action' => 'checkout',
                'auditable_type' => Transaction::class,
                'auditable_id' => $transaction->id,
                'changes' => ['total' => $total, 'payment_method' => $data['payment_method'], 'member_id' => $member?->id],
                'ip_address' => $request->ip(),
                'created_at' => now(),
            ]);

            return $transaction;
        });

        return response()->json($transaction->load(['member', 'cashier', 'discount', 'items.item']), 201);
    }

    /** Shop Owner/Admin only — reverses a mistaken sale, restoring stock. */
    public function void(Transaction $transaction)
    {
        if ($transaction->payment_status === Transaction::STATUS_VOID) {
            return response()->json(['message' => 'Already voided.']);
        }

        DB::transaction(function () use ($transaction) {
            foreach ($transaction->items as $line) {
                $line->item()->lockForUpdate()->first()?->increment('current_stock', $line->quantity);
            }
            if ($transaction->member_id) {
                $shuRate = (float) $this->settings->get('shu_rate', 0.02);
                $transaction->member->decrement('shu_balance', round((float) $transaction->total * $shuRate, 2));
            }
            $transaction->update(['payment_status' => Transaction::STATUS_VOID]);
        });

        return response()->json($transaction->fresh());
    }

    /** Printable receipt data; the frontend renders this as a PDF/thermal ticket. */
    public function receipt(Transaction $transaction)
    {
        return response()->json([
            'transaction' => $transaction->load(['member', 'cashier', 'discount', 'items.item']),
            'store' => [
                'name' => $this->settings->get('store_name', config('app.name')),
                'address' => $this->settings->get('store_address', ''),
            ],
        ]);
    }

    private function resolveMember(?string $identifier): ?Member
    {
        if (! $identifier) {
            return null;
        }

        $member = Member::where('membership_id', $identifier)->orWhere('phone', $identifier)->first();

        if (! $member || ! $member->is_active) {
            throw ValidationException::withMessages(['member_identifier' => ['No active member matches that ID/phone.']]);
        }

        return $member;
    }
}
