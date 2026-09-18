<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Member;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Hash;
use Illuminate\Validation\ValidationException;

class MemberAuthController extends Controller
{
    /** Members log in with either their email or their membership ID. */
    public function login(Request $request)
    {
        $data = $request->validate([
            'identifier' => ['required', 'string'],
            'password' => ['required', 'string'],
        ]);

        $member = Member::where('email', $data['identifier'])
            ->orWhere('membership_id', $data['identifier'])
            ->first();

        if (! $member || ! Hash::check($data['password'], $member->password)) {
            throw ValidationException::withMessages([
                'identifier' => ['These credentials do not match our records.'],
            ]);
        }

        if (! $member->is_active) {
            throw ValidationException::withMessages([
                'identifier' => ['This membership is inactive. Contact the cooperative office.'],
            ]);
        }

        $token = $member->createToken($request->userAgent() ?? 'member-session')->plainTextToken;

        return response()->json([
            'token' => $token,
            'member' => $member,
        ]);
    }

    public function logout(Request $request)
    {
        $request->user('member')->currentAccessToken()->delete();

        return response()->json(['message' => 'Logged out.']);
    }

    public function me(Request $request)
    {
        return response()->json($request->user('member'));
    }
}
