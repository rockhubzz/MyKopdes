<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Member;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Hash;
use Illuminate\Validation\ValidationException;

class MemberAuthController extends Controller
{
    /** Members log in with their email, membership ID, or phone number. */
    public function login(Request $request)
    {
        $data = $request->validate([
            'identifier' => ['required', 'string'],
            'password' => ['required', 'string'],
        ]);

        $identifier = trim($data['identifier']);

        $member = Member::where('email', $identifier)
            ->orWhere('membership_id', $identifier)
            ->orWhere('phone', $identifier)
            ->first();

        if (! $member || ! Hash::check($data['password'], $member->password)) {
            throw ValidationException::withMessages([
                'identifier' => [__('api.auth.credentials')],
            ]);
        }

        if (! $member->is_active) {
            throw ValidationException::withMessages([
                'identifier' => [__('api.auth.member_inactive')],
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
