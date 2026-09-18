<?php

namespace App\Http\Controllers\Api\Member;

use App\Http\Controllers\Controller;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Hash;
use Illuminate\Validation\Rule;

class ProfileController extends Controller
{
    public function show(Request $request)
    {
        return response()->json($request->user('member'));
    }

    /** Members may update their own contact info only — never role, membership_id, or SHU balance. */
    public function update(Request $request)
    {
        $member = $request->user('member');

        $data = $request->validate([
            'phone' => ['sometimes', 'nullable', 'string', Rule::unique('members', 'phone')->ignore($member->id)],
            'email' => ['sometimes', 'nullable', 'email', Rule::unique('members', 'email')->ignore($member->id)],
            'address' => ['sometimes', 'nullable', 'string'],
            'password' => ['sometimes', 'nullable', 'string', 'min:8'],
        ]);

        if (! empty($data['password'])) {
            $data['password'] = Hash::make($data['password']);
        } else {
            unset($data['password']);
        }

        $member->update($data);

        return response()->json($member);
    }
}
