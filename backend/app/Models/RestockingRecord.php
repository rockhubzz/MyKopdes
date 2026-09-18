<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class RestockingRecord extends Model
{
    use HasFactory;

    // Restocking is additive-only from the API's point of view: nothing in
    // this app ever lets an Employee submit a negative quantity to reduce
    // stock (see Http/Controllers/Api/RestockController::store — quantity is
    // validated as integer|min:1). Stock only ever goes down via a completed
    // Transaction (see TransactionController).
    protected $fillable = [
        'item_id', 'supplier_id', 'quantity', 'cost_per_unit', 'total_cost',
        'submitted_by', 'notes', 'restocked_at',
    ];

    protected function casts(): array
    {
        return [
            'cost_per_unit' => 'decimal:2',
            'total_cost' => 'decimal:2',
            'restocked_at' => 'datetime',
        ];
    }

    public function item()
    {
        return $this->belongsTo(Item::class);
    }

    public function supplier()
    {
        return $this->belongsTo(Supplier::class);
    }

    public function submittedBy()
    {
        return $this->belongsTo(User::class, 'submitted_by');
    }
}
