import clsx from 'clsx';
import { Play, RotateCcw } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useI18n } from '../i18n/I18nProvider';

type Mode = 'sequential' | 'whenall' | 'blocking';
type Task = { label: string; start: number; end: number };

const DURATIONS = [
  { label: 'GetTemperatureAsync("Amman")', ms: 300 },
  { label: 'GetTemperatureAsync("Cairo")', ms: 450 },
  { label: 'GetHeadlinesAsync()', ms: 250 },
];

function schedule(mode: Mode): Task[] {
  if (mode === 'whenall') return DURATIONS.map((d) => ({ label: d.label, start: 0, end: d.ms }));
  let t = 0;
  return DURATIONS.map((d) => {
    const task = { label: d.label, start: t, end: t + d.ms };
    t += d.ms;
    return task;
  });
}

/** Periods when the request's thread is actually doing work (busy) instead of being back in the pool. */
function busySegments(mode: Mode, tasks: Task[]): [number, number][] {
  if (mode === 'blocking') return [[0, tasks[tasks.length - 1].end]];
  return tasks.flatMap((task) => [
    [task.start, task.start + 15],
    [task.end, task.end + 15],
  ]) as [number, number][];
}

const CODE: Record<Mode, string> = {
  sequential: 'foreach (var c in cities)\n    temps.Add(await GetTemperatureAsync(c));\nvar news = await GetHeadlinesAsync();',
  whenall: 'var t1 = GetTemperatureAsync("Amman");\nvar t2 = GetTemperatureAsync("Cairo");\nvar t3 = GetHeadlinesAsync();\nawait Task.WhenAll(t1, t2, t3);',
  blocking: 'var a = GetTemperatureAsync("Amman").Result;   // blocks!\nvar b = GetTemperatureAsync("Cairo").Result;\nvar n = GetHeadlinesAsync().Result;',
};

const SLOWDOWN = 3; // animate 3× slower than real time so you can follow it

