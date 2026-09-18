<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Item;
use App\Models\Transaction;
use App\Services\ReportService;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Cache;

class DashboardController extends Controller
{
    public function __construct(private readonly ReportService $reports)
    {
    }

    public function alerts()
    {
        return response()->json([
            'low_stock' => Cache::get('koperasi:alerts:low_stock', fn () => Item::where('is_active', true)->lowStock()->get(['id', 'name', 'current_stock', 'min_stock_threshold'])),
            'expiring_soon' => Cache::get('koperasi:alerts:expiring_soon', fn () => Item::where('is_active', true)->whereNotNull('expiry_date')->whereBetween('expiry_date', [now(), now()->addDays(7)])->get(['id', 'name', 'expiry_date'])),
        ]);
    }

    /** Admin / Shop Owner: full store health snapshot. */
    public function ownerSummary()
    {
        $today = $this->reports->summary(now()->startOfDay(), now()->endOfDay());
        $month = $this->reports->summary(now()->startOfMonth(), now()->endOfDay());

        return response()->json([
            'today' => $today,
            'month_to_date' => $month,
            'best_sellers' => $this->reports->bestSellingItems(now()->startOfMonth(), now()->endOfDay(), 5),
            'stock' => $this->reports->stockValuation(),
        ]);
    }

    /** Employee: their own shift snapshot. */
    public function employeeSummary(Request $request)
    {
        $userId = $request->user('staff')->id;

        $today = Transaction::where('cashier_id', $userId)
            ->whereDate('created_at', now())
            ->where('payment_status', Transaction::STATUS_PAID);

        return response()->json([
            'transactions_today' => (clone $today)->count(),
            'revenue_today' => round((float) (clone $today)->sum('total'), 2),
            'restocks_submitted_today' => $request->user('staff')->restockingRecords()->whereDate('restocked_at', now())->count(),
        ]);
    }

    /** Member: their own portal snapshot. */
    public function memberSummary(Request $request)
    {
        $member = $request->user('member');

        return response()->json([
            'shu_balance' => $member->shu_balance,
            'total_purchases' => $member->transactions()->where('payment_status', Transaction::STATUS_PAID)->count(),
            'total_spent' => round((float) $member->transactions()->where('payment_status', Transaction::STATUS_PAID)->sum('total'), 2),
            'recent_transactions' => $member->transactions()->with('items.item')->latest()->limit(5)->get(),
        ]);
    }
}
