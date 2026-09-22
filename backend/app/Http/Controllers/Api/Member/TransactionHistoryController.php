<?php

namespace App\Http\Controllers\Api\Member;

use App\Http\Controllers\Controller;
use Illuminate\Http\Request;

class TransactionHistoryController extends Controller
{
    public function index(Request $request)
    {
        // The history table prints item names per row, so lines are needed —
        // but only the FKs + the nested name, not full item rows.
        return $request->user('member')
            ->transactions()
            ->with(['items:id,transaction_id,item_id,quantity', 'items.item:id,name'])
            ->latest()
            ->paginate(min($request->integer('per_page', 20), 100));
    }

    public function show(Request $request, int $id)
    {
        $transaction = $request->user('member')->transactions()->with('items.item', 'discount')->findOrFail($id);

        return response()->json($transaction);
    }
}
