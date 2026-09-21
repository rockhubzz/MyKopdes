'use client';

import { useEffect, useState } from 'react';
import { apiFetch, apiUrl, ApiError } from '@/lib/api';
import { getToken } from '@/lib/auth';
import { useLanguage } from '@/lib/i18n/LanguageContext';

interface BackupFile {
  name: string;
  size_bytes: number;
  created_at: number;
}

export default function BackupsPage() {
  const [files, setFiles] = useState<BackupFile[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const { t } = useLanguage();

  function load() {
    setLoading(true);
    apiFetch<BackupFile[]>('/backups')
      .then(setFiles)
      .finally(() => setLoading(false));
  }

  useEffect(load, []);

  async function triggerBackup() {
    setBusy(true);
    setMessage(null);
    try {
      await apiFetch('/backups', { method: 'POST' });
      setMessage(t('backups.backupDone'));
      load();
    } catch (err) {
      setMessage(err instanceof ApiError ? err.message : t('backups.backupFailed'));
    } finally {
      setBusy(false);
    }
  }

  async function restore(filename: string) {
    if (!confirm(t('backups.restoreConfirm', { file: filename }))) return;
    setBusy(true);
    setMessage(null);
    try {
      await apiFetch('/backups/restore', { method: 'POST', body: { filename } });
      setMessage(t('backups.restoreDone'));
    } catch (err) {
      setMessage(err instanceof ApiError ? err.message : t('backups.restoreFailed'));
    } finally {
      setBusy(false);
    }
  }

  function downloadUrl(filename: string) {
    // Direct download needs the bearer token too; since <a href> can't set
    // headers, we open it via fetch->blob for a simple, dependency-free
    // authenticated download.
    return apiUrl(`/backups/${filename}/download`);
  }

  async function handleDownload(filename: string) {
    const res = await fetch(downloadUrl(filename), { headers: { Authorization: `Bearer ${getToken()}` } });
    const blob = await res.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    window.URL.revokeObjectURL(url);
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-koperasi-800">{t('backups.title')}</h1>
        <button className="btn-primary" onClick={triggerBackup} disabled={busy}>
          {busy ? t('backups.working') : t('backups.backupNow')}
        </button>
      </div>
      <p className="text-sm text-koperasi-500 -mt-3">
        {t('backups.intro')}
      </p>

      {message && <p className="text-sm text-koperasi-700">{message}</p>}

      <div className="card overflow-x-auto p-0">
        <table className="data-table">
          <thead>
            <tr>
              <th>{t('backups.colFile')}</th>
              <th>{t('backups.colSize')}</th>
              <th>{t('backups.colCreated')}</th>
              <th className="text-right">{t('backups.colActions')}</th>
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr>
                <td colSpan={4} className="text-center py-6 text-koperasi-400">
                  {t('common.loading')}
                </td>
              </tr>
            )}
            {!loading && files.length === 0 && (
              <tr>
                <td colSpan={4} className="text-center py-6 text-koperasi-400">
                  {t('backups.empty')}
                </td>
              </tr>
            )}
            {files.map((f) => (
              <tr key={f.name}>
                <td>{f.name}</td>
                <td>{(f.size_bytes / 1024).toFixed(1)} KB</td>
                <td>{new Date(f.created_at * 1000).toLocaleString('id-ID')}</td>
                <td className="text-right whitespace-nowrap">
                  <div className="flex justify-end gap-1.5">
                    <button className="btn-action-edit" onClick={() => handleDownload(f.name)}>
                      {t('backups.download')}
                    </button>
                    <button className="btn-action-danger" onClick={() => restore(f.name)} disabled={busy}>
                      {t('backups.restore')}
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
