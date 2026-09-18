<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('restocking_records', function (Blueprint $table) {
            $table->id();
            $table->foreignId('item_id')->constrained('items')->cascadeOnDelete();
            $table->foreignId('supplier_id')->nullable()->constrained('suppliers')->nullOnDelete();
            $table->integer('quantity'); // always positive; enforced in RestockController
            $table->decimal('cost_per_unit', 12, 2);
            $table->decimal('total_cost', 12, 2);
            // Every submission is attributable to the employee who made it,
            // per the brief's accountability requirement.
            $table->foreignId('submitted_by')->constrained('users')->restrictOnDelete();
            $table->text('notes')->nullable();
            $table->timestamp('restocked_at');
            $table->timestamps();
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('restocking_records');
    }
};
