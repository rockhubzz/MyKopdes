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

        return $query->orderBy('name')->paginate($request->integer('per_page', 20));
    }

    /** Exact barcode/SKU match used by the Cashier Mode scanner input. */
    public function scan(Request $request)
    {
        $data = $request->validate(['code' => ['required', 'string']]);

        $item = Item::where('barcode', $data['code'])->orWhere('sku', $data['code'])->first();

        if (! $item || ! $item->is_active) {
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

    public function destroy(Item $item)
    {
        // Preserve history: transaction_items / restocking_records reference
        // this row, so deactivate instead of hard-deleting.
        $item->update(['is_active' => false]);

        return response()->json(['message' => 'Item deactivated.']);
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
