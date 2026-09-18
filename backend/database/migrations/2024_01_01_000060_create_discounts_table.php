<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('discounts', function (Blueprint $table) {
            $table->id();
            $table->string('name');
            $table->text('description')->nullable();
            $table->enum('type', ['percentage', 'flat', 'buy_x_get_y']);
            $table->decimal('value', 12, 2)->default(0); // % or flat amount; ignored for bogo
            $table->integer('buy_qty')->nullable(); // buy-X-get-Y
            $table->integer('get_qty')->nullable();
            $table->enum('scope', ['general', 'member'])->default('general');
            $table->foreignId('category_id')->nullable()->constrained('item_categories')->nullOnDelete();
            $table->decimal('min_purchase', 12, 2)->nullable();
            $table->boolean('is_active')->default(true);
            $table->timestamp('starts_at')->nullable();
            $table->timestamp('ends_at')->nullable();
            $table->timestamps();

            $table->index(['is_active', 'scope', 'starts_at', 'ends_at']);
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('discounts');
    }
};
