<?php

namespace App\Http\Middleware;

use App\Models\AuditLog;
use Closure;
use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\Response;

/**
 * Blanket safety net for the Admin "full audit log of all system changes"
 * requirement: every mutating request (POST/PUT/PATCH/DELETE) that
 * completes with a 2xx status is recorded, in addition to the more
 * specific, human-readable entries individual controllers write for
 * high-value actions (login, checkout, settings changes, restocks).
 */
class LogActivity
{
    public function handle(Request $request, Closure $next): Response
    {
        $response = $next($request);

        if (in_array($request->method(), ['POST', 'PUT', 'PATCH', 'DELETE'], true)
            && $response->getStatusCode() < 300
            && ! $request->routeIs('auth.login')) {
            $actor = $request->user('staff') ?? $request->user('member');

            AuditLog::create([
                'actor_type' => $actor ? get_class($actor) : null,
                'actor_id' => $actor?->id,
                'action' => strtolower($request->method()).' '.$request->path(),
                'auditable_type' => null,
                'auditable_id' => null,
                'changes' => collect($request->all())->except(['password', 'password_confirmation'])->toArray(),
                'ip_address' => $request->ip(),
                'created_at' => now(),
            ]);
        }

        return $response;
    }
}
