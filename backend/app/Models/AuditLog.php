<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Model;

class AuditLog extends Model
{
    // Deliberately append-only from the application layer: no
    // UpdateAuditLog / DeleteAuditLog route exists anywhere in
    // routes/api.php, and there is no `updated_at` maintained beyond
    // creation, so a full history survives even a compromised admin account.
    public $timestamps = false;

    protected $fillable = [
        'actor_type', 'actor_id', 'action', 'auditable_type', 'auditable_id',
        'changes', 'ip_address', 'created_at',
    ];

    protected function casts(): array
    {
        return [
            'changes' => 'array',
            'created_at' => 'datetime',
        ];
    }

    public function actor()
    {
        return $this->morphTo();
    }

    public function auditable()
    {
        return $this->morphTo();
    }
}
