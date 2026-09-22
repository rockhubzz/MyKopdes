<?php

namespace App\Exports;

use App\Models\Transaction;
use Illuminate\Support\Carbon;
use Maatwebsite\Excel\Concerns\Exportable;
use Maatwebsite\Excel\Concerns\FromQuery;
use Maatwebsite\Excel\Concerns\WithHeadings;
use Maatwebsite\Excel\Concerns\WithMapping;

/**
 * Query-backed export: maatwebsite/excel reads the rows in chunks straight
 * from the DB cursor instead of requiring the caller to hydrate the whole
 * range into memory first. Only the columns the sheet prints are selected.
 */
class TransactionsExport implements FromQuery, WithHeadings, WithMapping
{
    use Exportable;

    public function __construct(
        private readonly Carbon $from,
        private readonly Carbon $to,
    ) {
    }

    public function query()
    {
        return Transaction::query()
            ->with(['cashier:id,name', 'member:id,name'])
            ->whereBetween('created_at', [$this->from, $this->to])
            ->orderBy('created_at');
    }

    public function headings(): array
    {
        return ['Code', 'Date', 'Cashier', 'Member', 'Subtotal', 'Discount', 'Tax', 'Total', 'Payment Method', 'Status'];
    }

    public function map($t): array
    {
        return [
            $t->transaction_code,
            $t->created_at->toDateTimeString(),
            $t->cashier->name ?? '-',
            $t->member->name ?? '-',
            $t->subtotal,
            $t->discount_amount,
            $t->tax_amount,
            $t->total,
            $t->payment_method,
            $t->payment_status,
        ];
    }
}
