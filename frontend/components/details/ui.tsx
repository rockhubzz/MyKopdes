'use client';

import type { ReactNode } from 'react';

export function formatRp(n: number | string): string {
  return 'Rp ' + Number(n).toLocaleString('id-ID');
}

export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return '—';
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? String(iso) : d.toLocaleString('id-ID');
}

export function formatDate(iso: string | null | undefined): string {
  if (!iso) return '—';
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? String(iso) : d.toLocaleDateString('id-ID');
}

export function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="mt-5 first:mt-0">
      <h3 className="text-xs font-semibold uppercase tracking-wider text-koperasi-400 mb-2">{title}</h3>
      {children}
    </div>
  );
}

export function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex justify-between gap-4 py-1.5 text-sm border-b border-koperasi-50 last:border-b-0">
      <span className="text-koperasi-400 shrink-0">{label}</span>
      <span className="text-right font-medium text-koperasi-800 break-words min-w-0">{children}</span>
    </div>
  );
}

/** Inline button that drills into another entity's detail view. */
export function Drill({ onOpen, children }: { onOpen: () => void; children: ReactNode }) {
  return (
    <button type="button" onClick={onOpen} className="text-koperasi-600 hover:underline font-medium text-right">
      {children}
    </button>
  );
}

export function EmptyNote({ children }: { children: ReactNode }) {
  return <p className="text-sm text-koperasi-400 py-2">{children}</p>;
}
