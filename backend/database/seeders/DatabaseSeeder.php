<?php

namespace Database\Seeders;

use App\Models\Discount;
use App\Models\Item;
use App\Models\ItemCategory;
use App\Models\Member;
use App\Models\RestockingRecord;
use App\Models\Setting;
use App\Models\Supplier;
use App\Models\Transaction;
use App\Models\TransactionItem;
use App\Models\User;
use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Str;

/**
 * Demo/testing seed data per the brief's "Seed data for demo/testing"
 * requirement: sample items, users, members, and transactions, plus the
 * initial rows for the runtime-editable `settings` table so the app has
 * sane defaults on first boot.
 *
 * Run with: php artisan migrate:fresh --seed
 * (scripts/first-run.sh does this automatically on a brand-new install.)
 */
class DatabaseSeeder extends Seeder
{
    public function run(): void
    {
        $this->seedSettings();
        $users = $this->seedUsers();
        $categories = $this->seedCategories();
        $suppliers = $this->seedSuppliers();
        $items = $this->seedItems($categories);
        $this->seedRestockingRecords($items, $suppliers, $users);
        $this->seedDiscounts($categories);
        $members = $this->seedMembers();
        $this->seedTransactions($items, $members, $users);

        $this->command->info('Seed complete. Demo logins:');
        $this->command->table(['Role', 'Email / ID', 'Password'], [
            ['Admin', 'admin@koperasi.test', 'password'],
            ['Shop Owner', 'owner@koperasi.test', 'password'],
            ['Employee', 'kasir1@koperasi.test', 'password'],
            ['Member', $members->first()->membership_id.' (or phone '.$members->first()->phone.')', 'password'],
        ]);
    }

    private function seedSettings(): void
    {
        $rows = [
            ['key' => 'store_name', 'value' => 'Koperasi Sejahtera Bersama', 'group' => 'infrastructure', 'is_public' => true],
            ['key' => 'store_address', 'value' => 'Jl. Raya Desa No. 1, Malang, Jawa Timur', 'group' => 'infrastructure', 'is_public' => true],
            ['key' => 'api_base_url', 'value' => env('APP_URL', 'http://localhost'), 'group' => 'infrastructure', 'is_public' => false],
            ['key' => 'db_host', 'value' => env('DB_HOST', 'mysql'), 'group' => 'infrastructure', 'is_public' => false],
            ['key' => 'db_port', 'value' => (string) env('DB_PORT', 3306), 'group' => 'infrastructure', 'is_public' => false],
            ['key' => 'tax_rate', 'value' => '0', 'group' => 'business_rules', 'is_public' => false],
            ['key' => 'shu_rate', 'value' => '0.02', 'group' => 'business_rules', 'is_public' => false],
            ['key' => 'low_stock_default_threshold', 'value' => '10', 'group' => 'business_rules', 'is_public' => false],
            ['key' => 'backup_schedule_cron', 'value' => '0 2 * * *', 'group' => 'backup', 'is_public' => false],
        ];

        foreach ($rows as $row) {
            Setting::updateOrCreate(['key' => $row['key']], $row);
        }
    }

    private function seedUsers()
    {
        $admin = User::updateOrCreate(['email' => 'admin@koperasi.test'], [
            'name' => 'Budi Santoso', 'password' => Hash::make('password'), 'role' => User::ROLE_ADMIN,
            'phone' => '081200000001', 'is_active' => true,
        ]);
        $owner = User::updateOrCreate(['email' => 'owner@koperasi.test'], [
            'name' => 'Siti Aminah', 'password' => Hash::make('password'), 'role' => User::ROLE_SHOP_OWNER,
            'phone' => '081200000002', 'is_active' => true,
        ]);
        $emp1 = User::updateOrCreate(['email' => 'kasir1@koperasi.test'], [
            'name' => 'Agus Wijaya', 'password' => Hash::make('password'), 'role' => User::ROLE_EMPLOYEE,
            'phone' => '081200000003', 'shift_label' => 'Pagi (07:00-15:00)', 'is_active' => true,
        ]);
        $emp2 = User::updateOrCreate(['email' => 'kasir2@koperasi.test'], [
            'name' => 'Dewi Lestari', 'password' => Hash::make('password'), 'role' => User::ROLE_EMPLOYEE,
            'phone' => '081200000004', 'shift_label' => 'Sore (15:00-21:00)', 'is_active' => true,
        ]);

        return collect([$admin, $owner, $emp1, $emp2]);
    }

