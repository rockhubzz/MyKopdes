<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <style>
        body { font-family: sans-serif; font-size: 11px; }
        h2 { margin-bottom: 0; }
        table { width: 100%; border-collapse: collapse; margin-top: 12px; }
        th, td { border: 1px solid #ccc; padding: 4px 6px; text-align: left; }
        th { background: #f0f0f0; }
        .right { text-align: right; }
    </style>
</head>
<body>
    <h2>Transaction Report</h2>
    <p>{{ $from->toDateString() }} &ndash; {{ $to->toDateString() }}</p>
    <table>
        <thead>
            <tr>
                <th>Code</th><th>Date</th><th>Cashier</th><th>Member</th>
                <th class="right">Subtotal</th><th class="right">Discount</th>
                <th class="right">Tax</th><th class="right">Total</th>
                <th>Payment</th><th>Status</th>
            </tr>
        </thead>
        <tbody>
        @foreach ($transactions as $t)
            <tr>
                <td>{{ $t->transaction_code }}</td>
                <td>{{ $t->created_at->format('Y-m-d H:i') }}</td>
                <td>{{ $t->cashier->name ?? '-' }}</td>
                <td>{{ $t->member->name ?? '-' }}</td>
                <td class="right">{{ number_format($t->subtotal, 2) }}</td>
                <td class="right">{{ number_format($t->discount_amount, 2) }}</td>
                <td class="right">{{ number_format($t->tax_amount, 2) }}</td>
                <td class="right">{{ number_format($t->total, 2) }}</td>
                <td>{{ $t->payment_method }}</td>
                <td>{{ $t->payment_status }}</td>
            </tr>
        @endforeach
        </tbody>
    </table>
</body>
</html>
