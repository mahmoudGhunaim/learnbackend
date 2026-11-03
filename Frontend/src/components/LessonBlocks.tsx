import { useQuery } from '@tanstack/react-query';
import clsx from 'clsx';
import { AlertTriangle, CheckCircle2, FileCode2, Info, Lightbulb, Play, Sparkles, XCircle } from 'lucide-react';
import { lazy, Suspense, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useI18n } from '../i18n/I18nProvider';
import { api } from '../lib/api';
import type { Block } from '../lib/types';
import { CodeView } from './CodeView';
import { RichText, inline } from './RichText';
import { Spinner } from './ui';

const Visual = lazy(() => import('../visuals/Visual'));

export function LessonBlock({ block }: { block: Block }) {
  switch (block.type) {
    case 'text':
      return <LocalizedRich text={block} />;
    case 'heading':
      return <HeadingBlock block={block} />;
    case 'analogy':
      return <AnalogyBlock block={block} />;
    case 'callout':
      return <CalloutBlock block={block} />;
    case 'code':
      return <CodeBlock block={block} />;
    case 'compare':
      return <CompareBlock block={block} />;
    case 'annotated':
      return <AnnotatedBlock block={block} />;
    case 'source':
      return <SourceBlock block={block} />;
    case 'mistakes':
      return <MistakesBlock block={block} />;
    case 'table':
      return <TableBlock block={block} />;
    case 'visual':
      return (
        <Suspense fallback={<Spinner />}>
          <Visual name={block.name} caption={block.caption} />
        </Suspense>
      );
    default:
      return null;
  }
}

/** Renders Arabic or English text; in English mode falls back to Arabic with a small notice. */
function LocalizedRich({ text, className }: { text: { ar: string; en?: string | null }; className?: string }) {
  const { lang, t } = useI18n();
  const useEn = lang === 'en' && !!text.en;
  return (
    <div className={className}>
      {lang === 'en' && !text.en && <p className="mb-1 text-xs text-muted italic">{t('common.arabicOnly')}</p>}
      <div dir={useEn ? 'ltr' : 'rtl'} lang={useEn ? 'en' : 'ar'}>
        <RichText text={useEn ? text.en! : text.ar} />
      </div>
    </div>
  );
}

function HeadingBlock({ block }: { block: Extract<Block, { type: 'heading' }> }) {
  const { pick } = useI18n();
  return <h2 className="mt-12 mb-1 border-b border-line pb-2 text-xl font-bold sm:text-2xl">{pick(block)}</h2>;
}

function AnalogyBlock({ block }: { block: Extract<Block, { type: 'analogy' }> }) {
  const { t } = useI18n();
  return (
    <aside className="relative overflow-hidden rounded-2xl border border-warn/30 bg-warn-soft/60 p-5">
      <div className="mb-2 flex items-center gap-2 font-semibold text-warn">
        <Lightbulb className="size-5" />
        {t('lesson.analogy')}
      </div>
      <LocalizedRich text={block} />
    </aside>
  );
}

const CALLOUT = {
  tip: { icon: Sparkles, cls: 'border-ok/30 bg-ok-soft/60', title: 'text-ok' },
  warning: { icon: AlertTriangle, cls: 'border-bad/30 bg-bad-soft/60', title: 'text-bad' },
  info: { icon: Info, cls: 'border-info/30 bg-info-soft/60', title: 'text-info' },
  dotnet: { icon: Sparkles, cls: 'border-accent/30 bg-accent-soft/60', title: 'text-accent' },
} as const;

function CalloutBlock({ block }: { block: Extract<Block, { type: 'callout' }> }) {
  const { t } = useI18n();
  const style = CALLOUT[block.kind] ?? CALLOUT.info;
  const Icon = style.icon;
  return (
    <aside className={clsx('rounded-2xl border p-4', style.cls)}>
      <div className={clsx('mb-1.5 flex items-center gap-2 text-sm font-semibold', style.title)}>
        <Icon className="size-4" />
        {t(`callout.${block.kind}`)}
      </div>
      <LocalizedRich text={block} />
    </aside>
  );
}

