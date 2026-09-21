'use client';

import { useEffect, useState } from 'react';
import { apiFetch, ApiError } from '@/lib/api';
import { useLanguage } from '@/lib/i18n/LanguageContext';

interface SettingsResponse {
  infrastructure: Record<string, string>;
  business_rules: Record<string, string>;
  backup: Record<string, string>;
}

export default function SettingsPage() {
  const [settings, setSettings] = useState<SettingsResponse | null>(null);
  const [infra, setInfra] = useState({ store_name: '', store_address: '', api_base_url: '', db_host: '', db_port: '' });
  const [rules, setRules] = useState({ tax_rate: '0', shu_rate: '0.02', low_stock_default_threshold: '10' });
  const [backup, setBackup] = useState({ backup_schedule_cron: '0 2 * * *' });
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const { t } = useLanguage();

  useEffect(() => {
    apiFetch<SettingsResponse>('/admin/settings').then((res) => {
      setSettings(res);
      setInfra((prev) => ({ ...prev, ...res.infrastructure }));
      setRules((prev) => ({ ...prev, ...res.business_rules }));
      setBackup((prev) => ({ ...prev, ...res.backup }));
    });
  }, []);

  async function save(group: 'infrastructure' | 'business_rules' | 'backup', values: Record<string, string>) {
    setStatus(null);
    setError(null);
    try {
      await apiFetch('/admin/settings', { method: 'PUT', body: { group, values } });
      setStatus(t('settings.saved'));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t('settings.saveFailed'));
    }
  }

  return (
    <div className="space-y-6 max-w-2xl">
      <h1 className="text-2xl font-bold text-koperasi-800">{t('settings.title')}</h1>
      <p className="text-sm text-koperasi-500 -mt-4">
        {t('settings.intro')}
      </p>

      {status && <p className="text-sm text-green-600">{status}</p>}
      {error && <p className="text-sm text-red-600">{error}</p>}

      <section className="card space-y-3">
        <h2 className="font-semibold text-koperasi-800">{t('settings.infra')}</h2>
        <div>
          <label className="label">{t('settings.storeName')}</label>
          <input className="input" value={infra.store_name} onChange={(e) => setInfra({ ...infra, store_name: e.target.value })} />
        </div>
        <div>
          <label className="label">{t('settings.storeAddress')}</label>
          <input className="input" value={infra.store_address} onChange={(e) => setInfra({ ...infra, store_address: e.target.value })} />
        </div>
        <div>
          <label className="label">{t('settings.apiBase')}</label>
          <input className="input" value={infra.api_base_url} onChange={(e) => setInfra({ ...infra, api_base_url: e.target.value })} />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="label">{t('settings.dbHost')}</label>
            <input className="input" value={infra.db_host} onChange={(e) => setInfra({ ...infra, db_host: e.target.value })} />
          </div>
          <div>
            <label className="label">{t('settings.dbPort')}</label>
            <input className="input" value={infra.db_port} onChange={(e) => setInfra({ ...infra, db_port: e.target.value })} />
          </div>
        </div>
        <button className="btn-primary" onClick={() => save('infrastructure', infra)}>
          {t('settings.saveInfra')}
        </button>
      </section>

      <section className="card space-y-3">
        <h2 className="font-semibold text-koperasi-800">{t('settings.businessRules')}</h2>
        <div>
          <label className="label">{t('settings.taxRate')}</label>
          <input className="input" type="number" step="0.01" value={rules.tax_rate} onChange={(e) => setRules({ ...rules, tax_rate: e.target.value })} />
        </div>
        <div>
          <label className="label">{t('settings.shuRate')}</label>
          <input className="input" type="number" step="0.001" value={rules.shu_rate} onChange={(e) => setRules({ ...rules, shu_rate: e.target.value })} />
        </div>
        <div>
          <label className="label">{t('settings.lowThreshold')}</label>
          <input
            className="input"
            type="number"
            value={rules.low_stock_default_threshold}
            onChange={(e) => setRules({ ...rules, low_stock_default_threshold: e.target.value })}
          />
        </div>
        <button className="btn-primary" onClick={() => save('business_rules', rules)}>
          {t('settings.saveRules')}
        </button>
      </section>

      <section className="card space-y-3">
        <h2 className="font-semibold text-koperasi-800">{t('settings.backupSchedule')}</h2>
        <div>
          <label className="label">{t('settings.cronExpr')}</label>
          <input
            className="input"
            value={backup.backup_schedule_cron}
            onChange={(e) => setBackup({ ...backup, backup_schedule_cron: e.target.value })}
          />
          <p className="text-xs text-koperasi-400 mt-1">{t('settings.cronDefault')}</p>
        </div>
        <button className="btn-primary" onClick={() => save('backup', backup)}>
          {t('settings.saveBackup')}
        </button>
      </section>
    </div>
  );
}
