import { Fragment, type ReactNode } from 'react';
import { fixBidi } from '../lib/bidi';
import { TermLink } from './TermProvider';

/**
 * A deliberately tiny markdown subset used by lesson content:
 *   paragraphs (blank line), "- " bullet lists, "1. " numbered lists,
 *   **bold**, `code`, [[term-id]] and [[term-id|custom label]] glossary links.
 */
export function RichText({ text, className }: { text: string; className?: string }) {
  const paragraphs = text.split(/\n{2,}/);
  return (
    <div className={className}>
      {paragraphs.map((p, i) => {
        const lines = p.split('\n');
        if (lines.every((l) => /^\s*-\s/.test(l))) {
          return (
            <ul key={i} className="my-3 list-disc space-y-1.5 ps-6 marker:text-accent">
              {lines.map((l, j) => (
                <li key={j}>{inline(l.replace(/^\s*-\s/, ''))}</li>
              ))}
            </ul>
          );
        }
        if (lines.every((l) => /^\s*\d+\.\s/.test(l))) {
          return (
            <ol key={i} className="my-3 list-decimal space-y-1.5 ps-6 marker:font-semibold marker:text-accent">
              {lines.map((l, j) => (
                <li key={j}>{inline(l.replace(/^\s*\d+\.\s/, ''))}</li>
              ))}
            </ol>
          );
        }
        return (
          <p key={i} className="my-3 first:mt-0 last:mb-0">
            {lines.map((l, j) => (
              <Fragment key={j}>
                {j > 0 && <br />}
                {inline(l)}
              </Fragment>
            ))}
          </p>
        );
      })}
    </div>
  );
}

const TOKEN = /(`[^`]+`)|(\*\*.+?\*\*)|(\[\[([a-z0-9-]+)(?:\|([^\]]+))?\]\])/g;

export function inline(text: string): ReactNode[] {
  const out: ReactNode[] = [];
  let last = 0;
  let m: RegExpExecArray | null;
  TOKEN.lastIndex = 0;
  while ((m = TOKEN.exec(text)) !== null) {
    if (m.index > last) out.push(fixBidi(text.slice(last, m.index)));
    if (m[1]) {
      out.push(
        <code key={m.index} className="prose-code">
          {m[1].slice(1, -1)}
        </code>,
      );
    } else if (m[2]) {
      out.push(
        <strong key={m.index} className="font-semibold text-text">
          {inlineNested(m[2].slice(2, -2))}
        </strong>,
      );
    } else if (m[3]) {
      out.push(<TermLink key={m.index} id={m[4]} label={m[5]} />);
    }
    last = m.index + m[0].length;
  }
  if (last < text.length) out.push(fixBidi(text.slice(last)));
  return out;
}

// Bold text may itself contain code or term links. The shared regex's lastIndex is saved and restored around the nested call.
function inlineNested(text: string): ReactNode[] {
  const saved = TOKEN.lastIndex;
  const result = inline(text);
  TOKEN.lastIndex = saved;
  return result;
}
