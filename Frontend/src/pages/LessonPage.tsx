import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { ArrowLeft, ArrowRight, CheckCircle2, Clock, Code2, Lock, Trophy, XCircle } from 'lucide-react';
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { LessonBlock } from '../components/LessonBlocks';
import { CodeView } from '../components/CodeView';
import { TermChip } from '../components/TermProvider';
import { Badge, Button, Card, DifficultyBadge, ErrorState, Spinner } from '../components/ui';
import { useI18n } from '../i18n/I18nProvider';
import { api, ApiError } from '../lib/api';
import type { Lesson, QuizResult } from '../lib/types';

export function LessonPage() {
  const { id = '' } = useParams();
  const { t, pick, lang } = useI18n();
  const { data: lesson, isLoading, error, refetch } = useQuery({ queryKey: ['lesson', id], queryFn: () => api.lesson(id), retry: false });

  if (isLoading) return <Spinner />;
  if (error instanceof ApiError && error.status === 403) {
    return (
      <Card className="mx-auto my-16 max-w-lg p-8 text-center">
        <Lock className="mx-auto mb-3 size-10 text-muted" />
        <p className="mb-4 leading-8">{error.problem?.detail}</p>
        <Link to="/learn" className="text-accent hover:underline">
          {t('nav.learn')}
        </Link>
      </Card>
    );
  }
  if (error || !lesson) return <ErrorState error={error} onRetry={refetch} />;

  const Prev = lang === 'ar' ? ArrowRight : ArrowLeft;
  const Next = lang === 'ar' ? ArrowLeft : ArrowRight;

  return (
    <article className="mx-auto max-w-4xl px-4 py-8">
      <header className="mb-8">
        <Link to={`/learn#phase-${lesson.phase}`} className="text-sm text-muted hover:text-accent">
          {t('learn.phase')} {lesson.phase} · {t('nav.learn')}
        </Link>
        <h1 className="mt-2 text-3xl leading-tight font-bold sm:text-4xl">{pick(lesson.title)}</h1>
        <p className="mt-3 text-lg leading-8 text-muted">{pick(lesson.summary)}</p>
        <div className="mt-4 flex flex-wrap items-center gap-2">
          <Badge>
            <Clock className="size-3" /> {lesson.minutes} {t('common.minutes')}
          </Badge>
          {lesson.completed && (
            <Badge tone="ok">
              <CheckCircle2 className="size-3" /> {t('lesson.completed')}
            </Badge>
          )}
        </div>
        {lesson.terms.length > 0 && (
          <div className="mt-6 rounded-2xl border border-line bg-surface p-4">
            <p className="mb-2.5 text-sm font-medium text-muted">{t('lesson.terms')}</p>
            <div className="flex flex-wrap gap-2">
              {lesson.terms.map((term) => (
                <TermChip key={term} id={term} />
              ))}
            </div>
          </div>
        )}
      </header>

      <div className="space-y-6 text-[16.5px] leading-8">
        {lesson.sections.map((block, i) => (
          <LessonBlock key={i} block={block} />
        ))}
      </div>

      <section className="mt-14">
        <h2 className="mb-4 flex items-center gap-2 text-xl font-bold">
          <Code2 className="size-5 text-accent" />
          {t('lesson.exercises')}
        </h2>
        <div className="grid gap-3 sm:grid-cols-2">
          {lesson.exercises.map((e) => (
            <Link key={e.id} to={`/exercises/${e.id}`} className="group rounded-2xl border border-line bg-surface p-4 transition hover:border-accent">
              <div className="mb-2 flex items-center gap-2">
                {e.kind === 'project' && <Trophy className="size-4 text-warn" />}
                <DifficultyBadge difficulty={e.difficulty} />
                {e.solved && <Badge tone="ok">{t('exercise.solved')}</Badge>}
              </div>
              <p className="font-semibold group-hover:text-accent">{pick(e.title)}</p>
            </Link>
          ))}
        </div>
      </section>

      <Quiz lesson={lesson} />

      <nav className="mt-12 flex justify-between gap-4 border-t border-line pt-6">
        {lesson.previousLessonId ? (
          <Link to={`/lessons/${lesson.previousLessonId}`} className="inline-flex items-center gap-2 text-muted hover:text-accent">
            <Prev className="size-4" /> {t('common.previous')}
          </Link>
        ) : (
          <span />
        )}
        {lesson.nextLessonId && (
          <Link to={`/lessons/${lesson.nextLessonId}`} className="inline-flex items-center gap-2 font-medium text-accent hover:underline">
            {t('common.next')} <Next className="size-4" />
          </Link>
        )}
      </nav>
    </article>
  );
}

