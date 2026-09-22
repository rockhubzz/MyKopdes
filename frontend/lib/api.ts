import { clearSession, getToken } from './auth';
import { getStoredLanguage, translate } from './i18n/LanguageContext';
import { clearSessionCaches } from './session-cache';

const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || '/api';

export class ApiError extends Error {
  status: number;
  errors?: Record<string, string[]>;

  constructor(status: number, message: string, errors?: Record<string, string[]>) {
    super(message);
    this.status = status;
    this.errors = errors;
  }
}

interface RequestOptions extends Omit<RequestInit, 'body'> {
  body?: unknown;
  isFormData?: boolean;
  /**
   * Cache TTL for GET responses (ms). Defaults to 15s — revisits within the
   * window (e.g. back-navigation, tab switches) resolve instantly from
   * memory without a network round trip. Pass `0` / `noCache: true` for
   * data that must always be fresh.
   */
  ttlMs?: number;
  noCache?: boolean;
}

/**
 * In-flight GET deduplication: concurrent identical GETs (e.g. a layout and
 * its page both fetching `/auth/staff/me` on mount) share one network
 * request instead of firing two. Entries are removed as soon as the request
 * settles, so nothing is ever served stale — this only collapses overlap.
 * Requests with an AbortSignal opt out (a caller aborting must not cancel a
 * request another component is still waiting on).
 */
const inflightGets = new Map<string, Promise<unknown>>();

/**
 * Short-lived in-memory GET cache for snappy client-side navigation.
 * Going back to a page visited seconds ago (or hovering a nav link that
 * prefetched its data) resolves synchronously instead of refetching.
 * Mutations clear it via `invalidateApiCache()` so tables never show stale
 * rows after a create/edit/delete.
 */
const responseCache = new Map<string, { data: unknown; expires: number }>();
const DEFAULT_GET_TTL_MS = 15_000;

function cacheKey(path: string): string {
  return `${BASE_URL}${path}`;
}

function readCache<T>(key: string): T | undefined {
  const entry = responseCache.get(key);
  if (!entry) return undefined;
  if (Date.now() > entry.expires) {
    responseCache.delete(key);
    return undefined;
  }
  return entry.data as T;
}

/** Clear cached GETs — all of them, or only those under a path prefix. */
export function invalidateApiCache(prefix?: string) {
  if (!prefix) {
    responseCache.clear();
    return;
  }
  const full = `${BASE_URL}${prefix}`;
  for (const key of responseCache.keys()) {
    if (key.startsWith(full)) responseCache.delete(key);
  }
}

/**
 * Warm the GET cache in the background (e.g. on nav-link hover). Failures
 * are swallowed — the real navigation will simply fetch on demand.
 */
export function apiPrefetch(path: string, ttlMs = DEFAULT_GET_TTL_MS): void {
  if (typeof window === 'undefined') return;
  const key = cacheKey(path);
  if (readCache(key) !== undefined || inflightGets.has(key)) return;
  apiFetch(path, { ttlMs }).catch(() => {});
}

export async function apiFetch<T = unknown>(path: string, options: RequestOptions = {}): Promise<T> {
  const method = (options.method || 'GET').toUpperCase();
  if (method !== 'GET') {
    const result = await doFetch<T>(path, options);
    // Any write may have changed list/detail/dashboard data — drop cached
    // GETs so the next read refetches exactly once, then re-caches.
    invalidateApiCache();
    return result;
  }
  if (options.noCache || options.ttlMs === 0) {
    return doFetch<T>(path, options);
  }
  const key = cacheKey(path);
  const ttlMs = options.ttlMs ?? DEFAULT_GET_TTL_MS;
  // Fast path: fresh cache hit resolves without touching the network, and
  // works even when the caller passed an AbortSignal.
  if (!inflightGets.has(key)) {
    const cached = readCache<T>(key);
    if (cached !== undefined) return cached;
  }
  if (options.signal) {
    // Abortable callers (DataTable, search-as-you-type) skip dedup/caching
    // on the way in so a superseded request can truly be cancelled — but a
    // success still warms the cache for the next visitor.
    const result = await doFetch<T>(path, options);
    if (!options.signal.aborted) {
      responseCache.set(key, { data: result, expires: Date.now() + ttlMs });
    }
    return result;
  }
  const existing = inflightGets.get(key);
  if (existing) return existing as Promise<T>;
  const pending = doFetch<T>(path, options)
    .then((result) => {
      responseCache.set(key, { data: result, expires: Date.now() + ttlMs });
      return result;
    })
    .finally(() => {
      if (inflightGets.get(key) === pending) inflightGets.delete(key);
    });
  inflightGets.set(key, pending);
  return pending;
}

async function doFetch<T = unknown>(path: string, options: RequestOptions = {}): Promise<T> {
  const token = getToken();
  const headers: Record<string, string> = {
    Accept: 'application/json',
    // Tells the API which language to render error messages in
    // (backend lang/*/api.php, negotiated by SetLocale middleware).
    'Accept-Language': getStoredLanguage(),
    ...(options.isFormData ? {} : { 'Content-Type': 'application/json' }),
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...(options.headers as Record<string, string> | undefined),
  };

  // Strip our own extensions before handing init to fetch.
  const { body, isFormData, ttlMs: _ttl, noCache: _noCache, ...fetchInit } = options;

  const res = await fetch(`${BASE_URL}${path}`, {
    ...fetchInit,
    headers,
    body: body
      ? isFormData
        ? (body as FormData)
        : JSON.stringify(body)
      : undefined,
  });

  if (res.status === 401) {
    // Token expired/invalid — drop the stale session so the next
    // navigation is caught by middleware.ts and sent back to /login.
    clearSession();
    clearSessionCaches();
    invalidateApiCache();
    if (typeof window !== 'undefined') window.location.href = '/login';
    throw new ApiError(401, translate(getStoredLanguage(), 'api.sessionExpired'));
  }

  if (res.status === 204) {
    return undefined as T;
  }

  const isJson = res.headers.get('content-type')?.includes('application/json');
  const payload = isJson ? await res.json() : await res.text();

  if (!res.ok) {
    const message = (isJson && (payload as { message?: string })?.message) || res.statusText;
    const errors = isJson ? (payload as { errors?: Record<string, string[]> })?.errors : undefined;
    throw new ApiError(res.status, message, errors);
  }

  return payload as T;
}

export function apiUrl(path: string): string {
  return `${BASE_URL}${path}`;
}

/** Resolves a storage-relative path (e.g. item.image_path) to a servable URL.
 *  Nginx serves /storage/* at the same origin as /api, one level up. */
export function storageUrl(path: string): string {
  const origin = BASE_URL.replace(/\/api\/?$/, '');
  return `${origin}/storage/${path}`;
}