    private function seedCategories()
    {
        $names = ['Sembako (Staples)', 'Minuman (Beverages)', 'Makanan Ringan (Snacks)', 'Kebersihan (Household)', 'Pertanian (Farming Supplies)'];

        return collect($names)->map(fn ($name) => ItemCategory::updateOrCreate(['name' => $name], [
            'description' => "Kategori $name",
        ]));
    }

    private function seedSuppliers()
    {
        $suppliers = [
            ['name' => 'CV Sumber Makmur', 'contact_person' => 'Pak Hendra', 'phone' => '081311110001'],
            ['name' => 'PT Distribusi Nusantara', 'contact_person' => 'Bu Rina', 'phone' => '081311110002'],
            ['name' => 'Koperasi Tani Makmur', 'contact_person' => 'Pak Slamet', 'phone' => '081311110003'],
        ];

        return collect($suppliers)->map(fn ($s) => Supplier::updateOrCreate(['name' => $s['name']], $s));
    }

    private function seedItems($categories)
    {
        $catByName = $categories->keyBy('name');
        $rows = [
            ['name' => 'Beras Premium 5kg', 'sku' => 'SKU-0001', 'barcode' => '8991001000015', 'category' => 'Sembako (Staples)', 'unit_price' => 68000, 'cost_price' => 60000, 'uom' => 'sack', 'stock' => 40, 'min' => 10],
            ['name' => 'Minyak Goreng 1L', 'sku' => 'SKU-0002', 'barcode' => '8991001000022', 'category' => 'Sembako (Staples)', 'unit_price' => 18000, 'cost_price' => 15500, 'uom' => 'bottle', 'stock' => 60, 'min' => 15],
            ['name' => 'Gula Pasir 1kg', 'sku' => 'SKU-0003', 'barcode' => '8991001000039', 'category' => 'Sembako (Staples)', 'unit_price' => 15000, 'cost_price' => 13000, 'uom' => 'pack', 'stock' => 50, 'min' => 15],
            ['name' => 'Teh Celup Kotak', 'sku' => 'SKU-0004', 'barcode' => '8991001000046', 'category' => 'Minuman (Beverages)', 'unit_price' => 9000, 'cost_price' => 7000, 'uom' => 'box', 'stock' => 35, 'min' => 10],
            ['name' => 'Kopi Sachet Renceng', 'sku' => 'SKU-0005', 'barcode' => '8991001000053', 'category' => 'Minuman (Beverages)', 'unit_price' => 12000, 'cost_price' => 9500, 'uom' => 'pack', 'stock' => 45, 'min' => 10],
            ['name' => 'Kerupuk Udang', 'sku' => 'SKU-0006', 'barcode' => '8991001000060', 'category' => 'Makanan Ringan (Snacks)', 'unit_price' => 8000, 'cost_price' => 6000, 'uom' => 'pack', 'stock' => 30, 'min' => 8],
            ['name' => 'Biskuit Kaleng', 'sku' => 'SKU-0007', 'barcode' => '8991001000077', 'category' => 'Makanan Ringan (Snacks)', 'unit_price' => 25000, 'cost_price' => 20000, 'uom' => 'tin', 'stock' => 20, 'min' => 5],
            ['name' => 'Sabun Cuci Piring', 'sku' => 'SKU-0008', 'barcode' => '8991001000084', 'category' => 'Kebersihan (Household)', 'unit_price' => 7500, 'cost_price' => 6000, 'uom' => 'bottle', 'stock' => 5, 'min' => 10],
            ['name' => 'Deterjen Bubuk 1kg', 'sku' => 'SKU-0009', 'barcode' => '8991001000091', 'category' => 'Kebersihan (Household)', 'unit_price' => 22000, 'cost_price' => 18000, 'uom' => 'pack', 'stock' => 3, 'min' => 10],
            ['name' => 'Pupuk Urea 5kg', 'sku' => 'SKU-0010', 'barcode' => '8991001000107', 'category' => 'Pertanian (Farming Supplies)', 'unit_price' => 45000, 'cost_price' => 38000, 'uom' => 'sack', 'stock' => 25, 'min' => 5],
        ];

        return collect($rows)->map(fn ($r) => Item::updateOrCreate(['sku' => $r['sku']], [
            'name' => $r['name'], 'barcode' => $r['barcode'], 'category_id' => $catByName[$r['category']]->id,
            'unit_price' => $r['unit_price'], 'cost_price' => $r['cost_price'], 'unit_of_measure' => $r['uom'],
            'current_stock' => $r['stock'], 'min_stock_threshold' => $r['min'], 'is_active' => true,
        ]));
    }

