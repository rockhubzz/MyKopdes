<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\AuditLog;
use App\Services\SettingsService;
use Illuminate\Http\Request;

class SettingsController extends Controller
{
    public function __construct(private readonly SettingsService $settings)
    {
    }

    public function index()
    {
        return response()->json([
            'infrastructure' => $this->settings->group('infrastructure'),
            'business_rules' => $this->settings->group('business_rules'),
            'backup' => $this->settings->group('backup'),
        ]);
    }

    /** Publicly readable subset (e.g. store name/address for receipts) — no auth required. */
    public function publicSettings()
    {
        return response()->json($this->settings->publicSettings());
    }

    /**
     * PUT /api/admin/settings
     * { "group": "infrastructure", "values": {"db_host": "...", "api_base_url": "..."} }
     *
     * Every change here is audit-logged individually (in addition to the
     * blanket LogActivity middleware) because infrastructure/business-rule
     * changes are exactly the kind of thing an Admin's audit trail exists
     * to catch.
     */
    public function update(Request $request)
    {
        $data = $request->validate([
            'group' => ['required', 'in:infrastructure,business_rules,backup,security'],
            'values' => ['required', 'array'],
        ]);

        $before = $this->settings->group($data['group']);
        $this->settings->setMany($data['values'], $data['group']);
        $after = $this->settings->group($data['group']);

        AuditLog::create([
            'actor_type' => \App\Models\User::class,
            'actor_id' => $request->user('staff')->id,
            'action' => 'settings_changed',
            'auditable_type' => null,
            'auditable_id' => null,
            'changes' => ['group' => $data['group'], 'before' => $before, 'after' => $after],
            'ip_address' => $request->ip(),
            'created_at' => now(),
        ]);

        return response()->json($after);
    }
}
