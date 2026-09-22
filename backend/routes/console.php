<?php

use App\Services\SettingsService;
use Illuminate\Foundation\Inspiring;
use Illuminate\Support\Facades\Artisan;
use Illuminate\Support\Facades\Schedule;

Artisan::command('inspire', function () {
    $this->comment(Inspiring::quote());
})->purpose('Display an inspiring quote');

// Database backup automation. The cron expression is configurable at
// runtime from Admin > Settings > Backup (stored in the `settings` table).
//
// Implementation note: `->cron()` only accepts a static string — passing a
// Closure (as this file once did) crashes both `schedule:run` and
// `schedule:list`, because the scheduler hands the expression straight to
// CronExpression. So the entry ticks every minute and the *gate* re-reads
// the setting each tick: an admin edit still takes effect without a redeploy.
Schedule::call(function () {
    Artisan::call('koperasi:backup');
})->everyMinute()
  ->when(function () {
      $expr = trim((string) app(SettingsService::class)->get('backup_schedule_cron', config('services.backup.default_cron', '0 2 * * *')));
      try {
          return (new Cron\CronExpression($expr))->isDue(now()->toDateTimeString());
      } catch (Throwable) {
          // A mistyped expression in Settings must never kill the scheduler.
          return false;
      }
  })
  ->name('koperasi-db-backup')
  ->onOneServer();

// Expiry / low-stock alert sweep, feeds the dashboard notification bell.
Schedule::command('koperasi:check-alerts')->everyFifteenMinutes();
