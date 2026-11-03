import clsx from 'clsx';
import { useState } from 'react';
import { useI18n } from '../i18n/I18nProvider';

type Env = 'Development' | 'Production';
type Layer = { id: string; label: string; ar: string; en: string; color: string; values: Record<string, string>; devOnly?: boolean; envFile?: boolean };

const KEYS = ['Logging:LogLevel:Default', 'ConnectionStrings:Default', 'Sandbox:TimeoutSeconds', 'Stripe:ApiKey'];

const LAYERS: Layer[] = [
  {
    id: 'json',
    label: 'appsettings.json',
    ar: 'القيم الافتراضية لكل البيئات',
    en: 'Defaults for every environment',
    color: 'bg-zinc-500',
    values: { 'Logging:LogLevel:Default': 'Information', 'ConnectionStrings:Default': 'Data Source=learn.db', 'Sandbox:TimeoutSeconds': '5' },
  },
  {
    id: 'envjson',
    label: 'appsettings.{Environment}.json',
    ar: 'تُقرأ حسب البيئة الحالية',
    en: 'Picked by the current environment',
    color: 'bg-sky-500',
    envFile: true,
    values: { 'Logging:LogLevel:Default': 'Debug' },
  },
  {
    id: 'secrets',
    label: 'User Secrets',
    ar: 'أسرار التطوير، في Development فقط',
    en: 'Dev secrets, Development only',
    color: 'bg-amber-500',
    devOnly: true,
    values: { 'Stripe:ApiKey': 'sk_test_51H…' },
  },
  {
    id: 'env',
    label: 'Environment variables',
    ar: 'تُضبط على الخادم أو في Docker',
    en: 'Set on the server or in Docker',
    color: 'bg-emerald-500',
    values: { 'ConnectionStrings:Default': 'Server=prod-db;Database=learn', 'Stripe:ApiKey': 'sk_live_9xQ…' },
  },
  {
    id: 'cli',
    label: 'Command line',
    ar: '--Key=Value عند التشغيل',
    en: '--Key=Value at startup',
    color: 'bg-fuchsia-500',
    values: { 'Sandbox:TimeoutSeconds': '10' },
  },
];

export function ConfigLayers() {
  const { lang } = useI18n();
  const [env, setEnv] = useState<Env>('Development');
  const [enabled, setEnabled] = useState<Record<string, boolean>>({ json: true, envjson: true, secrets: true, env: false, cli: false });

  const active = (layer: Layer) => enabled[layer.id] && !(layer.devOnly && env !== 'Development');
  const envJsonValues = (layer: Layer) => (layer.envFile && env === 'Production' ? { 'Logging:LogLevel:Default': 'Warning' } : layer.values);

  // Merge in order: the last active layer that defines a key wins.
  const result = KEYS.map((key) => {
    let value: string | undefined;
    let from: Layer | undefined;
    for (const layer of LAYERS) {
      const values = envJsonValues(layer);
      if (active(layer) && values[key] !== undefined) {
        value = values[key];
        from = layer;
      }
    }
    return { key, value, from };
  });

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-2 text-sm">
        <span className="font-mono text-xs text-muted" dir="ltr">
          ASPNETCORE_ENVIRONMENT =
        </span>
        {(['Development', 'Production'] as Env[]).map((e) => (
          <button
            key={e}
            onClick={() => setEnv(e)}
            className={clsx('rounded-lg border px-3 py-1 font-mono text-xs', env === e ? 'border-accent bg-accent-soft text-accent' : 'border-line text-muted')}
          >
            {e}
          </button>
        ))}
      </div>

      <div className="grid gap-2">
        {LAYERS.map((layer, i) => {
          const isActive = active(layer);
          const disabledByEnv = layer.devOnly && env !== 'Development';
          const values = envJsonValues(layer);
          return (
            <label
              key={layer.id}
              className={clsx(
                'flex cursor-pointer flex-wrap items-center gap-3 rounded-xl border p-3 transition-all',
                isActive ? 'border-line bg-surface' : 'border-dashed border-line bg-transparent opacity-55',
              )}
            >
              <input
                type="checkbox"
                checked={enabled[layer.id]}
                disabled={disabledByEnv}
                onChange={(e) => setEnabled((s) => ({ ...s, [layer.id]: e.target.checked }))}
                className="accent-[var(--accent)]"
              />
              <span className={clsx('size-2.5 rounded-full', layer.color)} />
              <span className="text-xs text-muted tabular-nums">{i + 1}</span>
              <span className="font-mono text-sm font-semibold" dir="ltr">
                {layer.envFile ? `appsettings.${env}.json` : layer.label}
              </span>
              <span className="text-xs text-muted">{disabledByEnv ? (lang === 'ar' ? 'معطّلة في Production' : 'off in Production') : lang === 'ar' ? layer.ar : layer.en}</span>
              <span className="flex w-full flex-wrap gap-1.5 ps-7" dir="ltr">
                {Object.entries(values).map(([k, v]) => (
                  <code key={k} className="rounded bg-surface-2 px-1.5 py-0.5 text-[11px] text-muted">
                    {k}={v}
                  </code>
                ))}
              </span>
            </label>
          );
        })}
      </div>

      <div className="overflow-x-auto rounded-xl border border-accent/40">
        <table className="w-full text-sm" dir="ltr">
          <thead className="bg-accent-soft/60">
            <tr>
              <th className="px-3 py-2 text-left font-semibold">builder.Configuration[key]</th>
              <th className="px-3 py-2 text-left font-semibold">value</th>
              <th className="px-3 py-2 text-left font-semibold">from</th>
            </tr>
          </thead>
          <tbody>
            {result.map((r) => (
              <tr key={r.key} className="border-t border-line">
                <td className="px-3 py-2 font-mono text-xs">{r.key}</td>
                <td className="px-3 py-2 font-mono text-xs font-semibold">{r.value ?? <span className="text-muted">null</span>}</td>
                <td className="px-3 py-2">
                  {r.from && (
                    <span className="inline-flex items-center gap-1.5 text-xs">
                      <span className={clsx('size-2 rounded-full', r.from.color)} />
                      {r.from.envFile ? `appsettings.${env}.json` : r.from.label}
                    </span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="rounded-xl bg-surface-2 px-4 py-3 text-[15px]">
        {lang === 'ar'
          ? 'جرّب: فعّل «Environment variables» وغيّر البيئة إلى Production. سلسلة الاتصال تأتي الآن من الخادم، ومفتاح Stripe التجريبي يختفي لأن User Secrets لا تعمل في Production.'
          : 'Try it: enable "Environment variables" and switch to Production. The connection string now comes from the server, and the test Stripe key disappears because user secrets are off in Production.'}
      </p>
    </div>
  );
}
