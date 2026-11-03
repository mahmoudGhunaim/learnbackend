import clsx from 'clsx';
import { AlertOctagon, AlertTriangle, CheckCircle2, ChevronDown, Clock, ShieldAlert, XCircle } from 'lucide-react';
import { useState } from 'react';
import { useI18n } from '../i18n/I18nProvider';
import type { CodeDiagnostic, RunOutcome, TestCaseResult } from '../lib/types';

export function OutcomeBanner({ outcome, passed, total, elapsedMs }: { outcome: RunOutcome; passed?: number; total?: number; elapsedMs?: number }) {
  const { t } = useI18n();
  const good = outcome === 'Passed' || outcome === 'Completed';
  const Icon = good ? CheckCircle2 : outcome === 'Blocked' ? ShieldAlert : outcome === 'Timeout' ? Clock : outcome === 'CompileError' ? AlertOctagon : XCircle;
  return (
    <div className={clsx('flex flex-wrap items-center gap-3 rounded-xl px-4 py-3', good ? 'bg-ok-soft text-ok' : 'bg-bad-soft text-bad')}>
      <Icon className="size-5 shrink-0" />
      <span className="font-semibold">{t(`outcome.${outcome}`)}</span>
      {total !== undefined && total > 0 && (
        <span className="text-sm tabular-nums" dir="ltr">
          {passed}/{total}
        </span>
      )}
      {elapsedMs !== undefined && elapsedMs > 0 && (
        <span className="ms-auto text-xs opacity-70 tabular-nums" dir="ltr">
          {elapsedMs} ms
        </span>
      )}
    </div>
  );
}

export function DiagnosticsList({ diagnostics, onJump }: { diagnostics: CodeDiagnostic[]; onJump?: (line: number) => void }) {
  const { t, lang } = useI18n();
  if (diagnostics.length === 0) return null;
  const sorted = [...diagnostics].sort((a, b) => (a.severity === b.severity ? a.line - b.line : a.severity === 'error' ? -1 : 1));
  return (
    <ul className="space-y-2">
      {sorted.map((d, i) => (
        <li key={i} className={clsx('rounded-xl border p-3 text-sm', d.severity === 'error' ? 'border-bad/40 bg-bad-soft/40' : 'border-warn/40 bg-warn-soft/40')}>
          <div className="mb-1 flex flex-wrap items-center gap-2">
            {d.severity === 'error' ? <XCircle className="size-4 text-bad" /> : <AlertTriangle className="size-4 text-warn" />}
            <code className="text-xs font-semibold">{d.id}</code>
            {d.line > 0 ? (
              <button onClick={() => onJump?.(d.line)} className="rounded bg-surface-3 px-1.5 text-xs hover:text-accent" dir="ltr">
                {t('diag.line')} {d.line}:{d.column}
              </button>
            ) : (
              d.source === 'Tests' && <span className="text-xs text-muted">{t('diag.fromTests')}</span>
            )}
          </div>
          {d.messageAr && lang === 'ar' && (
            <p className="mb-1 leading-7" dir="rtl">
              {d.messageAr}
            </p>
          )}
          <p className="font-mono text-[12.5px] text-muted" dir="ltr">
            {d.message}
          </p>
          {(lang === 'ar' ? d.tipAr : d.tipEn ?? d.tipAr) && (
            <p className="mt-1.5 text-[13px]" dir={lang === 'ar' || !d.tipEn ? 'rtl' : 'ltr'}>
              <span className="font-semibold text-accent">{t('diag.tip')}: </span>
              {lang === 'ar' ? d.tipAr : d.tipEn ?? d.tipAr}
            </p>
          )}
        </li>
      ))}
    </ul>
  );
}

export function TestList({ tests, onJump }: { tests: TestCaseResult[]; onJump?: (line: number) => void }) {
  return (
    <ul className="space-y-1.5">
      {tests.map((test) => (
        <TestRow key={test.name} test={test} onJump={onJump} />
      ))}
    </ul>
  );
}

function TestRow({ test, onJump }: { test: TestCaseResult; onJump?: (line: number) => void }) {
  const { t, lang } = useI18n();
  const passed = test.status === 'Passed';
  const [open, setOpen] = useState(!passed);
  const hasDetails = !passed || !!test.output;
  const Icon = passed ? CheckCircle2 : test.status === 'Timeout' ? Clock : test.status === 'NotRun' ? AlertTriangle : XCircle;

  return (
    <li className={clsx('rounded-xl border', passed ? 'border-line bg-surface' : 'border-bad/40 bg-bad-soft/30')}>
      <button onClick={() => hasDetails && setOpen((o) => !o)} className="flex w-full items-center gap-2.5 px-3 py-2 text-start text-sm">
        <Icon className={clsx('size-4 shrink-0', passed ? 'text-ok' : test.status === 'NotRun' ? 'text-warn' : 'text-bad')} />
        <span className="flex-1">{lang === 'ar' ? test.ar : test.en}</span>
        {test.status === 'NotRun' && <span className="text-xs text-warn">{t('test.NotRun')}</span>}
        {hasDetails && <ChevronDown className={clsx('size-4 text-muted transition', open && 'rotate-180')} />}
      </button>
      {open && hasDetails && (
        <div className="space-y-2 border-t border-line/70 px-3 py-2.5 text-sm">
          {test.message && (
            <p className="font-mono text-[12.5px]" dir="ltr">
              {test.exceptionType && test.status === 'Error' && <span className="text-bad">{test.exceptionType.split('.').pop()}: </span>}
              {test.message}
            </p>
          )}
          {test.line && (
            <button onClick={() => onJump?.(test.line!)} className="text-xs text-accent hover:underline">
              {t('test.atLine')} {test.line}
            </button>
          )}
          {(test.expected !== null || test.actual !== null) && (
            <div className="grid gap-2 sm:grid-cols-2" dir="ltr">
              <ValueBox label={t('test.expected')} value={test.expected} tone="ok" />
              <ValueBox label={t('test.actual')} value={test.actual} tone="bad" />
            </div>
          )}
          {test.output && <ValueBox label={t('test.output')} value={test.output} tone="neutral" />}
        </div>
      )}
    </li>
  );
}

function ValueBox({ label, value, tone }: { label: string; value: string | null; tone: 'ok' | 'bad' | 'neutral' }) {
  return (
    <div dir="ltr">
      <div className={clsx('mb-0.5 text-[11px] font-semibold', tone === 'ok' && 'text-ok', tone === 'bad' && 'text-bad', tone === 'neutral' && 'text-muted')}>{label}</div>
      <pre className="overflow-x-auto rounded-lg bg-code px-2.5 py-1.5 font-mono text-[12.5px] whitespace-pre-wrap">{value ?? 'null'}</pre>
    </div>
  );
}
