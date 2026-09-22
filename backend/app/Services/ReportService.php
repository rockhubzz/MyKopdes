<?php

namespace App\Services;

use App\Models\Item;
use App\Models\Transaction;
use App\Models\TransactionItem;
use Illuminate\Support\Carbon;
use Illuminate\Support\Facades\DB;

class ReportService
{
    public function summary(Carbon $from, Carbon $to): array
    {
        // One aggregate row instead of four round trips (revenue, discount,
        // tax, count each used to be its own SELECT).
        $row = Transaction::whereBetween('created_at', [$from, $to])
            ->where('payment_status', Transaction::STATUS_PAID)
            ->selectRaw('COALESCE(SUM(total), 0) as revenue')
            ->selectRaw('COALESCE(SUM(discount_amount), 0) as discount_given')
            ->selectRaw('COALESCE(SUM(tax_amount), 0) as tax_collected')
            ->selectRaw('COUNT(*) as transaction_count')
            ->first();

        $revenue = (float) ($row->revenue ?? 0);
        $transactionCount = (int) ($row->transaction_count ?? 0);

        $costOfGoodsSold = (float) TransactionItem::query()
            ->join('transactions', 'transactions.id', '=', 'transaction_items.transaction_id')
            ->join('items', 'items.id', '=', 'transaction_items.item_id')
            ->whereBetween('transactions.created_at', [$from, $to])
            ->where('transactions.payment_status', Transaction::STATUS_PAID)
            ->sum(DB::raw('transaction_items.quantity * items.cost_price'));

        $profit = $revenue - $costOfGoodsSold;

        return [
            'period' => ['from' => $from->toDateString(), 'to' => $to->toDateString()],
            'revenue' => round($revenue, 2),
            'cost_of_goods_sold' => round($costOfGoodsSold, 2),
            'profit' => round($profit, 2),
            'profit_margin_pct' => $revenue > 0 ? round(($profit / $revenue) * 100, 2) : 0,
            'discount_given' => round((float) ($row->discount_given ?? 0), 2),
            'tax_collected' => round((float) ($row->tax_collected ?? 0), 2),
            'transaction_count' => $transactionCount,
            'average_basket' => $transactionCount > 0 ? round($revenue / $transactionCount, 2) : 0,
        ];
    }

    public function bestSellingItems(Carbon $from, Carbon $to, int $limit = 10): array
    {
        $limit = min(max($limit, 1), 50);

        return TransactionItem::query()            ->join('transactions', 'transactions.id', '=', 'transaction_items.transaction_id')
            ->join('items', 'items.id', '=', 'transaction_items.item_id')
            ->whereBetween('transactions.created_at', [$from, $to])
            ->where('transactions.payment_status', Transaction::STATUS_PAID)
            ->select('items.id', 'items.name', 'items.sku')
            ->selectRaw('SUM(transaction_items.quantity) as units_sold')
            ->selectRaw('SUM(transaction_items.subtotal) as revenue')
            ->groupBy('items.id', 'items.name', 'items.sku')
            ->orderByDesc('units_sold')
            ->limit($limit)
            ->get()
            ->toArray();
    }

    /**
     * Stock valuation totals are computed in SQL (one aggregate row) instead
     * of hydrating every item into PHP. The per-item breakdown is opt-in via
     * $withItems — the dashboards and reports screens only render the totals.
     */
    public function stockValuation(bool $withItems = false): array
    {
        $totals = Item::query()
            ->selectRaw('COALESCE(SUM(current_stock * cost_price), 0) as at_cost')
            ->selectRaw('COALESCE(SUM(current_stock * unit_price), 0) as at_retail')
            ->first();

        $atCost = (float) ($totals->at_cost ?? 0);
        $atRetail = (float) ($totals->at_retail ?? 0);

        $result = [
            'total_value_at_cost' => round($atCost, 2),
            'total_value_at_retail' => round($atRetail, 2),
            'potential_profit_if_all_sold' => round($atRetail - $atCost, 2),
            'low_stock_count' => Item::lowStock()->count(),
        ];

        if ($withItems) {
            $items = Item::query()->select('id', 'name', 'sku', 'current_stock', 'cost_price', 'unit_price')->get();
            $result['items'] = $items->map(fn ($i) => [
                'id' => $i->id,
                'name' => $i->name,
                'sku' => $i->sku,
                'current_stock' => $i->current_stock,
                'value_at_cost' => round($i->current_stock * (float) $i->cost_price, 2),
                'value_at_retail' => round($i->current_stock * (float) $i->unit_price, 2),
            ]);
        }

        return $result;
    }

    /** Daily revenue series, useful for a dashboard sparkline / bar chart. */
    public function dailySeries(Carbon $from, Carbon $to): array
    {
        return Transaction::query()
            ->whereBetween('created_at', [$from, $to])
            ->where('payment_status', Transaction::STATUS_PAID)
            ->selectRaw('DATE(created_at) as date')
            ->selectRaw('SUM(total) as revenue')
            ->selectRaw('COUNT(*) as transactions')
            ->groupBy('date')
            ->orderBy('date')
            ->get()
            ->toArray();
    }
}
