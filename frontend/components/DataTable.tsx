'use client';

import { memo, useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';
import { useDebouncedValue } from '@/lib/useDebouncedValue';
import type { Paginated } from '@/lib/types';

export interface Column<T> {
  header: string;
  render: (row: T) => React.ReactNode;
  className?: string;
}

const PAGE_SIZE = 15;

function SkeletonRows({ cols }: { cols: number }) {
  return (
    <>
      {Array.from({ length: 5 }).map((_, i) => (
        <tr key={i} aria-hidden="true">
          <td colSpan={cols} className="py-2 px-3">
            <div className="skeleton h-4 rounded" />
          </td>
        </tr>
      ))}
    </>
  );
}

function DataTableInner<T extends { id: number }>({
  endpoint,
  columns,
  searchable = true,
  extraParams = '',
  actions,
  reloadKey,
  onRowClick,
}: {
  endpoint: string;
  columns: Column<T>[];
  searchable?: boolean;
  extraParams?: string;
  actions?: (row: T) => React.ReactNode;
  reloadKey?: number | string;
  /** When set, rows become clickable (and keyboard-activatable) and invoke this. Action buttons still work independently. */
  onRowClick?: (row: T) => void;
}) {
  const [data, setData] = useState<Paginated<T> | null>(null);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Fetch once per pause in typing, not once per keystroke.
  const debouncedSearch = useDebouncedValue(search, 300);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError(null);

    const params = new URLSearchParams({ page: String(page), per_page: String(PAGE_SIZE) });
    if (debouncedSearch) params.set('search', debouncedSearch);

    apiFetch<Paginated<T>>(`${endpoint}?${params.toString()}${extraParams}`, { signal: controller.signal })
      .then((res) => setData(res))
      .catch((e) => {
        // Aborted superseded request — not an error worth showing.
        if (controller.signal.aborted) return;
        setError(e.message || 'Failed to load.');
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });

    // Abort the in-flight request when params change or on unmount, so a
    // slow earlier response can never overwrite newer results.
    return () => controller.abort();
  }, [endpoint, page, debouncedSearch, extraParams, reloadKey]);

  const colCount = columns.length + (actions ? 1 : 0);

  return (
    <div>
      {searchable && (
        <div className="mb-3">
          <label htmlFor={`datatable-search-${endpoint}`} className="sr-only">
            Search
          </label>
          <input
            id={`datatable-search-${endpoint}`}
            className="input max-w-xs"
            placeholder="Search..."
            autoComplete="off"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
          />
        </div>
      )}

      <div className="card overflow-x-auto p-0">
        <table className="data-table">
          <thead>
            <tr>
              {columns.map((c) => (
                <th key={c.header} scope="col" className={c.className}>
                  {c.header}
                </th>
              ))}
              {actions && (
                <th scope="col" className="text-right">
                  Actions
                </th>
              )}
            </tr>
          </thead>
          <tbody>
            {loading && !data && <SkeletonRows cols={colCount} />}
            {loading && data && (
              <tr aria-hidden="true">
                <td colSpan={colCount} className="p-0">
                  <div className="skeleton h-1" />
                </td>
              </tr>
            )}
            {!loading && error && (
              <tr>
                <td colSpan={colCount} className="text-center py-6 text-red-500" role="alert">
                  {error}
                </td>
              </tr>
            )}
            {!loading && !error && data?.data.length === 0 && (
              <tr>
                <td colSpan={colCount} className="text-center py-6 text-koperasi-400">
                  No records found.
                </td>
              </tr>
            )}
            {!error &&
              data?.data.map((row) => (
                <tr
                  key={row.id}
                  onClick={onRowClick ? () => onRowClick(row) : undefined}
                  onKeyDown={
                    onRowClick
                      ? (e) => {
                          if (e.key === 'Enter' || e.key === ' ') {
                            e.preventDefault();
                            onRowClick(row);
                          }
                        }
                      : undefined
                  }
                  tabIndex={onRowClick ? 0 : undefined}
                  className={onRowClick ? 'cursor-pointer' : undefined}
                >
                  {columns.map((c) => (
                    <td key={c.header} className={c.className}>
                      {c.render(row)}
                    </td>
                  ))}
                  {actions && (
                    <td
                      className="text-right whitespace-nowrap"
                      // Action buttons (edit/void/…) must not open the detail view.
                      onClick={(e) => e.stopPropagation()}
                      onKeyDown={(e) => e.stopPropagation()}
                    >
                      {actions(row)}
                    </td>
                  )}
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      {data && data.last_page > 1 && (
        <div className="flex items-center justify-between mt-3 text-sm text-koperasi-500">
          <span>
            Page {data.current_page} of {data.last_page} ({data.total} total)
          </span>
          <div className="space-x-2">
            <button
              type="button"
              className="btn-secondary px-3 py-1"
              disabled={page <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
            >
              Previous
            </button>
            <button
              type="button"
              className="btn-secondary px-3 py-1"
              disabled={page >= data.last_page}
              onClick={() => setPage((p) => p + 1)}
            >
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

// memo() skips re-renders when ancestors re-render without changing our
// props (common: a sibling modal opening above the table). Inline `columns`
// arrays still re-render the table, but that path is cheap now that network
// fetches are debounced and abortable.
export default memo(DataTableInner) as typeof DataTableInner;
