<?php

namespace App\Http\Middleware;

use Closure;
use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\Response;

/**
 * Negotiates the locale every API error message is rendered in.
 *
 * Precedence: explicit Accept-Language header (the Next.js apiFetch helper
 * always sends the UI's current language) → koperasi_lang cookie (also kept
 * in sync by the frontend) → the app default. Only en/id are honored;
 * anything else falls through to the default.
 *
 * Attached to the api middleware group in bootstrap/app.php, so it runs for
 * every /api/* request — including pre-auth ones like login, where the
 * account's stored locale isn't known yet.
 */
class SetLocale
{
    private const SUPPORTED = ['en', 'id'];

    public function handle(Request $request, Closure $next): Response
    {
        // getPreferredLanguage() falls back to the first supported locale
        // when nothing matches, so only consult it when the client actually
        // sent the header — otherwise the cookie gets a say first.
        $locale = $request->headers->has('Accept-Language')
            ? $request->getPreferredLanguage(self::SUPPORTED)
            : null;

        if (! in_array($locale, self::SUPPORTED, true)) {
            $cookie = $request->cookie('koperasi_lang');
            $locale = in_array($cookie, self::SUPPORTED, true) ? $cookie : null;
        }

        if (! in_array($locale, self::SUPPORTED, true)) {
            $locale = config('app.locale', 'id');
        }

        if (! in_array($locale, self::SUPPORTED, true)) {
            $locale = 'id';
        }

        app()->setLocale($locale);

        return $next($request);
    }
}
