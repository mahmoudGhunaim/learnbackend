import clsx from 'clsx';
import { Pause, Play, RotateCcw, StepForward } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useI18n } from '../i18n/I18nProvider';

const SOURCE = [1, 2, 3, 4, 5, 6, 7, 8];

type Event =
  | { kind: 'where'; item: number; pass: boolean }
  | { kind: 'materialize'; items: number[] }
  | { kind: 'select'; item: number; result: number }
  | { kind: 'take'; result: number; count: number }
  | { kind: 'stop' };

/** Simulates the exact order in which LINQ calls each lambda. */
function simulate(eager: boolean): Event[] {
  const events: Event[] = [];
  let taken = 0;
  if (!eager) {
    // numbers.Where(n => n % 2 == 0).Select(n => n * 10).Take(2): one item flows all the way through before the next starts.
    for (const n of SOURCE) {
      const pass = n % 2 === 0;
      events.push({ kind: 'where', item: n, pass });
      if (!pass) continue;
      events.push({ kind: 'select', item: n, result: n * 10 });
      taken++;
      events.push({ kind: 'take', result: n * 10, count: taken });
      if (taken === 2) break;
    }
  } else {
    // numbers.Where(...).ToList().Select(...).Take(2): ToList forces Where over EVERYTHING first.
    const list: number[] = [];
    for (const n of SOURCE) {
      const pass = n % 2 === 0;
      events.push({ kind: 'where', item: n, pass });
      if (pass) list.push(n);
    }
    events.push({ kind: 'materialize', items: list });
    for (const n of list) {
      events.push({ kind: 'select', item: n, result: n * 10 });
      taken++;
      events.push({ kind: 'take', result: n * 10, count: taken });
      if (taken === 2) break;
    }
  }
  events.push({ kind: 'stop' });
  return events;
}

