<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

/**
 * Read-performance indexes for the screens the frontend hits on every
 * navigation (dashboards, DataTables, cashier lookups, detail modals).
 *
 * Rationale per index is inline. All are additive composite/single-column
 * indexes — no column changes, no backfill, safe to run online on the
 * small tables this app has.
 */
return new class extends Migration
{
    public function up(): void
    {
        Schema::table('transactions', function (Blueprint $table) {
            // Employee dashboard + "my history": WHERE cashier_id = ? AND created_at >= ? ORDER BY created_at.
            $table->index(['cashier_id', 'created_at'], 'transactions_cashier_created_idx');
            // Member portal history/summary: WHERE member_id = ? AND created_at ... ORDER BY created_at.
            $table->index(['member_id', 'created_at'], 'transactions_member_created_idx');
            // ReportService ranges: WHERE created_at BETWEEN ? AND ? AND payment_status = ?.
            $table->index(['created_at', 'payment_status'], 'transactions_created_status_idx');
        });

        Schema::table('restocking_records', function (Blueprint $table) {
            // Employee "mine only" listing + dashboard count: WHERE submitted_by = ? AND restocked_at >= ?.
            $table->index(['submitted_by', 'restocked_at'], 'restocks_submitter_restocked_idx');
            // Item detail modal "recent restocks": WHERE item_id = ? ORDER BY restocked_at DESC.
            $table->index(['item_id', 'restocked_at'], 'restocks_item_restocked_idx');
            // Owner restock listing: ORDER BY restocked_at DESC.
            $table->index(['restocked_at'], 'restocks_restocked_idx');
        });

        Schema::table('transaction_items', function (Blueprint $table) {
            // Best-sellers join direction: transaction_items.item_id -> items, grouped per item.
            $table->index(['item_id', 'transaction_id'], 'trx_items_item_trx_idx');
        });

        Schema::table('items', function (Blueprint $table) {
            // Catalog listing: WHERE is_active = ? ORDER BY name.
            $table->index(['is_active', 'name'], 'items_active_name_idx');
            // Category drill-down: WHERE category_id = ? AND is_active = ?.
            $table->index(['category_id', 'is_active'], 'items_category_active_idx');
        });

        Schema::table('audit_logs', function (Blueprint $table) {
            // Audit-log listing: ORDER BY created_at DESC + optional actor filter.
            $table->index(['created_at'], 'audit_logs_created_idx');
            $table->index(['actor_id', 'created_at'], 'audit_logs_actor_created_idx');
            // Morph actor eager loads: WHERE actor_type = ? AND actor_id = ?.
            $table->index(['actor_type', 'actor_id'], 'audit_logs_actor_type_id_idx');
        });

        Schema::table('members', function (Blueprint $table) {
            // Member listing + cashier lookup: WHERE is_active = ? ORDER BY / LIKE name.
            $table->index(['is_active', 'name'], 'members_active_name_idx');
        });

        Schema::table('users', function (Blueprint $table) {
            // Staff listing: ORDER BY name with optional is_active/role filter.
            $table->index(['is_active', 'name'], 'users_active_name_idx');
        });
    }

    public function down(): void
    {
        Schema::table('transactions', function (Blueprint $table) {
            $table->dropIndex('transactions_cashier_created_idx');
            $table->dropIndex('transactions_member_created_idx');
            $table->dropIndex('transactions_created_status_idx');
        });
        Schema::table('restocking_records', function (Blueprint $table) {
            $table->dropIndex('restocks_submitter_restocked_idx');
            $table->dropIndex('restocks_item_restocked_idx');
            $table->dropIndex('restocks_restocked_idx');
        });
        Schema::table('transaction_items', function (Blueprint $table) {
            $table->dropIndex('trx_items_item_trx_idx');
        });
        Schema::table('items', function (Blueprint $table) {
            $table->dropIndex('items_active_name_idx');
            $table->dropIndex('items_category_active_idx');
        });
        Schema::table('audit_logs', function (Blueprint $table) {
            $table->dropIndex('audit_logs_created_idx');
            $table->dropIndex('audit_logs_actor_created_idx');
            $table->dropIndex('audit_logs_actor_type_id_idx');
        });
        Schema::table('members', function (Blueprint $table) {
            $table->dropIndex('members_active_name_idx');
        });
        Schema::table('users', function (Blueprint $table) {
            $table->dropIndex('users_active_name_idx');
        });
    }
};