function CodeBlock({ block }: { block: Extract<Block, { type: 'code' }> }) {
  const { t, pick } = useI18n();
  const navigate = useNavigate();
  return (
    <figure>
      <CodeView
        code={block.code}
        lang={block.lang}
        header={
          (block.title || block.runnable) && (
            <div className="flex items-center gap-3 border-b border-line bg-surface-2 px-4 py-2 text-sm" dir="auto">
              {block.title && <span className="font-medium">{pick(block.title)}</span>}
              {block.runnable && (
                <button
                  onClick={() => navigate('/playground', { state: { code: block.code } })}
                  className="ms-auto inline-flex items-center gap-1.5 rounded-lg bg-accent px-3 py-1 text-xs font-medium text-white hover:brightness-110"
                >
                  <Play className="size-3.5" />
                  {t('lesson.tryPlayground')}
                </button>
              )}
            </div>
          )
        }
      />
    </figure>
  );
}

const LANG_COLORS: Record<string, string> = {
  javascript: 'bg-yellow-400 text-black',
  typescript: 'bg-sky-500 text-white',
  php: 'bg-indigo-400 text-white',
  csharp: 'bg-accent text-white',
  bash: 'bg-zinc-600 text-white',
};

function CompareBlock({ block }: { block: Extract<Block, { type: 'compare' }> }) {
  const { pick } = useI18n();
  const [active, setActive] = useState(block.items.length - 1);
  return (
    <section className="rounded-2xl border border-line bg-surface p-4">
      <h3 className="mb-3 font-semibold">{pick(block.title)}</h3>
      {/* Mobile: tabs. Desktop: side by side. */}
      <div className="mb-3 flex gap-1.5 lg:hidden" dir="ltr">
        {block.items.map((item, i) => (
          <button
            key={i}
            onClick={() => setActive(i)}
            className={clsx('rounded-lg px-3 py-1 text-xs font-medium', i === active ? LANG_COLORS[item.lang] ?? 'bg-accent text-white' : 'bg-surface-2 text-muted')}
          >
            {item.label}
          </button>
        ))}
      </div>
      <div className={clsx('grid gap-3', block.items.length === 3 ? 'lg:grid-cols-3' : 'lg:grid-cols-2')} dir="ltr">
        {block.items.map((item, i) => (
          <div key={i} className={clsx(i !== active && 'hidden lg:block')}>
            <div className="mb-1.5 hidden lg:block">
              <span className={clsx('rounded-md px-2 py-0.5 text-xs font-semibold', LANG_COLORS[item.lang] ?? 'bg-surface-3')}>{item.label}</span>
            </div>
            <CodeView code={item.code} lang={item.lang} lineNumbers={false} className="h-full" />
          </div>
        ))}
      </div>
      {block.note && <LocalizedRich text={block.note} className="mt-3 text-sm text-muted" />}
    </section>
  );
}