export function LinqPipeline() {
  const { lang } = useI18n();
  const [eager, setEager] = useState(false);
  const [pos, setPos] = useState(0);
  const [playing, setPlaying] = useState(false);
  const events = useMemo(() => simulate(eager), [eager]);
  const done = events.slice(0, pos);
  const current = done[done.length - 1];

  useEffect(() => {
    if (!playing) return;
    if (pos >= events.length) {
      setPlaying(false);
      return;
    }
    const id = setTimeout(() => setPos((p) => p + 1), 650);
    return () => clearTimeout(id);
  }, [playing, pos, events.length]);

  const whereCalls = done.filter((e) => e.kind === 'where').length;
  const selectCalls = done.filter((e) => e.kind === 'select').length;
  const output = done.filter((e): e is Extract<Event, { kind: 'take' }> => e.kind === 'take').map((e) => e.result);
  const touched = new Set(done.filter((e): e is Extract<Event, { kind: 'where' }> => e.kind === 'where').map((e) => e.item));
  const finished = pos >= events.length;

  const describe = (e: Event | undefined) => {
    if (!e) return lang === 'ar' ? 'لم يبدأ أي شيء: بناء الاستعلام لا يشغّله.' : 'Nothing has run: building the query does not execute it.';
    switch (e.kind) {
      case 'where':
        return `Where(${e.item}) → ${e.pass ? '✓' : '✗'}`;
      case 'materialize':
        return `ToList() → [${e.items.join(', ')}]`;
      case 'select':
        return `Select(${e.item}) → ${e.result}`;
      case 'take':
        return `Take: ${e.count}/2 → ${e.result}`;
      case 'stop':
        return lang === 'ar' ? 'انتهى: Take(2) حصل على عنصرين فتوقف كل شيء.' : 'Done: Take(2) got two items, so everything stops.';
    }
  };

  const reset = (nextEager = eager) => {
    setEager(nextEager);
    setPos(0);
    setPlaying(false);
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-2 text-sm" dir="ltr">
        {[false, true].map((mode) => (
          <button
            key={String(mode)}
            onClick={() => reset(mode)}
            className={clsx('rounded-lg border px-3 py-1.5 font-mono text-xs', eager === mode ? 'border-accent bg-accent-soft text-accent' : 'border-line text-muted')}
          >
            {mode ? '.Where(...).ToList().Select(...).Take(2)' : '.Where(...).Select(...).Take(2)'}
          </button>
        ))}
      </div>

      <div dir="ltr" className="space-y-3">
        <div className="flex flex-wrap items-center gap-1.5">
          <span className="w-20 text-xs text-muted">numbers</span>
          {SOURCE.map((n) => {
            const isCurrent = current?.kind === 'where' && current.item === n;
            return (
              <span
                key={n}
                className={clsx(
                  'grid size-9 place-items-center rounded-lg border font-mono text-sm transition-all',
                  isCurrent && 'animate-pulse-ring scale-110 border-accent bg-accent-soft',
                  !isCurrent && touched.has(n) && 'border-line bg-surface-2 text-muted',
                  !touched.has(n) && finished && 'border-dashed border-line text-muted/40',
                  !touched.has(n) && !finished && 'border-line',
                )}
                title={!touched.has(n) && finished ? 'never evaluated' : undefined}
              >
                {n}
              </span>
            );
          })}
        </div>

        <div className="grid gap-2 sm:grid-cols-3">
          <Stage label="Where(n => n % 2 == 0)" calls={whereCalls} active={current?.kind === 'where'} result={current?.kind === 'where' ? (current.pass ? '✓ pass' : '✗ skip') : undefined} />
          <Stage label="Select(n => n * 10)" calls={selectCalls} active={current?.kind === 'select'} result={current?.kind === 'select' ? `${current.item} → ${current.result}` : undefined} />
          <Stage label="Take(2)" calls={output.length} active={current?.kind === 'take'} result={output.length ? `[${output.join(', ')}]` : undefined} />
        </div>
      </div>

      <p className="rounded-xl bg-surface-2 px-4 py-3 font-mono text-sm" dir="auto">
        {describe(current)}
      </p>

      {finished && (
        <p className="rounded-xl border border-ok/40 bg-ok-soft/60 px-4 py-3 text-sm">
          {eager
            ? lang === 'ar'
              ? `ToList() أجبرت Where على المرور على كل العناصر الثمانية (${whereCalls} استدعاءات) رغم أننا احتجنا عنصرين فقط.`
              : `ToList() forced Where over all 8 items (${whereCalls} calls) although we only needed two.`
            : lang === 'ar'
              ? `بفضل التنفيذ المؤجل استُدعيت Where ${whereCalls} مرات فقط، والأرقام 5 إلى 8 لم تُلمس أبداً.`
              : `Thanks to deferred execution, Where ran only ${whereCalls} times; 5 to 8 were never touched.`}
        </p>
      )}

      <div className="flex flex-wrap gap-2">
        <button onClick={() => setPlaying((p) => !p)} disabled={finished} className="inline-flex h-9 items-center gap-1.5 rounded-lg bg-accent px-4 text-sm font-medium text-white disabled:opacity-40">
          {playing ? <Pause className="size-4" /> : <Play className="size-4" />}
          {playing ? 'Pause' : 'Play'}
        </button>
        <button onClick={() => setPos((p) => Math.min(events.length, p + 1))} disabled={finished} className="inline-flex h-9 items-center gap-1.5 rounded-lg border border-line px-3 text-sm disabled:opacity-40">
          <StepForward className="size-4" /> Step
        </button>
        <button onClick={() => reset()} className="inline-flex h-9 items-center gap-1.5 rounded-lg px-3 text-sm text-muted hover:text-text">
          <RotateCcw className="size-4" />
        </button>
      </div>
    </div>
  );
}

function Stage({ label, calls, active, result }: { label: string; calls: number; active: boolean; result?: string }) {
  return (
    <div className={clsx('rounded-xl border p-3 transition-all', active ? 'border-accent bg-accent-soft' : 'border-line bg-surface')}>
      <div className="font-mono text-xs font-semibold">{label}</div>
      <div className="mt-1 flex items-center justify-between text-xs text-muted">
        <span>calls: {calls}</span>
        <span className="font-mono text-text">{result}</span>
      </div>
    </div>
  );
}
