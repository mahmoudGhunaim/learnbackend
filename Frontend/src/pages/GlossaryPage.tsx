import clsx from 'clsx';
import { Search } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { PageHeader } from '../components/Layout';
import { useGlossary, useTerms } from '../components/TermProvider';
import { ErrorState, Spinner } from '../components/ui';
import { useI18n } from '../i18n/I18nProvider';
import type { GlossaryTerm } from '../lib/types';

type Category = 'all' | 'csharp' | 'dotnet' | 'aspnet';

/** Arabic-aware normalization for search: strips diacritics and unifies letter forms. */
export function normalize(text: string) {
  return text
    .toLowerCase()
    .replace(/[ً-ْٰـ]/g, '')
    .replace(/[أإآ]/g, 'ا')
    .replace(/ة/g, 'ه')
    .replace(/ى/g, 'ي')
    .replace(/[^\p{L}\p{N}\s#.]/gu, ' ')
    .replace(/\s+/g, ' ')
    .trim();
}

export function GlossaryPage() {
  const { t, lang } = useI18n();
  const { data, isLoading, error, refetch } = useGlossary();
  const { openTerm } = useTerms();
  const [params] = useSearchParams();
  const [query, setQuery] = useState('');
  const [category, setCategory] = useState<Category>('all');

  // /glossary?term=decimal opens that card directly.
  const termParam = params.get('term');
  useEffect(() => {
    if (termParam && data) openTerm(termParam);
  }, [termParam, data, openTerm]);

  const filtered = useMemo(() => {
    const q = normalize(query);
    return (data ?? []).filter(
      (term) =>
        (category === 'all' || term.category === category) &&
        (!q || normalize(`${term.en} ${term.ar} ${term.id}`).includes(q) || normalize(term.definition.ar).includes(q)),
    );
  }, [data, query, category]);

  if (isLoading) return <Spinner />;
  if (error) return <ErrorState error={error} onRetry={refetch} />;

  const categories: Category[] = ['all', 'csharp', 'dotnet', 'aspnet'];

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      <PageHeader title={t('glossary.title')} subtitle={t('glossary.subtitle')}>
        <span className="text-sm text-muted tabular-nums">
          {filtered.length} {t('glossary.count')}
        </span>
      </PageHeader>

      <div className="sticky top-14 z-30 -mx-4 mb-6 space-y-3 bg-bg/90 px-4 py-3 backdrop-blur-md">
        <div className="relative">
          <Search className="pointer-events-none absolute start-3.5 top-1/2 size-4 -translate-y-1/2 text-muted" />
          <input
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder={t('glossary.search')}
            className="h-11 w-full rounded-xl border border-line bg-surface ps-10 pe-4 outline-none focus:border-accent"
            type="search"
            dir="auto"
          />
        </div>
        <div className="flex flex-wrap gap-1.5">
          {categories.map((c) => (
            <button
              key={c}
              onClick={() => setCategory(c)}
              className={clsx('rounded-full px-3 py-1 text-sm', category === c ? 'bg-accent text-white' : 'bg-surface-2 text-muted hover:text-text')}
            >
              {t(`glossary.${c}`)}
            </button>
          ))}
        </div>
      </div>

      {filtered.length === 0 ? (
        <p className="py-16 text-center text-muted">{t('glossary.empty')}</p>
      ) : (
        <ul className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {filtered.map((term) => (
            <TermCard key={term.id} term={term} lang={lang} onOpen={() => openTerm(term.id)} />
          ))}
        </ul>
      )}
    </div>
  );
}

function TermCard({ term, lang, onOpen }: { term: GlossaryTerm; lang: 'ar' | 'en'; onOpen: () => void }) {
  return (
    <li>
      <button onClick={onOpen} className="h-full w-full rounded-2xl border border-line bg-surface p-4 text-start transition hover:border-accent hover:shadow-md">
        <p dir="ltr" className="text-start font-semibold text-text">
          {term.en}
        </p>
        <p className="text-accent" dir="rtl">
          {term.ar}
        </p>
        <p className="mt-2 line-clamp-3 text-sm leading-6 text-muted" dir={lang === 'en' ? 'ltr' : 'rtl'}>
          {lang === 'en' && term.definition.en ? term.definition.en : term.definition.ar}
        </p>
      </button>
    </li>
  );
}
