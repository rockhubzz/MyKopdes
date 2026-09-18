<?php

namespace App\Services;

use App\Models\Discount;
use Illuminate\Support\Collection;

class DiscountService
{
    /**
     * @param  Collection<int, array{item: \App\Models\Item, quantity: int, subtotal: float}>  $cartLines
     * @param  bool  $hasMember  whether a member is attached to this sale (unlocks member-only discounts)
     * @return array{discount: ?Discount, amount: float}
     */
    public function bestDiscountFor(Collection $cartLines, bool $hasMember, ?int $explicitDiscountId = null): array
    {
        $subtotal = (float) $cartLines->sum('subtotal');

        $candidates = Discount::query()
            ->active()
            ->visibleTo($hasMember)
            ->when($explicitDiscountId, fn ($q) => $q->where('id', $explicitDiscountId))
            ->get()
            ->filter(fn (Discount $d) => ! $d->min_purchase || $subtotal >= (float) $d->min_purchase);

        if ($candidates->isEmpty()) {
            return ['discount' => null, 'amount' => 0.0];
        }

        // Pick whichever active, eligible discount saves the customer the
        // most — an admin can always steer this by scoping/expiring others.
        $best = null;
        $bestAmount = 0.0;

        foreach ($candidates as $discount) {
            $amount = $this->computeAmount($discount, $cartLines, $subtotal);
            if ($amount > $bestAmount) {
                $bestAmount = $amount;
                $best = $discount;
            }
        }

        return ['discount' => $best, 'amount' => round($bestAmount, 2)];
    }

    private function computeAmount(Discount $discount, Collection $cartLines, float $subtotal): float
    {
        return match ($discount->type) {
            Discount::TYPE_PERCENTAGE => $subtotal * ((float) $discount->value / 100),
            Discount::TYPE_FLAT => min((float) $discount->value, $subtotal),
            Discount::TYPE_BOGO => $this->computeBogo($discount, $cartLines),
            default => 0.0,
        };
    }

    /** Buy-X-get-Y: for each qualifying group of (buy_qty + get_qty) units of an
     *  item in the discount's category (or any item if no category is set),
     *  get_qty units are free at that item's unit price. */
    private function computeBogo(Discount $discount, Collection $cartLines): float
    {
        if (! $discount->buy_qty || ! $discount->get_qty) {
            return 0.0;
        }

        $groupSize = $discount->buy_qty + $discount->get_qty;
        $total = 0.0;

        foreach ($cartLines as $line) {
            $item = $line['item'];
            if ($discount->category_id && $item->category_id !== $discount->category_id) {
                continue;
            }
            $groups = intdiv($line['quantity'], $groupSize);
            $total += $groups * $discount->get_qty * (float) $item->unit_price;
        }

        return $total;
    }
}
