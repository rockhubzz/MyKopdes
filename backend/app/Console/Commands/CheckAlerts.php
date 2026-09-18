<?php

namespace App\Console\Commands;

use App\Models\Item;
use Illuminate\Console\Command;
use Illuminate\Support\Facades\Cache;

class CheckAlerts extends Command
{
    protected $signature = 'koperasi:check-alerts';
    protected $description = 'Refresh the cached low-stock / expiring-soon lists shown as dashboard notifications';

    public function handle(): int
    {
        $lowStock = Item::query()->where('is_active', true)->lowStock()->get(['id', 'name', 'current_stock', 'min_stock_threshold']);

        $expiringSoon = Item::query()
            ->where('is_active', true)
            ->whereNotNull('expiry_date')
            ->whereBetween('expiry_date', [now(), now()->addDays(7)])
            ->get(['id', 'name', 'expiry_date']);

        // Cached with a short TTL and read directly by
        // Api/DashboardController so the alert bell doesn't recompute this
        // query on every request across every role's dashboard.
        Cache::put('koperasi:alerts:low_stock', $lowStock, now()->addMinutes(20));
        Cache::put('koperasi:alerts:expiring_soon', $expiringSoon, now()->addMinutes(20));

        $this->info("Low stock: {$lowStock->count()}, expiring soon: {$expiringSoon->count()}");

        return self::SUCCESS;
    }
}
