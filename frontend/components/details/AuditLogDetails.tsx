'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';
import type { AuditLog } from '@/lib/types';
import { Field, Section, formatDateTime } from './ui';

export function AuditLogDetails({ id }: { id: number }) {
  const [log, setLog] = useState<AuditLog | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLog(null);
    setError(null);
    apiFetch<AuditLog>(`/audit-logs/${id}`)
      .then((res) => {
        if (!cancelled) setLog(res);
      })
      .catch((e) => {
        if (!cancelled) setError(e.message || 'Failed to load audit entry.');
      });
    return () => {
      cancelled = true;
    };
  }, [id]);

  if (error) {
    return (
      <p className="text-sm text-red-600" role="alert">
        {error}
      </p>
    );
  }
  if (!log) {
    return (
      <div className="space-y-2" aria-label="Loading details">
        {Array.from({ length: 6 }).map((_, i) => (
          <div key={i} className="skeleton h-5 rounded" />
        ))}
      </div>
    );
  }

  return (
    <div>
      <div className="font-bold text-lg text-koperasi-800 capitalize">{log.action.replace(/_/g, ' ')}</div>
      <div className="text-xs text-koperasi-400">{formatDateTime(log.created_at)}</div>

      <Section title="Actor">
        <Field label="Name">{log.actor?.name ?? `ID #${log.actor_id ?? '—'}`}</Field>
        {log.actor?.email && <Field label="Email">{log.actor.email}</Field>}
        <Field label="Type">{log.actor_type?.split('\\').pop() ?? '—'}</Field>
        <Field label="IP address">{log.ip_address ?? '—'}</Field>
      </Section>

      <Section title="Subject">
        <Field label="Entity">{log.auditable_type?.split('\\').pop() ?? '—'}</Field>
        <Field label="Entity ID">{log.auditable_id ?? '—'}</Field>
      </Section>

      {log.changes && (
        <Section title="Recorded changes">
          <pre className="text-xs bg-koperasi-50 rounded-lg p-3 overflow-x-auto text-koperasi-800">
            {JSON.stringify(log.changes, null, 2)}
          </pre>
        </Section>
      )}
    </div>
  );
}
