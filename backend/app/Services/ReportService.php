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
        $transactions = Transaction::whereBetween('created_at', [$from, $to])
            ->where('payment_status', Transaction::STATUS_PAID);

        $revenue = (clone $transactions)->sum('total');
        $discountGiven = (clone $transactions)->sum('discount_amount');
        $taxCollected = (clone $transactions)->sum('tax_amount');
        $transactionCount = (clone $transactions)->count();

        $costOfGoodsSold = TransactionItem::query()
            ->join('transactions', 'transactions.id', '=', 'transaction_items.transaction_id')
            ->join('items', 'items.id', '=', 'transaction_items.item_id')
            ->whereBetween('transactions.created_at', [$from, $to])
            ->where('transactions.payment_status', Transaction::STATUS_PAID)
            ->sum(DB::raw('transaction_items.quantity * items.cost_price'));

        $profit = $revenue - $costOfGoodsSold;

        return [
            'period' => ['from' => $from->toDateString(), 'to' => $to->toDateString()],
            'revenue' => round((float) $revenue, 2),
            'cost_of_goods_sold' => round((float) $costOfGoodsSold, 2),
            'profit' => round((float) $profit, 2),
            'profit_margin_pct' => $revenue > 0 ? round(($profit / $revenue) * 100, 2) : 0,
            'discount_given' => round((float) $discountGiven, 2),
            'tax_collected' => round((float) $taxCollected, 2),
            'transaction_count' => $transactionCount,
            'average_basket' => $transactionCount > 0 ? round($revenue / $transactionCount, 2) : 0,
        ];
    }

    public function bestSellingItems(Carbon $from, Carbon $to, int $limit = 10): array
    {
        return TransactionItem::query()
            ->join('transactions', 'transactions.id', '=', 'transaction_items.transaction_id')
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

    public function stockValuation(): array
    {
        $items = Item::query()->select('id', 'name', 'sku', 'current_stock', 'cost_price', 'unit_price')->get();

        $atCost = $items->sum(fn ($i) => $i->current_stock * (float) $i->cost_price);
        $atRetail = $items->sum(fn ($i) => $i->current_stock * (float) $i->unit_price);

        return [
            'total_value_at_cost' => round($atCost, 2),
            'total_value_at_retail' => round($atRetail, 2),
            'potential_profit_if_all_sold' => round($atRetail - $atCost, 2),
            'low_stock_count' => Item::lowStock()->count(),
            'items' => $items->map(fn ($i) => [
                'id' => $i->id,
                'name' => $i->name,
                'sku' => $i->sku,
                'current_stock' => $i->current_stock,
                'value_at_cost' => round($i->current_stock * (float) $i->cost_price, 2),
                'value_at_retail' => round($i->current_stock * (float) $i->unit_price, 2),
            ]),
        ];
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
