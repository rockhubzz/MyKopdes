<?php

// Application-specific defaults. These seed the runtime-editable `settings`
// table on first migrate/seed; after that, admins change behavior from the
// Admin > Settings screens and the DB row is authoritative (see
// App\Services\SettingsService). Nothing here needs a redeploy to change
// EXCEPT the values under 'bootstrap' (used before the DB is reachable).
return [
    'defaults' => [
        'tax_rate' => (float) env('DEFAULT_TAX_RATE', 0),
        'shu_rate' => (float) env('DEFAULT_SHU_RATE', 0.02),
        'low_stock_threshold' => (int) env('LOW_STOCK_DEFAULT_THRESHOLD', 10),
        'backup_schedule_cron' => env('BACKUP_SCHEDULE_CRON', '0 2 * * *'),
        'api_base_url' => env('APP_URL', 'http://localhost'),
    ],
];
