<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\ItemCategory;
use Illuminate\Http\Request;
use Illuminate\Validation\Rule;

class ItemCategoryController extends Controller
{
    public function index(Request $request)
    {
        $query = ItemCategory::withCount('items');

        if ($request->filled('search')) {
            $query->where('name', 'like', '%'.$request->string('search').'%');
        }

        return $query->orderBy('name')->paginate($request->integer('per_page', 50));
    }

    public function store(Request $request)
    {
        $data = $request->validate([
            'name' => ['required', 'string', 'max:255', 'unique:item_categories,name'],
            'description' => ['nullable', 'string'],
        ]);

        return response()->json(ItemCategory::create($data), 201);
    }

    public function show(ItemCategory $itemCategory)
    {
        return response()->json($itemCategory->loadCount('items'));
    }

    public function update(Request $request, ItemCategory $itemCategory)
    {
        $data = $request->validate([
            'name' => ['sometimes', 'string', 'max:255', Rule::unique('item_categories', 'name')->ignore($itemCategory->id)],
            'description' => ['sometimes', 'nullable', 'string'],
        ]);

        $itemCategory->update($data);

        return response()->json($itemCategory);
    }

    public function destroy(ItemCategory $itemCategory)
    {
        if ($itemCategory->items()->exists()) {
            abort(422, 'Cannot delete a category that still has items. Reassign or remove those items first.');
        }

        $itemCategory->delete();

        return response()->json(['message' => 'Category deleted.']);
    }
}
