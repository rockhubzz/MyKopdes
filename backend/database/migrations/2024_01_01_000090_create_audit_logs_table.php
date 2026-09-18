<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('audit_logs', function (Blueprint $table) {
            $table->id();
            $table->nullableMorphs('actor'); // usually a User, occasionally a Member (e.g. profile edit)
            $table->string('action'); // created, updated, deleted, login, restock, checkout, settings_changed, ...
            $table->nullableMorphs('auditable'); // the model that was changed
            $table->json('changes')->nullable(); // {before: {...}, after: {...}}
            $table->string('ip_address', 45)->nullable();
            $table->timestamp('created_at')->useCurrent();

            $table->index(['action', 'created_at']);
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('audit_logs');
    }
};
