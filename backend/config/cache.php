<?php

return [
    'default' => env('CACHE_STORE', 'file'),
    'stores' => [
        'file' => [
            'driver' => 'file',
            'path' => storage_path('framework/cache/data'),
        ],
        'database' => [
            'driver' => 'database',
            'table' => 'cache',
            'connection' => null,
        ],
        'array' => ['driver' => 'array'],
    ],
    'prefix' => env('CACHE_PREFIX', 'koperasi_cache'),
];
