<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class Transaction extends Model
{
    use HasFactory;

    public const PAYMENT_CASH = 'cash';
    public const PAYMENT_QRIS = 'qris';
    public const PAYMENT_BANK_TRANSFER = 'bank_transfer';

    public const STATUS_PAID = 'paid';
    public const STATUS_PENDING = 'pending';
    public const STATUS_VOID = 'void';

    protected $fillable = [
        'transaction_code', 'member_id', 'cashier_id', 'subtotal', 'discount_id',
        'discount_amount', 'tax_amount', 'total', 'payment_method',
        'payment_status', 'paid_at',
    ];

    protected function casts(): array
    {
        return [
            'subtotal' => 'decimal:2',
            'discount_amount' => 'decimal:2',
            'tax_amount' => 'decimal:2',
            'total' => 'decimal:2',
            'paid_at' => 'datetime',
        ];
    }

    public function items()
    {
        return $this->hasMany(TransactionItem::class);
    }

    public function member()
    {
        return $this->belongsTo(Member::class);
    }

    public function cashier()
    {
        return $this->belongsTo(User::class, 'cashier_id');
    }

    public function discount()
    {
        return $this->belongsTo(Discount::class);
    }
}
