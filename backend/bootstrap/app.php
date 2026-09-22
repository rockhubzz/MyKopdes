<?php

use App\Http\Middleware\EnsureRole;
use App\Http\Middleware\LogActivity;
use App\Http\Middleware\SetLocale;
use Illuminate\Foundation\Application;
use Illuminate\Foundation\Configuration\Exceptions;
use Illuminate\Foundation\Configuration\Middleware;

return Application::configure(basePath: dirname(__DIR__))
    ->withRouting(
        api: __DIR__.'/../routes/api.php',
        commands: __DIR__.'/../routes/console.php',
        health: '/up',
    )
    ->withMiddleware(function (Middleware $middleware) {
        // Deliberately NOT using Sanctum's EnsureFrontendRequestsAreStateful
        // here: the Next.js frontend authenticates with plain Bearer tokens
        // (Sanctum personal access tokens) sent via the Authorization
        // header, not first-party session cookies, so the SPA/CSRF cookie
        // flow is unnecessary — every request is stateless, which also
        // keeps a single API usable from the web app, a future mobile app,
        // and Cashier Mode's POS hardware alike.

        // Named middleware usable in routes, e.g. ->middleware('role:admin')
        $middleware->alias([
            'role' => EnsureRole::class,
            'log.activity' => LogActivity::class,
        ]);

        // Every /api/* request is throttled via the "api" rate limiter
        // registered in AppServiceProvider::boot(). Login endpoints layer an
        // additional, stricter throttle directly on the route (see
        // routes/api.php) as brute-force protection.
        $middleware->throttleApi();

        // Negotiate en/id for localized API error messages (lang/*/api.php)
        // from Accept-Language or the koperasi_lang cookie.
        $middleware->api(append: [SetLocale::class]);
    })
    ->withExceptions(function (Exceptions $exceptions) {
        $exceptions->shouldRenderJsonWhen(function ($request, Throwable $e) {
            return $request->is('api/*') || $request->expectsJson();
        });
    })->create();
