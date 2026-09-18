<?php

namespace App\Services;

use App\Models\Setting;
use Illuminate\Support\Facades\Cache;

/**
 * Backs the "infrastructure settings changeable via admin UI without
 * redeploying containers" requirement: DB host/IP, backend base URL, tax
 * rate, SHU rate, low-stock threshold, backup cron, etc. all live as rows in
 * the `settings` table (not hardcoded / not build-time constants) and are
 * read through this service everywhere in the app that needs them.
 *
 * Note on DB host/port specifically: changing the *bootstrap* DB connection
 * (config/database.php, read from backend/.env) still requires a container
 * restart, because you cannot reconnect a live PDO connection to a
 * different host mid-request. What IS live-editable without a restart is
 * everything the running app reads per-request: API base URL used when
 * building absolute links (e.g. receipts, storage URLs), CORS origins, tax
 * rate, SHU rate, discount rules, alert thresholds, and the backup cron
 * (routes/console.php re-reads it every scheduler tick). The admin UI
 * surfaces the DB fields too so ops can record/document them centrally, and
 * PUT /api/admin/settings writes them here immediately either way.
 */
class SettingsService
{
    private const CACHE_KEY = 'koperasi:settings';
    private const CACHE_TTL = 300; // seconds

    public function all(): array
    {
        return Cache::remember(self::CACHE_KEY, self::CACHE_TTL, function () {
            return Setting::all()->pluck('value', 'key')->toArray();
        });
    }

    public function get(string $key, mixed $default = null): mixed
    {
        return $this->all()[$key] ?? config("koperasi.defaults.$key", $default);
    }

    public function set(string $key, mixed $value, string $group = 'general', ?bool $isPublic = null): Setting
    {
        $existing = Setting::where('key', $key)->first();

        $setting = Setting::updateOrCreate(
            ['key' => $key],
            [
                'value' => is_scalar($value) ? (string) $value : json_encode($value),
                'group' => $group,
                // Preserve whatever is_public was already set to (e.g. by the
                // seeder) unless the caller explicitly overrides it — a
                // routine value edit via the admin settings panel must not
                // silently flip a public field (like store_name) private.
                'is_public' => $isPublic ?? $existing?->is_public ?? false,
            ]
        );

        Cache::forget(self::CACHE_KEY);

        return $setting;
    }

    public function setMany(array $pairs, string $group = 'general'): void
    {
        foreach ($pairs as $key => $value) {
            $this->set($key, $value, $group);
        }
    }

    public function group(string $group): array
    {
        return Setting::where('group', $group)->pluck('value', 'key')->toArray();
    }

    public function publicSettings(): array
    {
        return Setting::where('is_public', true)->pluck('value', 'key')->toArray();
    }
}
