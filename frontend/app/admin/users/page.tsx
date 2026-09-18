'use client';

import { useState } from 'react';
import DataTable, { Column } from '@/components/DataTable';
import DetailModal, { DetailTarget } from '@/components/details/DetailModal';
import { apiFetch, ApiError } from '@/lib/api';
import type { StaffRole, StaffUser } from '@/lib/types';

const emptyForm = { name: '', email: '', password: '', role: 'employee' as StaffRole, phone: '', shift_label: '' };

export default function UsersPage() {
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<StaffUser | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [detail, setDetail] = useState<DetailTarget | null>(null);

  function openCreate() {
    setEditing(null);
    setForm(emptyForm);
    setError(null);
    setShowForm(true);
  }

  function openEdit(u: StaffUser) {
    setEditing(u);
    setForm({ name: u.name, email: u.email, password: '', role: u.role, phone: u.phone ?? '', shift_label: u.shift_label ?? '' });
    setError(null);
    setShowForm(true);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      const body: Record<string, unknown> = { ...form };
      if (!body.password) delete body.password;

      if (editing) {
        await apiFetch(`/users/${editing.id}`, { method: 'PUT', body });
      } else {
        await apiFetch('/users', { method: 'POST', body });
      }
      setShowForm(false);
      setReloadKey((k) => k + 1);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to save.');
    }
  }

  async function handleDeactivate(u: StaffUser) {
    if (!confirm(`Deactivate ${u.name}?`)) return;
    await apiFetch(`/users/${u.id}`, { method: 'DELETE' });
    setReloadKey((k) => k + 1);
  }

  const columns: Column<StaffUser>[] = [
    { header: 'Name', render: (u) => u.name },
    { header: 'Email', render: (u) => u.email },
    {
      header: 'Role',
      render: (u) => (
        <span className="badge bg-koperasi-100 text-koperasi-700 capitalize">{u.role.replace('_', ' ')}</span>
      ),
    },
    { header: 'Phone', render: (u) => u.phone ?? '—' },
    {
      header: 'Status',
      render: (u) => (
        <span className={`badge ${u.is_active ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
          {u.is_active ? 'Active' : 'Inactive'}
        </span>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-koperasi-800">Staff Accounts</h1>
        <button className="btn-primary" onClick={openCreate}>
          + Add Staff
        </button>
      </div>

      {detail && <DetailModal target={detail} onClose={() => setDetail(null)} />}
      <DataTable
        endpoint="/users"
        columns={columns}
        onRowClick={(u) => setDetail({ entity: 'user', id: u.id })}
        reloadKey={reloadKey}
        actions={(u) => (
          <div className="space-x-2">
            <button className="text-koperasi-600 hover:underline text-sm" onClick={() => openEdit(u)}>
              Edit
            </button>
            <button className="text-red-600 hover:underline text-sm" onClick={() => handleDeactivate(u)}>
              Deactivate
            </button>
          </div>
        )}
      />

      {showForm && (
        <div className="fixed inset-0 bg-black/30 flex items-center justify-center z-50 p-4">
          <div className="card w-full max-w-md">
            <h2 className="font-semibold text-lg mb-4">{editing ? 'Edit Staff' : 'Add Staff'}</h2>
            <form onSubmit={handleSubmit} className="space-y-3">
              <div>
                <label className="label">Name</label>
                <input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
              </div>
              <div>
                <label className="label">Email</label>
                <input
                  className="input"
                  type="email"
                  value={form.email}
                  onChange={(e) => setForm({ ...form, email: e.target.value })}
                  required
                />
              </div>
              <div>
                <label className="label">Password {editing && '(leave blank to keep current)'}</label>
                <input
                  className="input"
                  type="password"
                  value={form.password}
                  onChange={(e) => setForm({ ...form, password: e.target.value })}
                  {...(!editing ? { required: true } : {})}
                />
              </div>
              <div>
                <label className="label">Role</label>
                <select className="input" value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value as StaffRole })}>
                  <option value="admin">Admin</option>
                  <option value="shop_owner">Shop Owner</option>
                  <option value="employee">Employee</option>
                </select>
              </div>
              <div>
                <label className="label">Phone</label>
                <input className="input" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
              </div>
              <div>
                <label className="label">Shift Label (employees)</label>
                <input
                  className="input"
                  value={form.shift_label}
                  onChange={(e) => setForm({ ...form, shift_label: e.target.value })}
                  placeholder="e.g. Pagi (07:00-15:00)"
                />
              </div>

              {error && <p className="text-sm text-red-600">{error}</p>}

              <div className="flex justify-end gap-2 pt-2">
                <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>
                  Cancel
                </button>
                <button type="submit" className="btn-primary">
                  Save
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
