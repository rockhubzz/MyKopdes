<?php

namespace App\Http\Middleware;

use App\Models\User;
use Closure;
use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\Response;

/**
 * Usage: ->middleware('role:employee')   // admin, shop_owner, employee may pass
 *        ->middleware('role:shop_owner') // admin, shop_owner may pass
 *        ->middleware('role:admin')      // admin only
 *
 * Matches the brief's hierarchy: "Admin: all Shop Owner and Employee
 * permissions, plus ...". A route is written for the LOWEST role allowed to
 * use it; anyone ranked at or above that role also passes.
 */
class EnsureRole
{
    public function handle(Request $request, Closure $next, string $minimumRole): Response
    {
        /** @var User|null $user */
        $user = $request->user('staff');

        // A member token must never satisfy a staff route: without the
        // instanceof check a Member model would blow up on hasAtLeast()
        // instead of getting a clean 403.
        if (! $user instanceof User || ! in_array($minimumRole, User::ROLES, true)) {
            abort(403, __('api.role.forbidden'));
        }

        if (! $user->is_active) {
            abort(403, __('api.role.deactivated'));
        }

        if (! $user->hasAtLeast($minimumRole)) {
            abort(403, __('api.role.forbidden'));
        }

        return $next($request);
    }
}
