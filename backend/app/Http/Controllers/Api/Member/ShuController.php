<?php

namespace App\Http\Controllers\Api\Member;

use App\Http\Controllers\Controller;
use Illuminate\Http\Request;

class ShuController extends Controller
{
    /** Sisa Hasil Usaha (profit-share) accrual for the logged-in member. */
    public function show(Request $request)
    {
        $member = $request->user('member');

        return response()->json([
            'shu_balance' => $member->shu_balance,
            'accrual_note' => 'SHU accrues automatically as a percentage of each purchase you make; the rate is set by the cooperative admin.',
        ]);
    }
}
