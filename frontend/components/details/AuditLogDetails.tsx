'use client';

import { useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';
import type { AuditLog } from '@/lib/types';
import { Field, Section, formatDateTime } from './ui';

export function AuditLogDetails({ id }: { id: number }) {
  const [log, setLog] = useState<AuditLog | null>(null);
  const [error, setError] = useState<string | null>(null);
  const { t } = useLanguage();

  useEffect(() => {
    let cancelled = false;
    setLog(null);
    setError(null);
    apiFetch<AuditLog>(`/audit-logs/${id}`)
      .then((res) => {
        if (!cancelled) setLog(res);
      })
      .catch((e) => {
        if (!cancelled) setError(e.message || t('details.failedAudit'));
      });
    return () => {
      cancelled = true;
    };
  }, [id, t]);

  if (error) {
    return (
      <p className="text-sm text-red-600" role="alert">
        {error}
      </p>
    );
  }
  if (!log) {
    return (
      <div className="space-y-2" aria-label={t('details.loading')}>
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

      <Section title={t('details.actor')}>
        <Field label={t('details.name')}>{log.actor?.name ?? `ID #${log.actor_id ?? '—'}`}</Field>
        {log.actor?.email && <Field label={t('details.email')}>{log.actor.email}</Field>}
        <Field label={t('details.type')}>{log.actor_type?.split('\\').pop() ?? '—'}</Field>
        <Field label={t('details.ip')}>{log.ip_address ?? '—'}</Field>
      </Section>

      <Section title={t('details.subject')}>
        <Field label={t('details.entity')}>{log.auditable_type?.split('\\').pop() ?? '—'}</Field>
        <Field label={t('details.entityId')}>{log.auditable_id ?? '—'}</Field>
      </Section>

      {log.changes && (
        <Section title={t('details.recordedChanges')}>
          <pre className="text-xs bg-koperasi-50 rounded-lg p-3 overflow-x-auto text-koperasi-800">
            {JSON.stringify(log.changes, null, 2)}
          </pre>
        </Section>
      )}
    </div>
  );
}
