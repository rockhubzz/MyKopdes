<?php

use App\Services\SettingsService;
use Illuminate\Foundation\Inspiring;
use Illuminate\Support\Facades\Artisan;
use Illuminate\Support\Facades\Schedule;

Artisan::command('inspire', function () {
    $this->comment(Inspiring::quote());
})->purpose('Display an inspiring quote');

// Database backup automation. The cron expression is configurable at
// runtime from Admin > Settings > Backup (stored in the `settings` table);
// this schedule entry re-reads it every tick so an admin edit takes effect
// without a redeploy.
Schedule::call(function () {
    Artisan::call('koperasi:backup');
})->cron(fn () => app(SettingsService::class)->get('backup_schedule_cron', config('services.backup.default_cron', '0 2 * * *')))
  ->name('koperasi-db-backup')
  ->onOneServer();

// Expiry / low-stock alert sweep, feeds the dashboard notification bell.
Schedule::command('koperasi:check-alerts')->everyFifteenMinutes();
