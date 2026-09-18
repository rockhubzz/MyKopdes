<?php

namespace App\Providers;

use Illuminate\Cache\RateLimiting\Limit;
use Illuminate\Support\Facades\RateLimiter;
use Illuminate\Support\Facades\URL;
use Illuminate\Support\ServiceProvider;

class AppServiceProvider extends ServiceProvider
{
    public function register(): void
    {
        $this->app->singleton(\App\Services\SettingsService::class);
    }

    public function boot(): void
    {
        if (config('app.env') === 'production') {
            URL::forceScheme('https');
        }

        // General API rate limiting per the brief's "API rate limiting ...
        // for security" requirement. Cashier Mode legitimately fires more
        // requests per minute than a browsing admin, so authenticated staff
        // get a higher ceiling than anonymous callers.
        RateLimiter::for('api', function ($request) {
            return $request->user('staff') || $request->user('member')
                ? Limit::perMinute(300)->by($request->user('staff')?->id ?? $request->user('member')?->id ?? $request->ip())
                : Limit::perMinute(30)->by($request->ip());
        });
    }
}
