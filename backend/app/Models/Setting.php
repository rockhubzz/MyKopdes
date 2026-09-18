<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class Setting extends Model
{
    // Backs the runtime-editable configuration described in the brief:
    // DB host/IP, backend base URL, tax rate, SHU rate, discount rules, low
    // stock threshold, backup schedule. Read/write only through
    // App\Services\SettingsService so every change is cached correctly and
    // (for the 'infrastructure' and 'security' groups) audit-logged.
    protected $fillable = ['key', 'value', 'group', 'is_public'];

    protected function casts(): array
    {
        return ['is_public' => 'boolean'];
    }
}
