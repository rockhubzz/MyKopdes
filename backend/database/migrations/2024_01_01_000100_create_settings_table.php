<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('settings', function (Blueprint $table) {
            $table->id();
            $table->string('key')->unique(); // e.g. "db_host", "api_base_url", "tax_rate", "shu_rate"
            $table->text('value')->nullable();
            $table->string('group')->default('general'); // infrastructure, business_rules, backup, security
            $table->boolean('is_public')->default(false); // exposed to unauthenticated GET /api/settings/public
            $table->timestamps();
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('settings');
    }
};
