'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { en, type Dictionary } from './en';
import { id } from './id';

export type Language = 'en' | 'id';

/** Backend stores arbitrary strings — coerce to a supported language. */
export function normalizeLocale(value: unknown): Language {
  return value === 'id' ? 'id' : 'en';
}

const STORAGE_KEY = 'koperasi_lang';
const COOKIE_KEY = 'koperasi_lang';

const dictionaries: Record<Language, Dictionary> = { en, id };

/** Dot-path union of every leaf key, e.g. 'common.save' | 'nav.dashboard' | ... */
type Join<K extends string | number, P extends string> = P extends '' ? `${K}` : `${P}.${K}`;
export type TKey = {
  [K in keyof Dictionary]: Dictionary[K] extends string
    ? Join<K & string, ''>
    : Dictionary[K] extends object
      ? { [S in keyof Dictionary[K]]: Join<S & string, Join<K & string, ''>> }[keyof Dictionary[K]]
      : never;
}[keyof Dictionary];

export type Params = Record<string, string | number>;

function lookup(dict: Dictionary, key: string): string | undefined {
  let node: unknown = dict;
  for (const part of key.split('.')) {
    if (typeof node !== 'object' || node === null || !(part in node)) return undefined;
    node = (node as Record<string, unknown>)[part];
  }
  return typeof node === 'string' ? node : undefined;
}

function interpolate(template: string, params?: Params): string {
  if (!params) return template;
  return template.replace(/\{(\w+)\}/g, (_, name: string) =>
    params[name] !== undefined ? String(params[name]) : `{${name}}`
  );
}

export function translate(lang: Language, key: string, params?: Params, fallback?: string): string {
  const template = lookup(dictionaries[lang], key);
  if (template === undefined) return fallback ?? key;
  return interpolate(template, params);
}

/** Synchronous stored-language read for non-component code (e.g. lib/api.ts). */
export function getStoredLanguage(): Language {
  if (typeof window === 'undefined') return 'en';
  try {
    return window.localStorage.getItem(STORAGE_KEY) === 'id' ? 'id' : 'en';
  } catch {
    return 'en';
  }
}

interface LanguageContextValue {
  lang: Language;
  setLang: (lang: Language) => void;
  /** Type-safe lookup for static keys. */
  t: (key: TKey, params?: Params) => string;
  /**
   * Lookup for data-driven keys (e.g. `status.${paymentStatus}`).
   * Returns `fallback` verbatim when the key is missing.
   */
  tx: (key: string, fallback: string, params?: Params) => string;
}

const LanguageContext = createContext<LanguageContextValue>({
  lang: 'en',
  setLang: () => {},
  t: (key, params) => translate('en', key, params),
  tx: (key, fallback, params) => translate('en', key, params, fallback),
});

export function LanguageProvider({ children }: { children: React.ReactNode }) {
  const [lang, setLangState] = useState<Language>('en');

  // Read the persisted choice after mount to avoid a hydration mismatch
  // (server renders 'en', client adopts the stored preference).
  useEffect(() => {
    setLangState(getStoredLanguage());
  }, []);

  // Keep <html lang>, localStorage, and a cookie (for future server use) in sync.
  useEffect(() => {
    document.documentElement.lang = lang;
    try {
      window.localStorage.setItem(STORAGE_KEY, lang);
    } catch {
      // Private mode etc. — the in-memory choice still applies.
    }
    document.cookie = `${COOKIE_KEY}=${lang}; path=/; max-age=31536000`;
  }, [lang]);

  const setLang = useCallback((next: Language) => setLangState(next), []);

  const value = useMemo<LanguageContextValue>(
    () => ({
      lang,
      setLang,
      t: (key, params) => translate(lang, key, params),
      tx: (key, fallback, params) => translate(lang, key, params, fallback),
    }),
    [lang, setLang]
  );

  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>;
}

export function useLanguage(): LanguageContextValue {
  return useContext(LanguageContext);
}
