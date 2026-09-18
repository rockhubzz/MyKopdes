<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Transaction;
use App\Services\ReportService;
use Illuminate\Http\Request;
use Illuminate\Support\Carbon;

class ReportController extends Controller
{
    public function __construct(private readonly ReportService $reports)
    {
    }

    private function range(Request $request): array
    {
        $to = $request->filled('to') ? Carbon::parse($request->string('to'))->endOfDay() : now()->endOfDay();
        $from = $request->filled('from') ? Carbon::parse($request->string('from'))->startOfDay() : $to->copy()->subDays(29)->startOfDay();

        return [$from, $to];
    }

    public function summary(Request $request)
    {
        [$from, $to] = $this->range($request);

        return response()->json($this->reports->summary($from, $to));
    }

    public function bestSellers(Request $request)
    {
        [$from, $to] = $this->range($request);

        return response()->json($this->reports->bestSellingItems($from, $to, $request->integer('limit', 10)));
    }

    public function stockValuation()
    {
        return response()->json($this->reports->stockValuation());
    }

    public function dailySeries(Request $request)
    {
        [$from, $to] = $this->range($request);

        return response()->json($this->reports->dailySeries($from, $to));
    }

    /** GET /api/reports/transactions/export?format=csv|xlsx|pdf&from=&to= */
    public function exportTransactions(Request $request)
    {
        [$from, $to] = $this->range($request);
        $format = $request->string('format', 'csv');

        $transactions = Transaction::with(['member', 'cashier'])
            ->whereBetween('created_at', [$from, $to])
            ->orderBy('created_at')
            ->get();

        return match ($format) {
            'xlsx' => (new \App\Exports\TransactionsExport($transactions))->download(
                'transactions_'.$from->toDateString().'_'.$to->toDateString().'.xlsx'
            ),
            'pdf' => \Barryvdh\DomPDF\Facade\Pdf::loadView('exports.transactions-pdf', compact('transactions', 'from', 'to'))
                ->download('transactions_'.$from->toDateString().'.pdf'),
            default => $this->streamCsv($transactions, $from, $to),
        };
    }

    private function streamCsv($transactions, Carbon $from, Carbon $to)
    {
        $filename = 'transactions_'.$from->toDateString().'_'.$to->toDateString().'.csv';

        $callback = function () use ($transactions) {
            $handle = fopen('php://output', 'w');
            fputcsv($handle, ['Code', 'Date', 'Cashier', 'Member', 'Subtotal', 'Discount', 'Tax', 'Total', 'Payment Method', 'Status']);
            foreach ($transactions as $t) {
                fputcsv($handle, [
                    $t->transaction_code, $t->created_at->toDateTimeString(), $t->cashier->name ?? '-',
                    $t->member->name ?? '-', $t->subtotal, $t->discount_amount, $t->tax_amount, $t->total,
                    $t->payment_method, $t->payment_status,
                ]);
            }
            fclose($handle);
        };

        return response()->streamDownload($callback, $filename, ['Content-Type' => 'text/csv']);
    }
}
