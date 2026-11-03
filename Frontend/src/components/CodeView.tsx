import clsx from 'clsx';
import { Check, Copy } from 'lucide-react';
import { useMemo, useState, type ReactNode } from 'react';
import { useI18n } from '../i18n/I18nProvider';
import { highlightLines } from '../lib/highlight';

interface CodeViewProps {
  code: string;
  lang: string;
  startLine?: number;
  /** A line to mark as "the one this is about" (absolute line number). */
  markedLine?: number;
  /** Lines that have an explanation (annotated code). */
  annotatedLines?: Set<number>;
  activeLine?: number | null;
  onLineEnter?: (line: number | null) => void;
  onLineClick?: (line: number) => void;
  lineNumbers?: boolean;
  className?: string;
  header?: ReactNode;
  maxHeight?: number;
}

export function CodeView({
  code,
  lang,
  startLine = 1,
  markedLine,
  annotatedLines,
  activeLine,
  onLineEnter,
  onLineClick,
  lineNumbers = true,
  className,
  header,
  maxHeight,
}: CodeViewProps) {
  const lines = useMemo(() => highlightLines(code, lang), [code, lang]);
  const gutter = String(startLine + lines.length - 1).length;

  return (
    <div dir="ltr" className={clsx('overflow-hidden rounded-xl border border-line bg-code', className)}>
      {header}
      <div className="relative">
        <CopyButton text={code} />
        <pre className="hljs overflow-auto py-3 text-[13px] leading-6" style={{ maxHeight }} onMouseLeave={() => onLineEnter?.(null)}>
          {lines.map((html, i) => {
            const n = startLine + i;
            const annotated = annotatedLines?.has(n);
            return (
              <div
                key={i}
                onMouseEnter={() => onLineEnter?.(n)}
                onClick={() => onLineClick?.(n)}
                className={clsx(
                  'flex min-w-max pe-10',
                  annotated && 'cursor-help',
                  n === markedLine && 'bg-accent-soft/80',
                  activeLine === n && 'bg-accent-soft',
                  annotated && activeLine !== n && 'hover:bg-surface-2',
                )}
              >
                {lineNumbers && (
                  <span
                    className={clsx(
                      'sticky left-0 shrink-0 select-none bg-code ps-3 pe-3 text-end text-muted/70',
                      (n === markedLine || activeLine === n) && 'text-accent',
                    )}
                    style={{ width: `${gutter + 2.5}ch` }}
                  >
                    {annotated ? <span className="text-accent">●</span> : n}
                  </span>
                )}
                <code className={clsx('whitespace-pre', !lineNumbers && 'ps-4')} dangerouslySetInnerHTML={{ __html: html || ' ' }} />
              </div>
            );
          })}
        </pre>
      </div>
    </div>
  );
}

export function CopyButton({ text }: { text: string }) {
  const { t } = useI18n();
  const [copied, setCopied] = useState(false);
  return (
    <button
      type="button"
      title={copied ? t('common.copied') : t('common.copy')}
      aria-label={t('common.copy')}
      onClick={async () => {
        try {
          await navigator.clipboard.writeText(text);
          setCopied(true);
          setTimeout(() => setCopied(false), 1500);
        } catch {
          /* clipboard blocked */
        }
      }}
      className="absolute top-2 right-2 z-10 rounded-lg border border-line bg-surface/90 p-1.5 text-muted opacity-70 transition hover:text-text hover:opacity-100"
    >
      {copied ? <Check className="size-3.5 text-ok" /> : <Copy className="size-3.5" />}
    </button>
  );
}
