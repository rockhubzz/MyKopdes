<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class Discount extends Model
{
    use HasFactory;

    public const TYPE_PERCENTAGE = 'percentage';
    public const TYPE_FLAT = 'flat';
    public const TYPE_BOGO = 'buy_x_get_y';

    public const SCOPE_GENERAL = 'general';
    public const SCOPE_MEMBER = 'member';

    protected $fillable = [
        'name', 'description', 'type', 'value', 'buy_qty', 'get_qty',
        'scope', 'category_id', 'min_purchase', 'is_active', 'starts_at', 'ends_at',
    ];

    protected function casts(): array
    {
        return [
            'value' => 'decimal:2',
            'min_purchase' => 'decimal:2',
            'buy_qty' => 'integer',
            'get_qty' => 'integer',
            'is_active' => 'boolean',
            'starts_at' => 'datetime',
            'ends_at' => 'datetime',
        ];
    }

    public function category()
    {
        return $this->belongsTo(ItemCategory::class, 'category_id');
    }

    public function scopeActive($query)
    {
        return $query->where('is_active', true)
            ->where(fn ($q) => $q->whereNull('starts_at')->orWhere('starts_at', '<=', now()))
            ->where(fn ($q) => $q->whereNull('ends_at')->orWhere('ends_at', '>=', now()));
    }

    public function scopeVisibleTo($query, bool $isMember)
    {
        return $isMember ? $query : $query->where('scope', self::SCOPE_GENERAL);
    }

    public function isCurrentlyValid(): bool
    {
        $now = now();
        return $this->is_active
            && (! $this->starts_at || $this->starts_at->lte($now))
            && (! $this->ends_at || $this->ends_at->gte($now));
    }
}
