<?php

use Laravel\Sanctum\Sanctum;

return [
    'stateful' => explode(',', env('SANCTUM_STATEFUL_DOMAINS', sprintf(
        '%s%s',
        'localhost,localhost:3000,127.0.0.1,127.0.0.1:8000,::1',
        env('APP_URL') ? ','.parse_url(env('APP_URL'), PHP_URL_HOST) : ''
    ))),

    // Must reference guards that do NOT use the `sanctum` driver.
    // Listing `staff`/`member` here makes Sanctum's guard callback call
    // $request->user('staff'|'member'), which re-enters the same callback:
    // infinite recursion ("Maximum call stack size ... reached").
    // Bearer-token auth is unaffected: with no session user, Sanctum falls
    // through to the personal-access-token lookup.
    'guard' => ['web'],

    'expiration' => env('SANCTUM_TOKEN_EXPIRATION_MINUTES', 720),

    'token_prefix' => env('SANCTUM_TOKEN_PREFIX', ''),

    'middleware' => [
        'authenticate_session' => Laravel\Sanctum\Http\Middleware\AuthenticateSession::class,
        'encrypt_cookies' => Illuminate\Cookie\Middleware\EncryptCookies::class,
        'validate_csrf_token' => Illuminate\Foundation\Http\Middleware\ValidateCsrfToken::class,
    ],
];
