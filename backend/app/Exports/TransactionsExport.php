<?php

namespace App\Exports;

use Illuminate\Support\Collection;
use Maatwebsite\Excel\Concerns\Exportable;
use Maatwebsite\Excel\Concerns\FromCollection;
use Maatwebsite\Excel\Concerns\WithHeadings;
use Maatwebsite\Excel\Concerns\WithMapping;

class TransactionsExport implements FromCollection, WithHeadings, WithMapping
{
    use Exportable;

    public function __construct(private readonly Collection $transactions)
    {
    }

    public function collection(): Collection
    {
        return $this->transactions;
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
