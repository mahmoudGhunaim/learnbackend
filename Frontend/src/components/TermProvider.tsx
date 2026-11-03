import { useQuery } from '@tanstack/react-query';
import clsx from 'clsx';
import { BookOpen, X } from 'lucide-react';
import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { useI18n } from '../i18n/I18nProvider';
import { api } from '../lib/api';
import type { GlossaryTerm } from '../lib/types';
import { CodeView } from './CodeView';
import { Badge } from './ui';

interface TermContextValue {
  terms: Map<string, GlossaryTerm>;
  openTerm: (id: string) => void;
}

const TermContext = createContext<TermContextValue | null>(null);

export function useGlossary() {
  return useQuery({ queryKey: ['glossary'], queryFn: api.glossary, staleTime: Infinity });
}

export function useLessonTitles() {
  const { data } = useQuery({ queryKey: ['curriculum'], queryFn: api.curriculum });
  return useMemo(() => {
    const map = new Map<string, { title: { ar: string; en?: string | null }; phase: number }>();
    data?.phases.forEach((p) => p.lessons.forEach((l) => map.set(l.id, { title: l.title, phase: p.id })));
    return map;
  }, [data]);
}

export function TermProvider({ children }: { children: ReactNode }) {
  const { data } = useGlossary();
  const [openId, setOpenId] = useState<string | null>(null);
  const terms = useMemo(() => new Map((data ?? []).map((t) => [t.id, t])), [data]);
  const openTerm = useCallback((id: string) => setOpenId(id), []);
  const value = useMemo(() => ({ terms, openTerm }), [terms, openTerm]);

  return (
    <TermContext.Provider value={value}>
      {children}
      {openId && terms.get(openId) && <TermModal term={terms.get(openId)!} onClose={() => setOpenId(null)} onOpen={setOpenId} />}
    </TermContext.Provider>
  );
}

export function useTerms() {
  const ctx = useContext(TermContext);
  if (!ctx) throw new Error('useTerms must be used inside <TermProvider>');
  return ctx;
}

/** How a term is displayed inline: "العربية (English)" in Arabic UI, "English" in English UI. */
export function termLabel(term: GlossaryTerm, lang: 'ar' | 'en') {
  if (lang === 'en') return term.en;
  return /[A-Za-z]/.test(term.ar) ? term.ar : `${term.ar} (${term.en})`;
}

export function TermLink({ id, label }: { id: string; label?: string }) {
  const { terms, openTerm } = useTerms();
  const { lang } = useI18n();
  const term = terms.get(id);
  const text = label ?? (term ? termLabel(term, lang) : id);
  return (
    <button
      type="button"
      onClick={() => openTerm(id)}
      className="inline cursor-pointer rounded-sm font-medium text-accent underline decoration-accent/40 decoration-dotted underline-offset-4 transition hover:decoration-solid"
    >
      {text}
    </button>
  );
}

export function TermChip({ id, className }: { id: string; className?: string }) {
  const { terms, openTerm } = useTerms();
  const { lang } = useI18n();
  const term = terms.get(id);
  if (!term) return null;
  return (
    <button
      type="button"
      onClick={() => openTerm(id)}
      className={clsx(
        'inline-flex items-center gap-1.5 rounded-full border border-line bg-surface-2 px-3 py-1 text-sm transition hover:border-accent hover:text-accent',
        className,
      )}
    >
      <span>{lang === 'ar' ? term.ar : term.en}</span>
      {lang === 'ar' && !/[A-Za-z]/.test(term.ar) && (
        <bdi dir="ltr" className="text-xs text-muted">
          {term.en}
        </bdi>
      )}
    </button>
  );
}

function TermModal({ term, onClose, onOpen }: { term: GlossaryTerm; onClose: () => void; onOpen: (id: string) => void }) {
  const { t, lang, pick } = useI18n();
  const lessons = useLessonTitles();
  const closeRef = useRef<HTMLButtonElement>(null);
  const lesson = term.lesson ? lessons.get(term.lesson) : undefined;

  useEffect(() => {
    closeRef.current?.focus();
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onClose]);

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-0 backdrop-blur-sm sm:items-center sm:p-4" onClick={onClose}>
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby="term-title"
        onClick={(e) => e.stopPropagation()}
        className="animate-fade-up max-h-[88dvh] w-full max-w-xl overflow-y-auto rounded-t-3xl border border-line bg-surface p-6 shadow-2xl sm:rounded-3xl"
      >
        <div className="mb-4 flex items-start justify-between gap-4">
          <div className="min-w-0">
            <h2 id="term-title" dir="ltr" className="text-2xl font-bold text-text">
              {term.en}
            </h2>
            <p className="mt-1 text-lg text-accent" dir="rtl" lang="ar">
              {term.ar}
            </p>
          </div>
          <button ref={closeRef} onClick={onClose} className="rounded-lg p-1.5 text-muted hover:bg-surface-2 hover:text-text" aria-label={t('common.close')}>
            <X className="size-5" />
          </button>
        </div>

        {term.category && (
          <Badge tone="accent" className="mb-4">
            {t(`glossary.${term.category}`)}
          </Badge>
        )}

        <p className="leading-8" dir="rtl" lang="ar">
          {term.definition.ar}
        </p>
        {term.definition.en && (
          <p className="mt-2 text-sm leading-7 text-muted" dir="ltr" lang="en">
            {term.definition.en}
          </p>
        )}

        {term.snippet && <CodeView code={term.snippet} lang="csharp" lineNumbers={false} className="mt-4" />}

        {term.related.length > 0 && (
          <div className="mt-5">
            <p className="mb-2 text-sm font-medium text-muted">{t('glossary.related')}</p>
            <div className="flex flex-wrap gap-2">
              {term.related.map((r) => (
                <RelatedChip key={r} id={r} onOpen={onOpen} />
              ))}
            </div>
          </div>
        )}

        <div className="mt-6 flex flex-wrap items-center gap-3 border-t border-line pt-4 text-sm">
          {lesson && term.lesson && (
            <Link to={`/lessons/${term.lesson}`} onClick={onClose} className="inline-flex items-center gap-1.5 text-accent hover:underline">
              <BookOpen className="size-4" />
              {t('glossary.lesson')}: {pick(lesson.title)}
            </Link>
          )}
          <Link to={`/glossary?term=${term.id}`} onClick={onClose} className="ms-auto text-muted hover:text-text">
            {t('term.open')} {lang === 'ar' ? '←' : '→'}
          </Link>
        </div>
      </div>
    </div>
  );
}

function RelatedChip({ id, onOpen }: { id: string; onOpen: (id: string) => void }) {
  const { terms } = useTerms();
  const { lang } = useI18n();
  const term = terms.get(id);
  if (!term) return null;
  return (
    <button onClick={() => onOpen(id)} className="rounded-full border border-line bg-surface-2 px-3 py-1 text-sm hover:border-accent hover:text-accent">
      {lang === 'ar' ? term.ar : term.en}
    </button>
  );
}
