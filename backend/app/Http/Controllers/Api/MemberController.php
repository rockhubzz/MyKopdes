<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Member;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Str;
use Illuminate\Validation\Rule;

class MemberController extends Controller
{
    public function index(Request $request)
    {
        $query = Member::query();

        if ($request->filled('search')) {
            $search = $request->string('search');
            $query->where(fn ($q) => $q->where('name', 'like', "%$search%")
                ->orWhere('membership_id', 'like', "%$search%")
                ->orWhere('phone', 'like', "%$search%"));
        }

        return $query->orderBy('name')->paginate($request->integer('per_page', 20));
    }

    /**
     * Checkout-time lookup: by phone, membership ID, or a QR/barcode payload
     * that simply encodes the membership ID. Used by Cashier Mode.
     */
    public function lookup(Request $request)
    {
        $data = $request->validate(['query' => ['required', 'string']]);

        $member = Member::where('membership_id', $data['query'])
            ->orWhere('phone', $data['query'])
            ->first();

        if (! $member || ! $member->is_active) {
            return response()->json(['message' => 'No active member found.'], 404);
        }

        return response()->json($member);
    }

    public function store(Request $request)
    {
        $data = $request->validate([
            'name' => ['required', 'string', 'max:255'],
            'email' => ['nullable', 'email', 'unique:members,email'],
            'phone' => ['nullable', 'string', 'unique:members,phone'],
            'address' => ['nullable', 'string'],
            'password' => ['required', 'string', 'min:8'],
            'join_date' => ['nullable', 'date'],
        ]);

        $data['membership_id'] = $this->generateMembershipId();
        $data['join_date'] = $data['join_date'] ?? now()->toDateString();
        $data['password'] = Hash::make($data['password']);

        $member = Member::create($data);

        return response()->json($member, 201);
    }

    public function show(Member $member)
    {
        return response()->json($member->load(['transactions' => fn ($q) => $q->latest()->limit(20)]));
    }

    public function update(Request $request, Member $member)
    {
        $data = $request->validate([
            'name' => ['sometimes', 'string', 'max:255'],
            'email' => ['sometimes', 'nullable', 'email', Rule::unique('members', 'email')->ignore($member->id)],
            'phone' => ['sometimes', 'nullable', 'string', Rule::unique('members', 'phone')->ignore($member->id)],
            'address' => ['sometimes', 'nullable', 'string'],
            'password' => ['sometimes', 'nullable', 'string', 'min:8'],
            'is_active' => ['sometimes', 'boolean'],
        ]);

        if (! empty($data['password'])) {
            $data['password'] = Hash::make($data['password']);
        } else {
            unset($data['password']);
        }

        $member->update($data);

        return response()->json($member);
    }

    public function destroy(Member $member)
    {
        $member->update(['is_active' => false]);

        return response()->json(['message' => 'Membership deactivated.']);
    }

    private function generateMembershipId(): string
    {
        do {
            $candidate = 'KOP-'.now()->format('y').'-'.Str::upper(Str::random(5));
        } while (Member::where('membership_id', $candidate)->exists());

        return $candidate;
    }
}
