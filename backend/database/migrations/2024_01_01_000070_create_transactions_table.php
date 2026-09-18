<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('transactions', function (Blueprint $table) {
            $table->id();
            $table->string('transaction_code')->unique(); // printed on the receipt
            $table->foreignId('member_id')->nullable()->constrained('members')->nullOnDelete();
            $table->foreignId('cashier_id')->constrained('users')->restrictOnDelete();
            $table->decimal('subtotal', 12, 2);
            $table->foreignId('discount_id')->nullable()->constrained('discounts')->nullOnDelete();
            $table->decimal('discount_amount', 12, 2)->default(0);
            $table->decimal('tax_amount', 12, 2)->default(0);
            $table->decimal('total', 12, 2);
            $table->enum('payment_method', ['cash', 'qris', 'bank_transfer']);
            $table->enum('payment_status', ['paid', 'pending', 'void'])->default('paid');
            $table->timestamp('paid_at')->nullable();
            $table->timestamps();

            $table->index(['created_at']);
            $table->index(['payment_method', 'payment_status']);
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('transactions');
    }
};
