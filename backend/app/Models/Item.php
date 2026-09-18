<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class Item extends Model
{
    use HasFactory;

    protected $fillable = [
        'name', 'sku', 'barcode', 'category_id', 'unit_price', 'cost_price',
        'unit_of_measure', 'image_path', 'current_stock', 'min_stock_threshold',
        'expiry_date', 'is_active',
    ];

    protected function casts(): array
    {
        return [
            'unit_price' => 'decimal:2',
            'cost_price' => 'decimal:2',
            'current_stock' => 'integer',
            'min_stock_threshold' => 'integer',
            'expiry_date' => 'date',
            'is_active' => 'boolean',
        ];
    }

    public function category()
    {
        return $this->belongsTo(ItemCategory::class, 'category_id');
    }

    public function restockingRecords()
    {
        return $this->hasMany(RestockingRecord::class);
    }

    public function transactionItems()
    {
        return $this->hasMany(TransactionItem::class);
    }

    public function isLowStock(): bool
    {
        return $this->current_stock <= $this->min_stock_threshold;
    }

    public function isExpiringSoon(int $days = 7): bool
    {
        return $this->expiry_date && now()->diffInDays($this->expiry_date, false) <= $days
            && now()->diffInDays($this->expiry_date, false) >= 0;
    }

    public function scopeLowStock($query)
    {
        return $query->whereColumn('current_stock', '<=', 'min_stock_threshold');
    }
}
