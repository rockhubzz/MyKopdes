<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Foundation\Auth\User as Authenticatable;
use Illuminate\Notifications\Notifiable;
use Laravel\Sanctum\HasApiTokens;

class Member extends Authenticatable
{
    use HasApiTokens, HasFactory, Notifiable;

    protected $fillable = [
        'membership_id', 'name', 'email', 'password', 'phone', 'address',
        'join_date', 'shu_balance', 'is_active',
    ];

    protected $hidden = ['password', 'remember_token'];

    protected function casts(): array
    {
        return [
            'password' => 'hashed',
            'join_date' => 'date',
            'shu_balance' => 'decimal:2',
            'is_active' => 'boolean',
        ];
    }

    public function transactions()
    {
        return $this->hasMany(Transaction::class, 'member_id');
    }

    public function auditLogs()
    {
        return $this->morphMany(AuditLog::class, 'actor');
    }

    /** Discounts currently valid for members, independent of general ones. */
    public function scopeActive($query)
    {
        return $query->where('is_active', true);
    }
}
