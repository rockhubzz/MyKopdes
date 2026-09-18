'use client';

import { useEffect, useState } from 'react';
import { apiFetch, ApiError } from '@/lib/api';
import type { Member } from '@/lib/types';

export default function MemberProfilePage() {
  const [member, setMember] = useState<Member | null>(null);
  const [form, setForm] = useState({ phone: '', email: '', address: '', password: '' });
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    apiFetch<Member>('/member/profile').then((m) => {
      setMember(m);
      setForm({ phone: m.phone ?? '', email: m.email ?? '', address: m.address ?? '', password: '' });
    });
  }, []);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSuccess(null);
    try {
      const body: Record<string, unknown> = { ...form };
      if (!body.password) delete body.password;
      const updated = await apiFetch<Member>('/member/profile', { method: 'PUT', body });
      setMember(updated);
      setForm((f) => ({ ...f, password: '' }));
      setSuccess('Profile updated.');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to update.');
    }
  }

  if (!member) return <p className="text-koperasi-400">Loading...</p>;

  return (
    <div className="max-w-md space-y-4">
      <h1 className="text-2xl font-bold text-koperasi-800">My Profile</h1>

      <div className="card space-y-1 text-sm">
        <div className="flex justify-between">
          <span className="text-koperasi-400">Name</span>
          <span className="font-medium">{member.name}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-koperasi-400">Membership ID</span>
          <span className="font-mono">{member.membership_id}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-koperasi-400">Joined</span>
          <span>{member.join_date}</span>
        </div>
        <p className="text-xs text-koperasi-400 pt-2">
          Name, membership ID, and join date are managed by cooperative staff. You can update your contact info below.
        </p>
      </div>

      <form onSubmit={handleSubmit} className="card space-y-3">
        <div>
          <label className="label">Phone</label>
          <input className="input" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
        </div>
        <div>
          <label className="label">Email</label>
          <input className="input" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
        </div>
        <div>
          <label className="label">Address</label>
          <textarea className="input" value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} />
        </div>
        <div>
          <label className="label">New Password (leave blank to keep current)</label>
          <input className="input" type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
        </div>

        {error && <p className="text-sm text-red-600">{error}</p>}
        {success && <p className="text-sm text-green-600">{success}</p>}

        <button type="submit" className="btn-primary w-full">
          Save Changes
        </button>
      </form>
    </div>
  );
}
