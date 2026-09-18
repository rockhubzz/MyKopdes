<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Item;
use App\Models\RestockingRecord;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;

class RestockController extends Controller
{
    public function index(Request $request)
    {
        $query = RestockingRecord::with(['item', 'supplier', 'submittedBy']);

        if ($request->filled('item_id')) {
            $query->where('item_id', $request->integer('item_id'));
        }
        // Employees only ever see their own submissions in "my history";
        // the broader listing (all restocks) is gated to shop_owner+ at the
        // route level (see routes/api.php).
        if ($request->boolean('mine_only')) {
            $query->where('submitted_by', $request->user('staff')->id);
        }

        return $query->latest('restocked_at')->paginate($request->integer('per_page', 20));
    }

    /**
     * Employee (or Shop Owner) submits a restock. Inventory updates
     * immediately and atomically; there is deliberately no endpoint that
     * lets an Employee submit a negative quantity — stock only ever
     * decreases through TransactionController::checkout().
     */
    public function store(Request $request)
    {
        $data = $request->validate([
            'item_id' => ['required', 'exists:items,id'],
            'supplier_id' => ['nullable', 'exists:suppliers,id'],
            'quantity' => ['required', 'integer', 'min:1'],
            'cost_per_unit' => ['required', 'numeric', 'min:0'],
            'notes' => ['nullable', 'string'],
            'restocked_at' => ['nullable', 'date'],
        ]);

        $record = DB::transaction(function () use ($data, $request) {
            $item = Item::lockForUpdate()->findOrFail($data['item_id']);

            $record = RestockingRecord::create([
                'item_id' => $item->id,
                'supplier_id' => $data['supplier_id'] ?? null,
                'quantity' => $data['quantity'],
                'cost_per_unit' => $data['cost_per_unit'],
                'total_cost' => $data['quantity'] * $data['cost_per_unit'],
                'submitted_by' => $request->user('staff')->id,
                'notes' => $data['notes'] ?? null,
                'restocked_at' => $data['restocked_at'] ?? now(),
            ]);

            $item->increment('current_stock', $data['quantity']);
            // Restocking is also a natural moment to refresh the standing
            // cost price used for margin reporting.
            $item->update(['cost_price' => $data['cost_per_unit']]);

            return $record;
        });

        return response()->json($record->load(['item', 'supplier']), 201);
    }

    public function show(RestockingRecord $restockingRecord)
    {
        return response()->json($restockingRecord->load(['item', 'supplier', 'submittedBy']));
    }
}
