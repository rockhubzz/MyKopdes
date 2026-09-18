'use client';

import { useEffect, useState } from 'react';
import { apiFetch, apiUrl, ApiError } from '@/lib/api';
import { getToken } from '@/lib/auth';

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
      setMessage('Backup completed.');
      load();
    } catch (err) {
      setMessage(err instanceof ApiError ? err.message : 'Backup failed.');
    } finally {
      setBusy(false);
    }
  }

  async function restore(filename: string) {
    if (!confirm(`This will OVERWRITE the current database with "${filename}". Continue?`)) return;
    setBusy(true);
    setMessage(null);
    try {
      await apiFetch('/backups/restore', { method: 'POST', body: { filename } });
      setMessage('Restore completed.');
    } catch (err) {
      setMessage(err instanceof ApiError ? err.message : 'Restore failed.');
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
        <h1 className="text-2xl font-bold text-koperasi-800">Backup & Restore</h1>
        <button className="btn-primary" onClick={triggerBackup} disabled={busy}>
          {busy ? 'Working...' : '+ Backup Now'}
        </button>
      </div>
      <p className="text-sm text-koperasi-500 -mt-3">
        Automated backups also run on the schedule configured under Settings → Backup Schedule.
      </p>

      {message && <p className="text-sm text-koperasi-700">{message}</p>}

      <div className="card overflow-x-auto p-0">
        <table className="data-table">
          <thead>
            <tr>
              <th>File</th>
              <th>Size</th>
              <th>Created</th>
              <th className="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr>
                <td colSpan={4} className="text-center py-6 text-koperasi-400">
                  Loading...
                </td>
              </tr>
            )}
            {!loading && files.length === 0 && (
              <tr>
                <td colSpan={4} className="text-center py-6 text-koperasi-400">
                  No backups yet.
                </td>
              </tr>
            )}
            {files.map((f) => (
              <tr key={f.name}>
                <td>{f.name}</td>
                <td>{(f.size_bytes / 1024).toFixed(1)} KB</td>
                <td>{new Date(f.created_at * 1000).toLocaleString('id-ID')}</td>
                <td className="text-right space-x-3 whitespace-nowrap">
                  <button className="text-koperasi-600 hover:underline text-sm" onClick={() => handleDownload(f.name)}>
                    Download
                  </button>
                  <button className="text-red-600 hover:underline text-sm" onClick={() => restore(f.name)} disabled={busy}>
                    Restore
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
