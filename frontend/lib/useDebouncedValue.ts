import { useEffect, useState } from 'react';

/**
 * Returns `value` delayed by `delayMs` after the last change — for search
 * inputs and date filters so we fetch once per pause in typing instead of
 * once per keystroke.
 */
export function useDebouncedValue<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(timer);
  }, [value, delayMs]);

  return debounced;
}
