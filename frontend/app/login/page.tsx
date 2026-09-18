'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { apiFetch, ApiError } from '@/lib/api';
import { dashboardPathFor, setSession } from '@/lib/auth';
import type { Member, Role, StaffUser } from '@/lib/types';

export default function LoginPage() {
  const router = useRouter();
  const [mode, setMode] = useState<'staff' | 'member'>('staff');
  const [identifier, setIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      if (mode === 'staff') {
        const res = await apiFetch<{ token: string; user: StaffUser }>('/auth/staff/login', {
          method: 'POST',
          body: { email: identifier, password },
        });
        const role: Role = res.user.role;
        setSession(res.token, role);
        router.push(dashboardPathFor(role));
      } else {
        const res = await apiFetch<{ token: string; member: Member }>('/auth/member/login', {
          method: 'POST',
          body: { identifier, password },
        });
        setSession(res.token, 'member');
        router.push(dashboardPathFor('member'));
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-koperasi-900 via-koperasi-800 to-koperasi-600 px-4 py-8">
      <div className="w-full max-w-md">
        <div className="text-center mb-6">
          <div className="text-4xl mb-2" aria-hidden="true">
            🌾
          </div>
          <h1 className="text-2xl font-bold text-white tracking-tight">Koperasi Store</h1>
          <p className="text-koperasi-100/90 text-sm mt-1">Village cooperative management system</p>
        </div>

        <div className="card shadow-xl">
          <div className="grid grid-cols-2 gap-1 mb-5 bg-koperasi-100 rounded-lg p-1">
            {(['staff', 'member'] as const).map((m) => (
              <button
                key={m}
                type="button"
                onClick={() => {
                  setMode(m);
                  setError(null);
                }}
                className={`py-2 rounded-md text-sm font-medium transition-colors ${
                  mode === m ? 'bg-white shadow text-koperasi-800' : 'text-koperasi-600'
                }`}
              >
                {m === 'staff' ? 'Staff Login' : 'Member Login'}
              </button>
            ))}
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label htmlFor="login-identifier" className="label">
                {mode === 'staff' ? 'Email' : 'Membership ID or Phone'}
              </label>
              <input
                id="login-identifier"
                className="input"
                type={mode === 'staff' ? 'email' : 'text'}
                value={identifier}
                onChange={(e) => setIdentifier(e.target.value)}
                placeholder={mode === 'staff' ? 'admin@koperasi.test' : 'KOP-26-XXXXX or 0813...'}
                autoComplete={mode === 'staff' ? 'email' : 'username'}
                required
              />
            </div>
            <div>
              <label htmlFor="login-password" className="label">
                Password
              </label>
              <input
                id="login-password"
                className="input"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete="current-password"
                required
              />
            </div>

            {error && (
              <p className="text-sm text-red-600" role="alert">
                {error}
              </p>
            )}

            <button type="submit" className="btn-primary w-full" disabled={loading}>
              {loading ? 'Signing in...' : 'Sign In'}
            </button>
          </form>

          <p className="text-xs text-koperasi-400 mt-4 text-center">
            Demo: admin@koperasi.test / owner@koperasi.test / kasir1@koperasi.test — password &quot;password&quot;
          </p>
        </div>
      </div>
    </div>
  );
}
