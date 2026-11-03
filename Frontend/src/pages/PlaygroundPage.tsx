import { useMutation } from '@tanstack/react-query';
import { Loader2, Play } from 'lucide-react';
import { lazy, Suspense, useEffect, useRef, useState } from 'react';
import { useLocation } from 'react-router-dom';
import type { CodeEditorHandle } from '../components/CodeEditor';
import { PageHeader } from '../components/Layout';
import { DiagnosticsList, OutcomeBanner } from '../components/RunResults';
import { Button, Spinner } from '../components/ui';
import { useI18n } from '../i18n/I18nProvider';
import { api } from '../lib/api';

const CodeEditor = lazy(() => import('../components/CodeEditor'));

const EXAMPLES: { label: string; code: string }[] = [
  {
    label: 'Hello, C#',
    code: `// Top-level statements: no class Program or Main needed
var name = "Sara";
var total = 1234.5m;
Console.WriteLine($"Hello {name}, you owe {total:N2} USD");

for (var i = 1; i <= 3; i++)
    Console.WriteLine($"{i} x 7 = {i * 7}");
`,
  },
  {
    label: 'LINQ',
    code: `var orders = new List<Order>
{
    new("Sara", 40m), new("Omar", 300m), new("Sara", 120m), new("Lina", 60m),
};

var totals = orders
    .GroupBy(o => o.Customer)
    .Select(g => new { Customer = g.Key, Total = g.Sum(o => o.Amount) })
    .OrderByDescending(x => x.Total);

foreach (var t in totals)
    Console.WriteLine($"{t.Customer,-6} {t.Total,8:N2}");

record Order(string Customer, decimal Amount);
`,
  },
  {
    label: 'async / await',
    code: `async Task<int> FetchAsync(string what, int ms)
{
    Console.WriteLine($"start {what}");
    await Task.Delay(ms);
    Console.WriteLine($"done  {what}");
    return ms;
}

var sw = System.Diagnostics.Stopwatch.StartNew();
var results = await Task.WhenAll(FetchAsync("weather", 300), FetchAsync("news", 200));
Console.WriteLine($"total {results.Sum()} ms of work in ~{sw.ElapsedMilliseconds} ms");
`,
  },
  {
    label: 'Records & patterns',
    code: `var shapes = new Shape[] { new Circle(1), new Rect(2, 3), new Rect(4, 4) };

foreach (var s in shapes)
{
    var description = s switch
    {
        Circle { R: > 5 } => "a big circle",
        Circle c => $"a circle with area {Math.PI * c.R * c.R:0.00}",
        Rect { W: var w, H: var h } when w == h => $"a square {w}x{h}",
        Rect r => $"a rectangle {r.W}x{r.H}",
        _ => "unknown",
    };
    Console.WriteLine(description);
}

abstract record Shape;
record Circle(double R) : Shape;
record Rect(double W, double H) : Shape;
`,
  },
];

const STORAGE = 'lb-playground';

export function PlaygroundPage() {
  const { t } = useI18n();
  const location = useLocation();
  const editorRef = useRef<CodeEditorHandle>(null);
  const [code, setCode] = useState<string>(() => {
    const fromLesson = (location.state as { code?: string } | null)?.code;
    if (fromLesson) return fromLesson;
    try {
      return localStorage.getItem(STORAGE) ?? EXAMPLES[0].code;
    } catch {
      return EXAMPLES[0].code;
    }
  });

  useEffect(() => {
    try {
      localStorage.setItem(STORAGE, code);
    } catch {
      /* private mode */
    }
  }, [code]);

  const run = useMutation({ mutationFn: () => api.runPlayground(code) });
  const execute = () => !run.isPending && code.trim() && run.mutate();
  const result = run.data;

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      <PageHeader title={t('playground.title')} subtitle={t('playground.subtitle')} />
      <div className="mb-3 flex flex-wrap items-center gap-2">
        <Button variant="primary" onClick={execute} disabled={run.isPending}>
          {run.isPending ? <Loader2 className="size-4 animate-spin" /> : <Play className="size-4" />}
          {t('playground.run')}
        </Button>
        <span className="ms-2 text-sm text-muted">{t('playground.examples')}:</span>
        {EXAMPLES.map((ex) => (
          <button key={ex.label} onClick={() => setCode(ex.code)} className="rounded-lg border border-line px-2.5 py-1 font-mono text-xs hover:border-accent hover:text-accent" dir="ltr">
            {ex.label}
          </button>
        ))}
      </div>
      <div className="grid gap-4 lg:grid-cols-[minmax(0,1.3fr)_minmax(0,1fr)]">
        <div className="h-[60vh] min-h-[360px] overflow-hidden rounded-2xl border border-line">
          <Suspense fallback={<Spinner />}>
            <CodeEditor ref={editorRef} value={code} onChange={setCode} diagnostics={result?.diagnostics} onRun={execute} />
          </Suspense>
        </div>
        <div className="space-y-3">
          <p className="text-sm font-semibold text-muted">{t('playground.output')}</p>
          {result && <OutcomeBanner outcome={result.outcome} elapsedMs={result.elapsedMs} />}
          <pre dir="ltr" className="min-h-40 overflow-auto rounded-2xl border border-line bg-code p-4 font-mono text-[13px] leading-6 whitespace-pre-wrap">
            {run.isPending ? '…' : result ? result.output || t('playground.empty') : ''}
            {result?.truncated && '\n… (output truncated)'}
          </pre>
          {result?.error && (
            <pre dir="ltr" className="overflow-auto rounded-xl bg-bad-soft p-3 font-mono text-xs whitespace-pre-wrap text-bad">
              {result.error}
            </pre>
          )}
          {result && <DiagnosticsList diagnostics={result.diagnostics} onJump={(l) => editorRef.current?.revealLine(l)} />}
          {run.error && <p className="text-sm text-bad">{String(run.error)}</p>}
        </div>
      </div>
    </div>
  );
}
