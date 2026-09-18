import { clearSession, getToken } from './auth';

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

export async function apiFetch<T = unknown>(path: string, options: RequestOptions = {}): Promise<T> {
  const method = (options.method || 'GET').toUpperCase();
  if (method !== 'GET' || options.signal) {
    return doFetch<T>(path, options);
  }
  const key = `${BASE_URL}${path}`;
  const existing = inflightGets.get(key);
  if (existing) return existing as Promise<T>;
  const pending = doFetch<T>(path, options).finally(() => {
    if (inflightGets.get(key) === pending) inflightGets.delete(key);
  });
  inflightGets.set(key, pending);
  return pending;
}

async function doFetch<T = unknown>(path: string, options: RequestOptions = {}): Promise<T> {
  const token = getToken();
  const headers: Record<string, string> = {
    Accept: 'application/json',
    ...(options.isFormData ? {} : { 'Content-Type': 'application/json' }),
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...(options.headers as Record<string, string> | undefined),
  };

  const res = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers,
    body: options.body
      ? options.isFormData
        ? (options.body as FormData)
        : JSON.stringify(options.body)
      : undefined,
  });

  if (res.status === 401) {
    // Token expired/invalid — drop the stale session so the next
    // navigation is caught by middleware.ts and sent back to /login.
    clearSession();
    if (typeof window !== 'undefined') window.location.href = '/login';
    throw new ApiError(401, 'Session expired. Please log in again.');
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
