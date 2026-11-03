import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { CheckCircle2, PartyPopper, RotateCw, XCircle } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { CodeView } from '../components/CodeView';
import { PageHeader } from '../components/Layout';
import { Badge, Button, Card, ErrorState, ProgressBar, Spinner } from '../components/ui';
import { useI18n } from '../i18n/I18nProvider';
import { api } from '../lib/api';
import type { Flashcard, GlossaryTerm } from '../lib/types';
import { normalize } from './GlossaryPage';

type Mode = 'srs' | 'test';

export function FlashcardsPage() {
  const { t } = useI18n();
  const [mode, setMode] = useState<Mode>('srs');
  const [session, setSession] = useState(0);
  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['flashcards', session],
    queryFn: () => api.flashcards(20),
    staleTime: Infinity,
  });

  return (
    <div className="mx-auto max-w-2xl px-4 py-8">
      <PageHeader title={t('flash.title')} />
      <div className="mb-6 grid grid-cols-2 gap-1 rounded-xl bg-surface-2 p-1">
        {(['srs', 'test'] as Mode[]).map((m) => (
          <button
            key={m}
            onClick={() => setMode(m)}
            className={clsx('rounded-lg py-2 text-sm font-medium transition', mode === m ? 'bg-surface text-text shadow-sm' : 'text-muted')}
          >
            {t(m === 'srs' ? 'flash.srs' : 'flash.test')}
          </button>
        ))}
      </div>
      {isLoading ? (
        <Spinner />
      ) : error ? (
        <ErrorState error={error} onRetry={refetch} />
      ) : !data || data.length === 0 ? (
        <Card className="p-10 text-center text-muted">{t('flash.empty')}</Card>
      ) : mode === 'srs' ? (
        <SpacedRepetition key={`srs-${session}`} cards={data} onRestart={() => setSession((s) => s + 1)} />
      ) : (
        <TermTest key={`test-${session}`} cards={data} onRestart={() => setSession((s) => s + 1)} />
      )}
    </div>
  );
}

function useReview() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ termId, quality }: { termId: string; quality: number }) => api.review(termId, quality),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
  });
}

function SessionDone({ onRestart, correct, total }: { onRestart: () => void; correct: number; total: number }) {
  const { t } = useI18n();
  return (
    <Card className="animate-fade-up p-10 text-center">
      <PartyPopper className="mx-auto mb-4 size-12 text-accent" />
      <p className="text-xl font-bold">{t('flash.done')}</p>
      <p className="mt-2 text-3xl font-bold tabular-nums" dir="ltr">
        {correct}/{total}
      </p>
      <p className="mt-2 text-muted">{t('flash.doneHint')}</p>
      <Button variant="primary" className="mt-6" onClick={onRestart}>
        <RotateCw className="size-4" /> {t('flash.again2')}
      </Button>
    </Card>
  );
}

function SpacedRepetition({ cards, onRestart }: { cards: Flashcard[]; onRestart: () => void }) {
  const { t } = useI18n();
  const [index, setIndex] = useState(0);
  const [flipped, setFlipped] = useState(false);
  const [correct, setCorrect] = useState(0);
  const review = useReview();
  const card = cards[index];

  if (!card) return <SessionDone onRestart={onRestart} correct={correct} total={cards.length} />;

  const rate = (quality: number) => {
    review.mutate({ termId: card.term.id, quality });
    if (quality >= 3) setCorrect((c) => c + 1);
    setFlipped(false);
    setIndex((i) => i + 1);
  };

  const buttons = [
    { q: 1, label: t('flash.again'), cls: 'bg-bad-soft text-bad hover:bg-bad hover:text-white' },
    { q: 3, label: t('flash.hard'), cls: 'bg-warn-soft text-warn hover:bg-warn hover:text-white' },
    { q: 4, label: t('flash.good'), cls: 'bg-ok-soft text-ok hover:bg-ok hover:text-white' },
    { q: 5, label: t('flash.easy'), cls: 'bg-info-soft text-info hover:bg-info hover:text-white' },
  ];

  return (
    <div>
      <p className="mb-3 text-sm text-muted">{t('flash.srsHint')}</p>
      <div className="mb-4 flex items-center gap-3">
        <ProgressBar value={(index / cards.length) * 100} className="flex-1" />
        <span className="text-xs text-muted tabular-nums" dir="ltr">
          {index + 1} / {cards.length}
        </span>
      </div>

      <button
        onClick={() => setFlipped((f) => !f)}
        className="relative block min-h-72 w-full rounded-3xl border border-line bg-surface p-6 text-center shadow-sm transition hover:border-accent/60"
      >
        {card.isNew && (
          <Badge tone="accent" className="absolute top-4 start-4">
            {t('flash.new')}
          </Badge>
        )}
        {!flipped ? (
          <div className="flex min-h-60 flex-col items-center justify-center gap-3">
            <p dir="ltr" className="text-3xl font-bold">
              {card.term.en}
            </p>
            <p className="text-sm text-muted">{t('flash.flip')}</p>
          </div>
        ) : (
          <CardBack term={card.term} />
        )}
      </button>

      <div className={clsx('mt-4 grid grid-cols-4 gap-2 transition', !flipped && 'pointer-events-none opacity-30')}>
        {buttons.map((b) => (
          <button key={b.q} onClick={() => rate(b.q)} className={clsx('h-12 rounded-xl text-sm font-semibold transition', b.cls)}>
            {b.label}
          </button>
        ))}
      </div>
    </div>
  );
}

