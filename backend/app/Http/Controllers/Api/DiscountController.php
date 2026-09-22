<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Discount;
use App\Models\Transaction;
use Illuminate\Http\Request;
use Illuminate\Validation\Rule;

class DiscountController extends Controller
{
    public function index(Request $request)
    {
        $query = Discount::with('category');

        if ($request->filled('scope')) {
            $query->where('scope', $request->string('scope'));
        }
        if ($request->boolean('active_only')) {
            $query->active();
        }

        return $query->latest()->paginate(min($request->integer('per_page', 20), 100));
    }

    /**
     * Active discounts visible to whoever is calling: Cashier Mode passes
     * ?has_member=1 once a member is attached to the cart so member-only
     * promos show up; the member portal always passes it implicitly via the
     * member guard.
     */
    public function active(Request $request)
    {
        $hasMember = $request->boolean('has_member') || $request->user('member') !== null;

        return response()->json(
            Discount::active()->visibleTo($hasMember)->with('category')->get()
        );
    }

    public function store(Request $request)
    {
        $data = $this->validated($request);

        return response()->json(Discount::create($data), 201);
    }

    public function show(Discount $discount)
    {
        return response()->json($discount->load('category'));
    }

    public function update(Request $request, Discount $discount)
    {
        $data = $this->validated($request, partial: true);
        $discount->update($data);

        return response()->json($discount);
    }

    public function destroy(Request $request, Discount $discount)
    {
        // Permanent deletion would null out discount_id on past transactions
        // and lose which promo was applied, so it is only allowed for
        // discounts never used in a sale.
        if ($request->boolean('force')) {
            if (Transaction::where('discount_id', $discount->id)->exists()) {
                abort(422, 'Cannot permanently delete a discount used in past transactions. Deactivate it instead.');
            }
            $discount->delete();

            return response()->json(['message' => 'Discount permanently deleted.']);
        }

        $discount->update(['is_active' => false]);

        return response()->json(['message' => 'Discount deactivated.']);
    }

    private function validated(Request $request, bool $partial = false): array
    {
        $req = $partial ? 'sometimes' : 'required';

        return $request->validate([
            'name' => [$req, 'string', 'max:255'],
            'description' => ['nullable', 'string'],
            'type' => [$req, Rule::in([Discount::TYPE_PERCENTAGE, Discount::TYPE_FLAT, Discount::TYPE_BOGO])],
            'value' => ['nullable', 'numeric', 'min:0'],
            'buy_qty' => ['nullable', 'integer', 'min:1'],
            'get_qty' => ['nullable', 'integer', 'min:1'],
            'scope' => [$req, Rule::in([Discount::SCOPE_GENERAL, Discount::SCOPE_MEMBER])],
            'category_id' => ['nullable', 'exists:item_categories,id'],
            'min_purchase' => ['nullable', 'numeric', 'min:0'],
            'is_active' => ['sometimes', 'boolean'],
            'starts_at' => ['nullable', 'date'],
            'ends_at' => ['nullable', 'date', 'after_or_equal:starts_at'],
        ]);
    }
}