function Quiz({ lesson }: { lesson: Lesson }) {
  const { t, pick } = useI18n();
  const queryClient = useQueryClient();
  const [answers, setAnswers] = useState<(number | null)[]>(() => lesson.quiz.map(() => null));
  const [result, setResult] = useState<QuizResult | null>(null);

  const submit = useMutation({
    mutationFn: () => api.submitQuiz(lesson.id, answers as number[]),
    onSuccess: (r) => {
      setResult(r);
      queryClient.invalidateQueries({ queryKey: ['curriculum'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      queryClient.invalidateQueries({ queryKey: ['lesson', lesson.id] });
    },
  });

  const allAnswered = answers.every((a) => a !== null);

  return (
    <section className="mt-14 rounded-3xl border border-line bg-surface p-5 sm:p-7">
      <h2 className="mb-6 text-xl font-bold">{t('lesson.quiz')}</h2>
      <ol className="space-y-7">
        {lesson.quiz.map((q, qi) => {
          const r = result?.questions[qi];
          return (
            <li key={qi}>
              <p className="mb-3 font-semibold">
                {qi + 1}. {pick(q.question)}
              </p>
              {q.code && <CodeView code={q.code} lang="csharp" className="mb-3" />}
              <div className="grid gap-2">
                {q.options.map((opt, oi) => {
                  const selected = answers[qi] === oi;
                  const isCorrect = r && r.correct === oi;
                  const isWrongPick = r && selected && !r.isCorrect;
                  return (
                    <label
                      key={oi}
                      className={clsx(
                        'flex cursor-pointer items-center gap-3 rounded-xl border px-4 py-2.5 transition',
                        !r && selected && 'border-accent bg-accent-soft',
                        !r && !selected && 'border-line hover:bg-surface-2',
                        isCorrect && 'border-ok bg-ok-soft',
                        isWrongPick && 'border-bad bg-bad-soft',
                        r && !isCorrect && !isWrongPick && 'border-line opacity-60',
                      )}
                    >
                      <input
                        type="radio"
                        name={`q${qi}`}
                        checked={selected}
                        disabled={!!r}
                        onChange={() => setAnswers((a) => a.map((x, i) => (i === qi ? oi : x)))}
                        className="accent-[var(--accent)]"
                      />
                      <span className="flex-1" dir="auto">
                        {pick(opt)}
                      </span>
                      {isCorrect && <CheckCircle2 className="size-4 text-ok" />}
                      {isWrongPick && <XCircle className="size-4 text-bad" />}
                    </label>
                  );
                })}
              </div>
              {r && <p className={clsx('mt-2 rounded-xl px-4 py-2 text-sm', r.isCorrect ? 'bg-ok-soft/60' : 'bg-bad-soft/60')}>{pick(r.explanation)}</p>}
            </li>
          );
        })}
      </ol>

      <div className="mt-8 flex flex-wrap items-center gap-4">
        {!result ? (
          <>
            <Button variant="primary" size="lg" disabled={!allAnswered || submit.isPending} onClick={() => submit.mutate()}>
              {t('quiz.submit')}
            </Button>
            {!allAnswered && <span className="text-sm text-muted">{t('quiz.answerAll')}</span>}
          </>
        ) : (
          <>
            <div className={clsx('rounded-xl px-4 py-2 font-semibold', result.passed ? 'bg-ok-soft text-ok' : 'bg-warn-soft text-warn')}>
              {t('quiz.score')}: {result.score}/{result.total}
            </div>
            <span className="text-sm">{result.passed ? t('quiz.passed') : t('quiz.failed')}</span>
            <Button
              variant="ghost"
              onClick={() => {
                setResult(null);
                setAnswers(lesson.quiz.map(() => null));
              }}
            >
              {t('quiz.retry')}
            </Button>
          </>
        )}
      </div>
    </section>
  );
}
