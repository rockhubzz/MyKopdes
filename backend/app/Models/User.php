<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Foundation\Auth\User as Authenticatable;
use Illuminate\Notifications\Notifiable;
use Laravel\Sanctum\HasApiTokens;

class User extends Authenticatable
{
    use HasApiTokens, HasFactory, Notifiable;

    // Role tiers for staff. "Member" is intentionally NOT here — members are
    // a separate table/guard, not a role on this model, so a compromised or
    // misassigned staff row can never inherit member-portal access and vice
    // versa.
    public const ROLE_ADMIN = 'admin';
    public const ROLE_SHOP_OWNER = 'shop_owner';
    public const ROLE_EMPLOYEE = 'employee';

    public const ROLES = [self::ROLE_ADMIN, self::ROLE_SHOP_OWNER, self::ROLE_EMPLOYEE];

    protected $fillable = [
        'name', 'email', 'password', 'role', 'phone', 'shift_label', 'is_active',
    ];

    protected $hidden = ['password', 'remember_token'];

    protected function casts(): array
    {
        return [
            'email_verified_at' => 'datetime',
            'password' => 'hashed',
            'is_active' => 'boolean',
        ];
    }

    public function isAdmin(): bool
    {
        return $this->role === self::ROLE_ADMIN;
    }

    public function isShopOwner(): bool
    {
        return $this->role === self::ROLE_SHOP_OWNER;
    }

    public function isEmployee(): bool
    {
        return $this->role === self::ROLE_EMPLOYEE;
    }

    /** Admin implicitly has every Shop Owner + Employee permission. */
    public function hasAtLeast(string $role): bool
    {
        $rank = array_flip(self::ROLES); // admin=0, shop_owner=1, employee=2
        return $rank[$this->role] <= $rank[$role];
    }

    public function restockingRecords()
    {
        return $this->hasMany(RestockingRecord::class, 'submitted_by');
    }

    public function transactions()
    {
        return $this->hasMany(Transaction::class, 'cashier_id');
    }

    public function auditLogs()
    {
        return $this->morphMany(AuditLog::class, 'actor');
    }
}
