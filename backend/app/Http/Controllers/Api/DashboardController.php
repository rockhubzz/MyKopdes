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
        // NOTE: this used to be Cache::get($key, $closure) — which evaluates
        // the closure on every miss WITHOUT storing anything, i.e. it queried
        // on every single dashboard visit. remember() actually caches.
        // 60s TTL keeps the badge fresh enough while making repeat visits free.
        return response()->json([
            'low_stock' => Cache::remember('koperasi:alerts:low_stock', 60, fn () => Item::where('is_active', true)->lowStock()->get(['id', 'name', 'current_stock', 'min_stock_threshold'])),
            'expiring_soon' => Cache::remember('koperasi:alerts:expiring_soon', 60, fn () => Item::where('is_active', true)->whereNotNull('expiry_date')->whereBetween('expiry_date', [now(), now()->addDays(7)])->get(['id', 'name', 'expiry_date'])),
        ]);
    }

    /** Admin / Shop Owner: full store health snapshot. */
    public function ownerSummary()
    {
        // The underlying ranges are "today" and "this month" — the key embeds
        // the date so a midnight rollover can never serve yesterday's figures,
        // and the short TTL absorbs repeat visits / double-mounts in dev.
        return response()->json(
            Cache::remember('koperasi:dash:owner-summary:'.now()->toDateString(), 60, function () {
                return [
                    'today' => $this->reports->summary(now()->startOfDay(), now()->endOfDay()),
                    'month_to_date' => $this->reports->summary(now()->startOfMonth(), now()->endOfDay()),
                    'best_sellers' => $this->reports->bestSellingItems(now()->startOfMonth(), now()->endOfDay(), 5),
                    'stock' => $this->reports->stockValuation(),
                ];
            })
        );
    }

    /** Employee: their own shift snapshot. */
    public function employeeSummary(Request $request)
    {
        $userId = $request->user('staff')->id;
        $bounds = [now()->startOfDay(), now()->endOfDay()];

        // Single aggregate row instead of count() + sum() round trips, and a
        // between-bounds predicate so the (cashier_id, created_at) index
        // applies (whereDate() wraps the column and defeats it).
        $row = Transaction::where('cashier_id', $userId)
            ->whereBetween('created_at', $bounds)
            ->where('payment_status', Transaction::STATUS_PAID)
            ->selectRaw('COUNT(*) as transactions_today')
            ->selectRaw('COALESCE(SUM(total), 0) as revenue_today')
            ->first();

        return response()->json([
            'transactions_today' => (int) ($row->transactions_today ?? 0),
            'revenue_today' => round((float) ($row->revenue_today ?? 0), 2),
            'restocks_submitted_today' => $request->user('staff')->restockingRecords()->whereBetween('restocked_at', $bounds)->count(),
        ]);
    }

    /** Member: their own portal snapshot. */
    public function memberSummary(Request $request)
    {
        $member = $request->user('member');

        // One aggregate instead of separate count() + sum() queries.
        $row = $member->transactions()
            ->where('payment_status', Transaction::STATUS_PAID)
            ->selectRaw('COUNT(*) as total_purchases')
            ->selectRaw('COALESCE(SUM(total), 0) as total_spent')
            ->first();

        return response()->json([
            'shu_balance' => $member->shu_balance,
            'total_purchases' => (int) ($row->total_purchases ?? 0),
            'total_spent' => round((float) ($row->total_spent ?? 0), 2),
            // The member dashboard renders date/code/total per row only —
            // no item lines, so skip the items.item eager load entirely.
            'recent_transactions' => $member->transactions()->latest()->limit(5)->get(),
        ]);
    }
}
