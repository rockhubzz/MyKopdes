<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Item;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Storage;
use Illuminate\Validation\Rule;

class ItemController extends Controller
{
    public function index(Request $request)
    {
        $query = Item::with('category');

        if ($request->filled('search')) {
            $search = $request->string('search');
            $query->where(fn ($q) => $q->where('name', 'like', "%$search%")
                ->orWhere('sku', 'like', "%$search%")
                ->orWhere('barcode', 'like', "%$search%"));
        }
        if ($request->filled('category_id')) {
            $query->where('category_id', $request->integer('category_id'));
        }
        if ($request->boolean('low_stock')) {
            $query->lowStock();
        }
        if (! $request->boolean('include_inactive')) {
            $query->where('is_active', true);
        }

        return $query->orderBy('name')->paginate(min($request->integer('per_page', 20), 100));
    }

    /** Exact barcode/SKU match used by the Cashier Mode scanner input. */
    public function scan(Request $request)
    {
        $data = $request->validate(['code' => ['required', 'string']]);

        // Both columns are unique-indexed; filter is_active in SQL so misses
        // short-circuit without hydrating a row we then reject in PHP.
        $item = Item::where('is_active', true)
            ->where(fn ($q) => $q->where('barcode', $data['code'])->orWhere('sku', $data['code']))
            ->first();

        if (! $item) {
            return response()->json(['message' => 'Item not found.'], 404);
        }

        return response()->json($item);
    }

    public function store(Request $request)
    {
        $data = $this->validated($request);
        $data['image_path'] = $this->storeImage($request);

        return response()->json(Item::create($data)->load('category'), 201);
    }

    public function show(Item $item)
    {
        return response()->json($item->load('category'));
    }

    public function update(Request $request, Item $item)
    {
        $data = $this->validated($request, $item->id);

        if ($request->hasFile('image')) {
            if ($item->image_path) {
                Storage::disk('public')->delete($item->image_path);
            }
            $data['image_path'] = $this->storeImage($request);
        }

        $item->update($data);

        return response()->json($item->load('category'));
    }

    public function destroy(Request $request, Item $item)
    {
        // Permanent deletion is only safe when nothing references the row;
        // otherwise it would wipe sales/restock history (restocking_records
        // even cascade). Callers that just want it off the shelf use the
        // plain DELETE, which deactivates.
        if ($request->boolean('force')) {
            if ($item->transactionItems()->exists() || $item->restockingRecords()->exists()) {
                abort(422, 'Cannot permanently delete an item with sales or restock history. Deactivate it instead.');
            }
            $item->delete();

            return response()->json(['message' => 'Item permanently deleted.']);
        }

        // Preserve history: transaction_items / restocking_records reference
        // this row, so deactivate instead of hard-deleting.
        $item->update(['is_active' => false]);

        return response()->json(['message' => 'Item deactivated.']);
    }

    /**
     * Reactivate a deactivated item. Separate from update() because the
     * update validator requires the full sellable payload (name, sku,
     * prices…), while reactivation is just the status flip the Items table's
     * Activate button needs.
     */
    public function activate(Item $item)
    {
        $item->update(['is_active' => true]);

        return response()->json($item->load('category'));
    }

    private function validated(Request $request, ?int $ignoreId = null): array
    {
        return $request->validate([
            'name' => ['required', 'string', 'max:255'],
            'sku' => ['required', 'string', 'max:100', Rule::unique('items', 'sku')->ignore($ignoreId)],
            'barcode' => ['nullable', 'string', 'max:100', Rule::unique('items', 'barcode')->ignore($ignoreId)],
            'category_id' => ['nullable', 'exists:item_categories,id'],
            'unit_price' => ['required', 'numeric', 'min:0'],
            'cost_price' => ['required', 'numeric', 'min:0'],
            'unit_of_measure' => ['required', 'string', 'max:20'],
            'min_stock_threshold' => ['nullable', 'integer', 'min:0'],
            'expiry_date' => ['nullable', 'date'],
            'is_active' => ['sometimes', 'boolean'],
        ]);
    }

    private function storeImage(Request $request): ?string
    {
        if (! $request->hasFile('image')) {
            return null;
        }

        return $request->file('image')->store('items', 'public');
    }
}