function CardBack({ term }: { term: GlossaryTerm }) {
  return (
    <div className="animate-fade-up space-y-3 text-start">
      <p dir="ltr" className="text-center text-lg font-semibold text-muted">
        {term.en}
      </p>
      <p className="text-center text-2xl font-bold text-accent" dir="rtl">
        {term.ar}
      </p>
      <p className="leading-8" dir="rtl">
        {term.definition.ar}
      </p>
      {term.definition.en && (
        <p className="text-sm text-muted" dir="ltr">
          {term.definition.en}
        </p>
      )}
      {term.snippet && <CodeView code={term.snippet} lang="csharp" lineNumbers={false} />}
    </div>
  );
}

/** Strips the "(English)" part and English words from an Arabic term so only the Arabic meaning is compared. */
function arabicCore(ar: string) {
  return normalize(ar.replace(/\(.*?\)/g, '').replace(/[A-Za-z#.<>]+/g, ' '));
}

function judge(answer: string, term: GlossaryTerm, direction: 'en-ar' | 'ar-en') {
  const a = normalize(answer);
  if (!a) return false;
  const expected = direction === 'en-ar' ? arabicCore(term.ar) : normalize(term.en);
  if (!expected) return false;
  if (expected.includes(a) || a.includes(expected)) return true;
  // Word overlap, ignoring the Arabic definite article "ال".
  const words = (s: string) => new Set(s.split(' ').map((w) => w.replace(/^ال/, '')).filter((w) => w.length > 1));
  const exp = words(expected);
  const got = words(a);
  const overlap = [...got].filter((w) => exp.has(w)).length;
  return exp.size > 0 && overlap / exp.size >= 0.5;
}

function TermTest({ cards, onRestart }: { cards: Flashcard[]; onRestart: () => void }) {
  const { t } = useI18n();
  const [direction, setDirection] = useState<'en-ar' | 'ar-en'>('en-ar');
  const [index, setIndex] = useState(0);
  const [answer, setAnswer] = useState('');
  const [checked, setChecked] = useState(false);
  const [correct, setCorrect] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const review = useReview();
  const card = cards[index];
  const looksRight = useMemo(() => (card && checked ? judge(answer, card.term, direction) : false), [card, checked, answer, direction]);

  useEffect(() => inputRef.current?.focus(), [index]);

  if (!card) return <SessionDone onRestart={onRestart} correct={correct} total={cards.length} />;

  const finish = (wasRight: boolean) => {
    review.mutate({ termId: card.term.id, quality: wasRight ? 4 : 1 });
    if (wasRight) setCorrect((c) => c + 1);
    setAnswer('');
    setChecked(false);
    setIndex((i) => i + 1);
  };

  return (
    <div>
      <div className="mb-4 flex flex-wrap items-center gap-2">
        {(['en-ar', 'ar-en'] as const).map((d) => (
          <button
            key={d}
            onClick={() => setDirection(d)}
            className={clsx('rounded-lg border px-3 py-1 text-sm', direction === d ? 'border-accent bg-accent-soft text-accent' : 'border-line text-muted')}
          >
            {t(d === 'en-ar' ? 'flash.directionEnAr' : 'flash.directionArEn')}
          </button>
        ))}
        <span className="ms-auto text-xs text-muted tabular-nums" dir="ltr">
          {index + 1} / {cards.length}
        </span>
      </div>

      <Card className="p-6">
        <p className="mb-5 text-center text-3xl font-bold" dir={direction === 'en-ar' ? 'ltr' : 'rtl'}>
          {direction === 'en-ar' ? card.term.en : card.term.ar}
        </p>
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (!checked && answer.trim()) setChecked(true);
          }}
          className="flex gap-2"
        >
          <input
            ref={inputRef}
            value={answer}
            onChange={(e) => setAnswer(e.target.value)}
            disabled={checked}
            dir={direction === 'en-ar' ? 'rtl' : 'ltr'}
            placeholder={t(direction === 'en-ar' ? 'flash.writeMeaning' : 'flash.writeTerm')}
            className="h-12 flex-1 rounded-xl border border-line bg-surface-2 px-4 outline-none focus:border-accent"
          />
          {!checked && (
            <Button variant="primary" size="lg" type="submit" disabled={!answer.trim()}>
              {t('flash.check')}
            </Button>
          )}
        </form>

        {checked && (
          <div className="animate-fade-up mt-5 space-y-4">
            <div className={clsx('flex items-center gap-2 rounded-xl px-4 py-2.5 font-medium', looksRight ? 'bg-ok-soft text-ok' : 'bg-warn-soft text-warn')}>
              {looksRight ? <CheckCircle2 className="size-5" /> : <XCircle className="size-5" />}
              {looksRight ? t('flash.looksRight') : t('flash.looksWrong')}
            </div>
            <div className="rounded-xl bg-surface-2 p-4">
              <p className="text-xs text-muted">{t('flash.correctAnswer')}</p>
              <p className="text-lg font-semibold" dir="ltr">
                {card.term.en}
              </p>
              <p className="text-lg font-semibold text-accent" dir="rtl">
                {card.term.ar}
              </p>
              <p className="mt-2 text-sm leading-7" dir="rtl">
                {card.term.definition.ar}
              </p>
            </div>
            <div className="grid grid-cols-2 gap-2">
              <Button variant={looksRight ? 'success' : 'secondary'} size="lg" onClick={() => finish(true)}>
                {t('flash.iWasRight')}
              </Button>
              <Button variant={looksRight ? 'secondary' : 'danger'} size="lg" onClick={() => finish(false)}>
                {t('flash.iWasWrong')}
              </Button>
            </div>
          </div>
        )}
      </Card>
    </div>
  );
}
