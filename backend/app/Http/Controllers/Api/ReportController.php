<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Transaction;
use App\Services\ReportService;
use Illuminate\Http\Request;
use Illuminate\Support\Carbon;
use Illuminate\Support\Facades\Cache;

class ReportController extends Controller
{
    public function __construct(private readonly ReportService $reports)
    {
    }

    private function range(Request $request): array
    {
        $to = $request->filled('to') ? Carbon::parse((string) $request->string('to'))->endOfDay() : now()->endOfDay();
        $from = $request->filled('from') ? Carbon::parse((string) $request->string('from'))->startOfDay() : $to->copy()->subDays(29)->startOfDay();

        return [$from, $to];
    }

    public function summary(Request $request)
    {
        [$from, $to] = $this->range($request);

        // Same figures for everyone asking about the same range — 60s of
        // caching absorbs the owner + admin dashboards hitting this together.
        $key = "koperasi:reports:summary:{$from->toDateString()}:{$to->toDateString()}";

        return response()->json(Cache::remember($key, 60, fn () => $this->reports->summary($from, $to)));
    }

    public function bestSellers(Request $request)
    {
        [$from, $to] = $this->range($request);
        $limit = min(max($request->integer('limit', 10), 1), 50);

        $key = "koperasi:reports:best-sellers:{$from->toDateString()}:{$to->toDateString()}:{$limit}";

        return response()->json(Cache::remember($key, 60, fn () => $this->reports->bestSellingItems($from, $to, $limit)));
    }

    public function stockValuation(Request $request)
    {
        // Totals only by default (what the screens render); the full per-item
        // breakdown is opt-in so large catalogs don't pay for rows nobody shows.
        $withItems = $request->boolean('with_items');

        if ($withItems) {
            return response()->json($this->reports->stockValuation(true));
        }

        return response()->json(
            Cache::remember('koperasi:reports:stock-valuation', 60, fn () => $this->reports->stockValuation())
        );
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
        // Cast: $request->string() returns a Stringable object, and a strict
        // ===/match against a plain string is always false — without this,
        // ?format=xlsx / pdf silently fell through to the CSV branch.
        $format = (string) $request->string('format', 'csv');

        if ($format === 'xlsx') {
            // Chunk-read from the DB cursor (see TransactionsExport) — a
            // year-long range no longer hydrates every row up front.
            return (new \App\Exports\TransactionsExport($from, $to))->download(
                'transactions_'.$from->toDateString().'_'.$to->toDateString().'.xlsx'
            );
        }

        if ($format === 'pdf') {
            $transactions = $this->exportQuery($from, $to)->get();

            return \Barryvdh\DomPDF\Facade\Pdf::loadView('exports.transactions-pdf', compact('transactions', 'from', 'to'))
                ->download('transactions_'.$from->toDateString().'.pdf');
        }

        return $this->streamCsv($from, $to);
    }

    /**
     * Shared export read: only the columns the CSV/PDF sheets print, with
     * names via constrained eager loads instead of full user/member rows.
     */
    private function exportQuery(Carbon $from, Carbon $to)
    {
        return Transaction::with(['cashier:id,name', 'member:id,name'])
            ->whereBetween('created_at', [$from, $to])
            ->orderBy('created_at');
    }

    private function streamCsv(Carbon $from, Carbon $to)
    {
        $filename = 'transactions_'.$from->toDateString().'_'.$to->toDateString().'.csv';

        // Rows flow through a DB cursor inside the download stream, so memory
        // stays flat no matter how wide the date range is.
        $callback = function () use ($from, $to) {
            $handle = fopen('php://output', 'w');
            fputcsv($handle, ['Code', 'Date', 'Cashier', 'Member', 'Subtotal', 'Discount', 'Tax', 'Total', 'Payment Method', 'Status']);
            foreach ($this->exportQuery($from, $to)->cursor() as $t) {
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