export function AsyncTimeline() {
  const { lang } = useI18n();
  const [mode, setMode] = useState<Mode>('sequential');
  const [time, setTime] = useState(0);
  const [running, setRunning] = useState(false);
  const raf = useRef(0);
  const tasks = schedule(mode);
  const total = Math.max(...tasks.map((t) => t.end));
  const scaleMax = 1000;
  const busy = busySegments(mode, tasks);
  const threadBusyNow = busy.some(([s, e]) => time >= s && time < e);

  useEffect(() => {
    if (!running) return;
    const startedAt = performance.now() - time * SLOWDOWN;
    const tick = (now: number) => {
      const t = Math.min(total, (now - startedAt) / SLOWDOWN);
      setTime(t);
      if (t < total) raf.current = requestAnimationFrame(tick);
      else setRunning(false);
    };
    raf.current = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(raf.current);
  }, [running, total]);

  const choose = (m: Mode) => {
    setMode(m);
    setTime(0);
    setRunning(false);
  };

  const pct = (ms: number) => `${(ms / scaleMax) * 100}%`;
  const labels: Record<Mode, string> = {
    sequential: lang === 'ar' ? 'await متسلسل' : 'Sequential await',
    whenall: 'Task.WhenAll',
    blocking: lang === 'ar' ? '.Result (حجز)' : '.Result (blocking)',
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap gap-2">
        {(Object.keys(labels) as Mode[]).map((m) => (
          <button
            key={m}
            onClick={() => choose(m)}
            className={clsx('rounded-lg border px-3 py-1.5 text-sm', mode === m ? 'border-accent bg-accent-soft font-medium text-accent' : 'border-line text-muted')}
          >
            {labels[m]}
          </button>
        ))}
      </div>

      <pre dir="ltr" className="rounded-xl border border-line bg-code p-3 font-mono text-[13px] leading-6">
        {CODE[mode]}
      </pre>

      <div dir="ltr" className="space-y-2">
        {tasks.map((task) => {
          const progress = Math.max(0, Math.min(1, (time - task.start) / (task.end - task.start)));
          return (
            <div key={task.label} className="grid grid-cols-[10rem_1fr] items-center gap-2 sm:grid-cols-[15rem_1fr]">
              <span className="truncate font-mono text-xs text-muted">{task.label}</span>
              <div className="relative h-7 rounded-md bg-surface-2">
                <div className="absolute inset-y-0 rounded-md border border-info/40 bg-info-soft" style={{ left: pct(task.start), width: pct(task.end - task.start) }}>
                  <div className="h-full rounded-md bg-info/60" style={{ width: `${progress * 100}%` }} />
                </div>
              </div>
            </div>
          );
        })}

        <div className="grid grid-cols-[10rem_1fr] items-center gap-2 pt-2 sm:grid-cols-[15rem_1fr]">
          <span className={clsx('text-xs font-semibold', threadBusyNow ? 'text-bad' : 'text-ok')}>
            Thread #7 · {threadBusyNow ? 'busy' : time > 0 && time < total ? 'free → serving others' : 'idle'}
          </span>
          <div className="relative h-7 rounded-md bg-ok-soft">
            {busy.map(([s, e], i) => (
              <div key={i} className="absolute inset-y-0 bg-bad/70" style={{ left: pct(s), width: pct(Math.max(e - s, 6)) }} />
            ))}
            <div className="absolute inset-y-0 w-0.5 bg-text" style={{ left: pct(time) }} />
          </div>
        </div>

        <div className="grid grid-cols-[10rem_1fr] gap-2 sm:grid-cols-[15rem_1fr]">
          <span />
          <div className="flex justify-between font-mono text-[11px] text-muted">
            <span>0 ms</span>
            <span>500 ms</span>
            <span>1000 ms</span>
          </div>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-3">
        <button
          onClick={() => {
            if (time >= total) setTime(0);
            setRunning(true);
          }}
          disabled={running}
          className="inline-flex h-9 items-center gap-1.5 rounded-lg bg-accent px-4 text-sm font-medium text-white disabled:opacity-40"
        >
          <Play className="size-4" /> {lang === 'ar' ? 'تشغيل' : 'Run'}
        </button>
        <button onClick={() => choose(mode)} className="inline-flex h-9 items-center rounded-lg px-3 text-muted hover:text-text">
          <RotateCcw className="size-4" />
        </button>
        <span className="font-mono text-sm tabular-nums" dir="ltr">
          {Math.round(time)} / {total} ms
        </span>
      </div>

      <p className="rounded-xl bg-surface-2 px-4 py-3 text-[15px]">
        {mode === 'sequential' &&
          (lang === 'ar'
            ? 'كل طلب ينتظر انتهاء الذي قبله: المجموع ≈ 1000ms. لكن الـ Thread (الشريط الأخضر) حرّ معظم الوقت ويخدم مستخدمين آخرين.'
            : 'Each call waits for the previous one: ≈1000 ms total. But the thread (green) is free most of the time, serving other users.')}
        {mode === 'whenall' &&
          (lang === 'ar'
            ? 'كل الطلبات تبدأ معاً: المجموع ≈ أطول طلب (450ms) بدلاً من مجموعها. والـ Thread حرّ أيضاً.'
            : 'All calls start together: total ≈ the slowest (450 ms), not the sum. The thread is still free.')}
        {mode === 'blocking' &&
          (lang === 'ar'
            ? 'نفس زمن الحالة المتسلسلة، لكن الـ Thread محجوز طوال 1000ms دون أن يفعل شيئاً. تحت الضغط تنفد الـ Threads ويتجمد الخادم (Thread starvation).'
            : 'Same time as sequential, but the thread is held for 1000 ms doing nothing. Under load the pool runs dry and the server stalls (thread starvation).')}
      </p>
    </div>
  );
}