    private function seedRestockingRecords($items, $suppliers, $users)
    {
        $employee = $users->firstWhere('role', User::ROLE_EMPLOYEE);

        $items->take(5)->each(function ($item, $i) use ($suppliers, $employee) {
            RestockingRecord::create([
                'item_id' => $item->id,
                'supplier_id' => $suppliers[$i % $suppliers->count()]->id,
                'quantity' => 20,
                'cost_per_unit' => $item->cost_price,
                'total_cost' => 20 * $item->cost_price,
                'submitted_by' => $employee->id,
                'notes' => 'Initial demo restock',
                'restocked_at' => now()->subDays(10 - $i),
            ]);
        });
    }

    private function seedDiscounts($categories)
    {
        Discount::updateOrCreate(['name' => 'Diskon Kemerdekaan 10%'], [
            'description' => 'Diskon umum 10% untuk semua pelanggan',
            'type' => Discount::TYPE_PERCENTAGE, 'value' => 10, 'scope' => Discount::SCOPE_GENERAL,
            'is_active' => true, 'starts_at' => now()->subDays(3), 'ends_at' => now()->addDays(27),
        ]);

        Discount::updateOrCreate(['name' => 'Diskon Anggota 5%'], [
            'description' => 'Diskon eksklusif anggota koperasi',
            'type' => Discount::TYPE_PERCENTAGE, 'value' => 5, 'scope' => Discount::SCOPE_MEMBER,
            'is_active' => true,
        ]);

        Discount::updateOrCreate(['name' => 'Beli 2 Gratis 1 - Kopi Sachet'], [
            'description' => 'Buy 2 get 1 free',
            'type' => Discount::TYPE_BOGO, 'buy_qty' => 2, 'get_qty' => 1, 'scope' => Discount::SCOPE_GENERAL,
            'is_active' => true,
        ]);
    }

    private function seedMembers()
    {
        $rows = [
            ['name' => 'Sri Wahyuni', 'phone' => '081399990001', 'email' => 'sri@example.test'],
            ['name' => 'Joko Prasetyo', 'phone' => '081399990002', 'email' => 'joko@example.test'],
            ['name' => 'Rina Marlina', 'phone' => '081399990003', 'email' => 'rina@example.test'],
        ];

        return collect($rows)->map(fn ($r, $i) => Member::updateOrCreate(['phone' => $r['phone']], [
            'membership_id' => 'KOP-26-'.Str::upper(Str::random(5)),
            'name' => $r['name'], 'email' => $r['email'], 'password' => Hash::make('password'),
            'address' => 'Desa Sukamaju, RT 0'.($i + 1),
            'join_date' => now()->subMonths(6 + $i), 'shu_balance' => 0, 'is_active' => true,
        ]));
    }

    private function seedTransactions($items, $members, $users)
    {
        $cashier = $users->firstWhere('role', User::ROLE_EMPLOYEE);

        foreach (range(1, 8) as $i) {
            $lines = $items->random(rand(1, 3));
            $subtotal = 0;
            $txItems = $lines->map(function ($item) use (&$subtotal) {
                $qty = rand(1, 3);
                $lineSubtotal = $qty * (float) $item->unit_price;
                $subtotal += $lineSubtotal;
                return ['item_id' => $item->id, 'quantity' => $qty, 'unit_price' => $item->unit_price, 'subtotal' => $lineSubtotal];
            });

            $member = $i % 2 === 0 ? $members->random() : null;

            $transaction = Transaction::create([
                'transaction_code' => 'TRX-DEMO-'.Str::upper(Str::random(6)),
                'member_id' => $member?->id,
                'cashier_id' => $cashier->id,
                'subtotal' => round($subtotal, 2),
                'discount_amount' => 0,
                'tax_amount' => 0,
                'total' => round($subtotal, 2),
                'payment_method' => [Transaction::PAYMENT_CASH, Transaction::PAYMENT_QRIS, Transaction::PAYMENT_BANK_TRANSFER][rand(0, 2)],
                'payment_status' => Transaction::STATUS_PAID,
                'paid_at' => now()->subDays(rand(0, 14)),
            ]);

            foreach ($txItems as $line) {
                TransactionItem::create([...$line, 'transaction_id' => $transaction->id]);
            }

            if ($member) {
                $member->increment('shu_balance', round($subtotal * 0.02, 2));
            }
        }
    }
}
