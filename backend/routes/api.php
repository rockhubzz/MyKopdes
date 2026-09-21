<?php

use App\Http\Controllers\Api\AuditLogController;
use App\Http\Controllers\Api\BackupController;
use App\Http\Controllers\Api\DashboardController;
use App\Http\Controllers\Api\DiscountController;
use App\Http\Controllers\Api\ItemCategoryController;
use App\Http\Controllers\Api\ItemController;
use App\Http\Controllers\Api\Member\ProfileController as MemberProfileController;
use App\Http\Controllers\Api\Member\ShuController;
use App\Http\Controllers\Api\Member\TransactionHistoryController;
use App\Http\Controllers\Api\MemberAuthController;
use App\Http\Controllers\Api\MemberController;
use App\Http\Controllers\Api\ReportController;
use App\Http\Controllers\Api\RestockController;
use App\Http\Controllers\Api\SettingsController;
use App\Http\Controllers\Api\Staff\ProfileController as StaffProfileController;
use App\Http\Controllers\Api\StaffAuthController;
use App\Http\Controllers\Api\SupplierController;
use App\Http\Controllers\Api\TransactionController;
use App\Http\Controllers\Api\UserController;
use Illuminate\Support\Facades\Route;

/*
|--------------------------------------------------------------------------
| Public
|--------------------------------------------------------------------------
*/
Route::post('/auth/staff/login', [StaffAuthController::class, 'login'])
    ->middleware('throttle:6,1')->name('auth.login');
Route::post('/auth/member/login', [MemberAuthController::class, 'login'])
    ->middleware('throttle:6,1');
Route::get('/settings/public', [SettingsController::class, 'publicSettings']);

/*
|--------------------------------------------------------------------------
| Staff — session
|--------------------------------------------------------------------------
*/
Route::middleware('auth:staff')->group(function () {
    Route::post('/auth/staff/logout', [StaffAuthController::class, 'logout']);
    Route::get('/auth/staff/me', [StaffAuthController::class, 'me']);
    // Own profile settings (all staff tiers). POST is honored alongside PUT
    // because avatar uploads go out as multipart/form-data, same rationale
    // as the items image workaround below.
    Route::get('/auth/staff/profile', [StaffProfileController::class, 'show']);
    Route::match(['put', 'post'], '/auth/staff/profile', [StaffProfileController::class, 'update']);
    Route::get('/dashboard/alerts', [DashboardController::class, 'alerts']);
});

/*
|--------------------------------------------------------------------------
| Employee and above (admin, shop_owner, employee)
| Cashier Mode + read-only catalog access + own restock/transaction history.
|--------------------------------------------------------------------------
*/
Route::middleware(['auth:staff', 'role:employee', 'log.activity'])->group(function () {
    // Catalog — read only at this tier; writes require shop_owner (below).
    Route::get('/items', [ItemController::class, 'index']);
    Route::get('/items/scan', [ItemController::class, 'scan']);
    Route::get('/items/{item}', [ItemController::class, 'show']);
    Route::get('/item-categories', [ItemCategoryController::class, 'index']);
    Route::get('/suppliers', [SupplierController::class, 'index']);

    // Restocking: submit + view own history. Stock only ever increases here.
    Route::post('/restocking-records', [RestockController::class, 'store']);
    Route::get('/restocking-records', [RestockController::class, 'index']); // ?mine_only=1 enforced in controller for this tier via UI convention
    Route::get('/restocking-records/{restockingRecord}', [RestockController::class, 'show']);

    // Member lookup at checkout (phone / membership ID / QR payload).
    Route::get('/members/lookup', [MemberController::class, 'lookup']);

    // Cashier Mode: cart building, checkout, receipts, own transaction history.
    Route::get('/transactions', [TransactionController::class, 'index']);
    Route::get('/transactions/{transaction}', [TransactionController::class, 'show']);
    Route::get('/transactions/{transaction}/receipt', [TransactionController::class, 'receipt']);
    Route::post('/transactions/checkout', [TransactionController::class, 'checkout']);
    // Read-only price preview (same math as checkout, no writes) so the
    // cashier can show the discount breakdown before charging.
    Route::post('/transactions/preview', [TransactionController::class, 'preview']);

    Route::get('/discounts/active', [DiscountController::class, 'active']);

    Route::get('/dashboard/employee-summary', [DashboardController::class, 'employeeSummary']);
});

