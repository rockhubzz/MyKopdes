<?php

namespace App\Http\Controllers\Api\Member;

use App\Http\Controllers\Controller;
use Illuminate\Http\Request;

class TransactionHistoryController extends Controller
{
    public function index(Request $request)
    {
        return $request->user('member')
            ->transactions()
            ->with('items.item')
            ->latest()
            ->paginate($request->integer('per_page', 20));
    }

    public function show(Request $request, int $id)
    {
        $transaction = $request->user('member')->transactions()->with('items.item', 'discount')->findOrFail($id);

        return response()->json($transaction);
    }
}
