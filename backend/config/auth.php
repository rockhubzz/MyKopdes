<?php

return [
    // Two independent auth stacks: staff (admin/shop_owner/employee live in
    // `users`) and cooperative members (`members`), each with their own
    // Sanctum-issued tokens and their own guard so a member token can never
    // satisfy a staff-only route and vice versa.
    'defaults' => [
        'guard' => 'staff',
        'passwords' => 'users',
    ],

    'guards' => [
        // Session guard exists only so `sanctum.guard` (see config/sanctum.php)
        // has a non-`sanctum`-driver guard to check for SPA session auth.
        // All real API auth is stateless Bearer tokens via staff/member below.
        'web' => [
            'driver' => 'session',
            'provider' => 'users',
        ],
        'staff' => [
            'driver' => 'sanctum',
            'provider' => 'users',
        ],
        'member' => [
            'driver' => 'sanctum',
            'provider' => 'members',
        ],
    ],

    'providers' => [
        'users' => [
            'driver' => 'eloquent',
            'model' => App\Models\User::class,
        ],
        'members' => [
            'driver' => 'eloquent',
            'model' => App\Models\Member::class,
        ],
    ],

    'passwords' => [
        'users' => [
            'provider' => 'users',
            'table' => 'password_reset_tokens',
            'expire' => 60,
            'throttle' => 60,
        ],
        'members' => [
            'provider' => 'members',
            'table' => 'password_reset_tokens',
            'expire' => 60,
            'throttle' => 60,
        ],
    ],

    'password_timeout' => 10800,
];