/*
|--------------------------------------------------------------------------
| Shop Owner and above (admin, shop_owner)
|--------------------------------------------------------------------------
*/
Route::middleware(['auth:staff', 'role:shop_owner', 'log.activity'])->group(function () {
    // Items & categories — full CRUD.
    Route::post('/items', [ItemController::class, 'store']);
    // Accepts POST too (not just PUT) because the Items edit form submits
    // multipart/form-data (for the optional image upload), and browsers
    // cannot natively send a PUT request with a file body — the frontend
    // sends a real POST with a `_method=PUT` field, which this route also
    // honors directly rather than depending on Laravel's spoofing being
    // enabled for API requests.
    Route::match(['put', 'post'], '/items/{item}', [ItemController::class, 'update']);
    Route::delete('/items/{item}', [ItemController::class, 'destroy']);
    Route::apiResource('item-categories', ItemCategoryController::class)->except(['index']);

    // Suppliers — full CRUD.
    Route::apiResource('suppliers', SupplierController::class)->except(['index']);

    // Employee accounts (Shop Owner manages staff it directly supervises;
    // full role reassignment across all tiers stays admin-only below).
    // Scoped strictly to role=employee — both so a Shop Owner can never use
    // this endpoint to view/edit/deactivate an Admin or fellow Shop Owner
    // account, and so `role` in the request body can never be used to
    // create or promote an account to anything other than "employee".
    Route::get('/employees', function (\Illuminate\Http\Request $request) {
        $request->merge(['role' => \App\Models\User::ROLE_EMPLOYEE]);
        return app(UserController::class)->index($request);
    });
    Route::post('/employees', function (\Illuminate\Http\Request $request) {
        $request->merge(['role' => \App\Models\User::ROLE_EMPLOYEE]);
        return app(UserController::class)->store($request);
    });
    Route::get('/employees/{user}', function (\Illuminate\Http\Request $request, \App\Models\User $user) {
        abort_unless($user->role === \App\Models\User::ROLE_EMPLOYEE, 404);
        return app(UserController::class)->show($user);
    });
    Route::put('/employees/{user}', function (\Illuminate\Http\Request $request, \App\Models\User $user) {
        abort_unless($user->role === \App\Models\User::ROLE_EMPLOYEE, 404);
        // Force role regardless of what's in the payload — role changes stay
        // admin-only. merge() (not "remove from $request->request") is
        // required here because this is a JSON API: JSON bodies live in
        // $request->json(), so stripping a key from $request->request would
        // silently do nothing and leave an attacker-supplied role in place.
        $request->merge(['role' => \App\Models\User::ROLE_EMPLOYEE]);
        return app(UserController::class)->update($request, $user);
    });
    Route::delete('/employees/{user}', function (\Illuminate\Http\Request $request, \App\Models\User $user) {
        abort_unless($user->role === \App\Models\User::ROLE_EMPLOYEE, 404);
        return app(UserController::class)->destroy($user);
    });

    // Member enrollment & records.
    Route::apiResource('members', MemberController::class);

    // Full transaction visibility + void.
    Route::post('/transactions/{transaction}/void', [TransactionController::class, 'void']);

    // Financial/inventory reports + exports.
    Route::get('/reports/summary', [ReportController::class, 'summary']);
    Route::get('/reports/best-sellers', [ReportController::class, 'bestSellers']);
    Route::get('/reports/stock-valuation', [ReportController::class, 'stockValuation']);
    Route::get('/reports/daily-series', [ReportController::class, 'dailySeries']);
    Route::get('/reports/transactions/export', [ReportController::class, 'exportTransactions']);

    Route::get('/dashboard/owner-summary', [DashboardController::class, 'ownerSummary']);
});

/*
|--------------------------------------------------------------------------
| Admin only
|--------------------------------------------------------------------------
*/
Route::middleware(['auth:staff', 'role:admin', 'log.activity'])->group(function () {
    // Full user/account + role management across all staff tiers.
    Route::apiResource('users', UserController::class);

    // Business rules & promotions.
    Route::apiResource('discounts', DiscountController::class)->except(['index']);
    Route::get('/discounts', [DiscountController::class, 'index']);

    // Runtime-editable infrastructure & business-rule settings.
    Route::get('/admin/settings', [SettingsController::class, 'index']);
    Route::put('/admin/settings', [SettingsController::class, 'update']);

    // Audit log.
    Route::get('/audit-logs', [AuditLogController::class, 'index']);
    Route::get('/audit-logs/{auditLog}', [AuditLogController::class, 'show']);

    // Backup / restore.
    Route::get('/backups', [BackupController::class, 'index']);
    Route::post('/backups', [BackupController::class, 'store']);
    Route::get('/backups/{filename}/download', [BackupController::class, 'download']);
    Route::post('/backups/restore', [BackupController::class, 'restore']);
});

/*
|--------------------------------------------------------------------------
| Member portal (fully isolated from every staff route above)
|--------------------------------------------------------------------------
*/
Route::middleware(['auth:member', 'log.activity'])->prefix('member')->group(function () {
    Route::post('/logout', [MemberAuthController::class, 'logout']);
    Route::get('/me', [MemberAuthController::class, 'me']);
    Route::get('/profile', [MemberProfileController::class, 'show']);
    // POST alongside PUT: avatar uploads go out as multipart/form-data.
    Route::match(['put', 'post'], '/profile', [MemberProfileController::class, 'update']);
    Route::get('/transactions', [TransactionHistoryController::class, 'index']);
    Route::get('/transactions/{id}', [TransactionHistoryController::class, 'show']);
    Route::get('/shu', [ShuController::class, 'show']);
    Route::get('/discounts', [DiscountController::class, 'active']);
    Route::get('/dashboard-summary', [DashboardController::class, 'memberSummary']);
});