function AnnotatedBlock({ block }: { block: Extract<Block, { type: 'annotated' }> }) {
  const { t, pick } = useI18n();
  const noteLines = Object.keys(block.notes).map(Number);
  const [active, setActive] = useState<number | null>(noteLines[0] ?? null);
  const [pinned, setPinned] = useState(false);
  const note = active !== null ? block.notes[String(active)] : undefined;

  return (
    <section className="overflow-hidden rounded-2xl border border-line bg-surface">
      <div className="flex flex-wrap items-center gap-2 border-b border-line px-4 py-2.5">
        <FileCode2 className="size-4 text-accent" />
        <span className="font-semibold">{pick(block.title)}</span>
        <span className="ms-auto text-xs text-muted">{t('lesson.hoverHint')}</span>
      </div>
      <div className="grid lg:grid-cols-[minmax(0,1.35fr)_minmax(0,1fr)]">
        <CodeView
          code={block.code}
          lang={block.lang}
          className="rounded-none border-0"
          annotatedLines={new Set(noteLines)}
          activeLine={active}
          onLineEnter={(n) => !pinned && n !== null && block.notes[String(n)] && setActive(n)}
          onLineClick={(n) => {
            if (block.notes[String(n)]) {
              setActive(n);
              setPinned(true);
            }
          }}
        />
        <div className="border-t border-line bg-surface-2/50 p-4 lg:border-s lg:border-t-0">
          {note && active !== null ? (
            <div key={active} className="animate-fade-up lg:sticky lg:top-20">
              <div className="mb-2 text-xs font-semibold text-accent">
                {t('lesson.line')} {active}
              </div>
              <LocalizedRich text={note} className="leading-8" />
            </div>
          ) : null}
          <div className="mt-4 flex flex-wrap gap-1.5" dir="ltr">
            {noteLines.map((n) => (
              <button
                key={n}
                onClick={() => {
                  setActive(n);
                  setPinned(true);
                }}
                className={clsx('size-7 rounded-md text-xs font-mono', n === active ? 'bg-accent text-white' : 'bg-surface-3 text-muted hover:text-text')}
              >
                {n}
              </button>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}

function SourceBlock({ block }: { block: Extract<Block, { type: 'source' }> }) {
  const { t } = useI18n();
  const [full, setFull] = useState(false);
  const { data, isLoading, error } = useQuery({
    queryKey: ['source', block.path, block.find, full],
    queryFn: () => (full ? api.source(block.path) : api.source(block.path, block.find, block.context)),
    staleTime: 60_000,
  });
  const markedLine = full ? undefined : data?.line;

  return (
    <section className="overflow-hidden rounded-2xl border border-accent/30 bg-accent-soft/30">
      <div className="flex flex-wrap items-center gap-2 px-4 py-3">
        <span className="inline-flex items-center gap-1.5 rounded-full bg-accent px-2.5 py-0.5 text-xs font-semibold text-white">
          <FileCode2 className="size-3.5" />
          {t('lesson.seeInApp')}
        </span>
        <code dir="ltr" className="truncate text-xs text-muted">
          /{block.path}
          {data && !full && `:${data.line}`}
        </code>
        <button onClick={() => setFull((f) => !f)} className="ms-auto text-xs text-accent hover:underline">
          {full ? `${t('lesson.line')} ${data?.line ?? ''}` : t('lesson.openFile')}
        </button>
      </div>
      <LocalizedRich text={block.note} className="px-4 pb-3 text-sm" />
      {isLoading && <div className="px-4 pb-4 text-sm text-muted">{t('common.loading')}</div>}
      {error && <div className="px-4 pb-4 text-sm text-bad">{String(error)}</div>}
      {data && (
        <CodeView
          code={data.lines.join('\n')}
          lang={data.language}
          startLine={data.startLine}
          markedLine={markedLine}
          className="rounded-none border-x-0 border-b-0"
          maxHeight={full ? 520 : undefined}
        />
      )}
    </section>
  );
}

function MistakesBlock({ block }: { block: Extract<Block, { type: 'mistakes' }> }) {
  const { t, pick } = useI18n();
  return (
    <section>
      <h2 className="mt-12 mb-4 flex items-center gap-2 border-b border-line pb-2 text-xl font-bold">
        <AlertTriangle className="size-5 text-bad" />
        {t('lesson.mistakes')}
      </h2>
      <div className="space-y-4">
        {block.items.map((item, i) => (
          <div key={i} className="rounded-2xl border border-line bg-surface p-4">
            <h3 className="mb-2 font-semibold">
              {i + 1}. {pick(item.title)}
            </h3>
            <LocalizedRich text={item} className="mb-3 text-[15px] text-muted" />
            <div className="grid gap-2 md:grid-cols-2" dir="ltr">
              {item.wrong && (
                <div>
                  <div className="mb-1 flex items-center gap-1 text-xs font-semibold text-bad">
                    <XCircle className="size-3.5" /> {t('lesson.wrong')}
                  </div>
                  <CodeView code={item.wrong} lang="csharp" lineNumbers={false} className="border-bad/40" />
                </div>
              )}
              {item.right && (
                <div>
                  <div className="mb-1 flex items-center gap-1 text-xs font-semibold text-ok">
                    <CheckCircle2 className="size-3.5" /> {t('lesson.right')}
                  </div>
                  <CodeView code={item.right} lang="csharp" lineNumbers={false} className="border-ok/40" />
                </div>
              )}
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

function TableBlock({ block }: { block: Extract<Block, { type: 'table' }> }) {
  const { pick } = useI18n();
  return (
    <div className="overflow-x-auto rounded-2xl border border-line">
      <table className="w-full text-start text-sm">
        <thead className="bg-surface-2">
          <tr>
            {block.headers.map((h, i) => (
              <th key={i} className="px-4 py-2.5 text-start font-semibold whitespace-nowrap">
                {inline(pick(h))}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {block.rows.map((row, i) => (
            <tr key={i} className="border-t border-line bg-surface">
              {row.map((cell, j) => (
                <td key={j} className={clsx('px-4 py-2.5 align-top', j === 0 && 'font-medium')}>
                  {inline(cell)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
