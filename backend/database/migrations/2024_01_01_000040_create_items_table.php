<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('items', function (Blueprint $table) {
            $table->id();
            $table->string('name');
            $table->string('sku')->unique();
            $table->string('barcode')->unique()->nullable(); // scanned in Cashier Mode
            $table->foreignId('category_id')->nullable()->constrained('item_categories')->nullOnDelete();
            $table->decimal('unit_price', 12, 2);
            $table->decimal('cost_price', 12, 2);
            $table->string('unit_of_measure')->default('pcs'); // pcs, kg, l, ...
            $table->string('image_path')->nullable();
            $table->integer('current_stock')->default(0);
            $table->integer('min_stock_threshold')->default(10);
            $table->date('expiry_date')->nullable();
            $table->boolean('is_active')->default(true);
            $table->timestamps();

            $table->index(['current_stock', 'min_stock_threshold']);
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('items');
    }
};
