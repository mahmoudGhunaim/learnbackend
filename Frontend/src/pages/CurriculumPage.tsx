import { useQuery } from '@tanstack/react-query';
import clsx from 'clsx';
import { CheckCircle2, Circle, Clock, Code2, Lock, Trophy } from 'lucide-react';
import { useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { PageHeader } from '../components/Layout';
import { Badge, Card, DifficultyBadge, ErrorState, ProgressBar, Spinner } from '../components/ui';
import { useI18n } from '../i18n/I18nProvider';
import { api } from '../lib/api';
import type { ExerciseSummary, Phase } from '../lib/types';

export function CurriculumPage() {
  const { t } = useI18n();
  const { data, isLoading, error, refetch } = useQuery({ queryKey: ['curriculum'], queryFn: api.curriculum });
  const { hash } = useLocation();

  useEffect(() => {
    if (hash && data) document.querySelector(hash)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }, [hash, data]);

  if (isLoading) return <Spinner />;
  if (error) return <ErrorState error={error} onRetry={refetch} />;

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <PageHeader title={t('learn.title')} subtitle={t('learn.subtitle')} />
      <ol className="relative space-y-6 border-s-2 border-line ps-6">
        {data!.phases.map((phase) => (
          <PhaseCard key={phase.id} phase={phase} />
        ))}
      </ol>
    </div>
  );
}

function PhaseCard({ phase }: { phase: Phase }) {
  const { t, pick } = useI18n();
  const locked = !phase.status.unlocked;
  const empty = phase.lessons.length === 0;
  const done = phase.status.percent === 100;

  return (
    <li id={`phase-${phase.id}`} className="relative scroll-mt-20">
      <span
        className={clsx(
          'absolute -start-[2.15rem] top-5 grid size-6 place-items-center rounded-full border-2 text-xs font-bold',
          done ? 'border-ok bg-ok text-white' : locked ? 'border-line bg-surface text-muted' : 'border-accent bg-accent text-white',
        )}
      >
        {phase.id}
      </span>
      <Card className={clsx('overflow-hidden', (locked || empty) && 'opacity-70')}>
        <div className="p-5">
          <div className="mb-2 flex flex-wrap items-center gap-2">
            <span className="text-xs font-medium text-muted">
              {t('learn.phase')} {phase.id}
            </span>
            {empty ? <Badge>{t('common.comingSoon')}</Badge> : locked && <Badge><Lock className="size-3" /> {t('common.locked')}</Badge>}
            {!empty && (
              <span className="ms-auto text-xs text-muted tabular-nums">
                {phase.status.lessonsCompleted}/{phase.status.lessonsTotal} · {phase.status.percent}%
              </span>
            )}
          </div>
          <h2 className="text-xl font-bold">{pick(phase.title)}</h2>
          <p className="mt-1 text-[15px] text-muted">{pick(phase.description)}</p>
          {!empty && <ProgressBar value={phase.status.percent} className="mt-4" tone={done ? 'ok' : 'accent'} />}
          {locked && !empty && <p className="mt-3 text-sm text-warn">{t('learn.lockedHint')}</p>}
        </div>

        {!empty && !locked && (
          <ul className="border-t border-line">
            {phase.lessons.map((lesson) => (
              <li key={lesson.id} className="border-b border-line last:border-b-0">
                <Link to={`/lessons/${lesson.id}`} className="flex items-start gap-3 px-5 py-3.5 transition hover:bg-surface-2">
                  {lesson.completed ? <CheckCircle2 className="mt-1 size-5 shrink-0 text-ok" /> : <Circle className="mt-1 size-5 shrink-0 text-muted" />}
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
                      <span className="font-semibold">
                        {lesson.order}. {pick(lesson.title)}
                      </span>
                      <span className="inline-flex items-center gap-1 text-xs text-muted">
                        <Clock className="size-3" /> {lesson.minutes} {t('common.minutes')}
                      </span>
                      {lesson.quizBestScore > 0 && <span className="text-xs text-muted tabular-nums">quiz {lesson.quizBestScore}/5</span>}
                    </div>
                    <p className="mt-0.5 line-clamp-2 text-sm text-muted">{pick(lesson.summary)}</p>
                    <div className="mt-2 flex flex-wrap gap-1.5">
                      {lesson.exercises.map((e) => (
                        <ExercisePill key={e.id} exercise={e} />
                      ))}
                    </div>
                  </div>
                </Link>
              </li>
            ))}
            {phase.project && (
              <li className="bg-accent-soft/30">
                <Link to={`/exercises/${phase.project.id}`} className="flex items-center gap-3 px-5 py-4 transition hover:bg-accent-soft/60">
                  <Trophy className={clsx('size-5 shrink-0', phase.project.solved ? 'text-ok' : 'text-warn')} />
                  <div className="flex-1">
                    <p className="text-xs text-muted">{t('learn.project')}</p>
                    <p className="font-semibold">{pick(phase.project.title)}</p>
                  </div>
                  {phase.project.solved ? <Badge tone="ok">{t('exercise.solved')}</Badge> : <DifficultyBadge difficulty={phase.project.difficulty} />}
                </Link>
              </li>
            )}
          </ul>
        )}
      </Card>
    </li>
  );
}

function ExercisePill({ exercise }: { exercise: ExerciseSummary }) {
  const { pick } = useI18n();
  return (
    <span className={clsx('inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs', exercise.solved ? 'bg-ok-soft text-ok' : 'bg-surface-3 text-muted')}>
      {exercise.solved ? <CheckCircle2 className="size-3" /> : <Code2 className="size-3" />}
      {pick(exercise.title)}
    </span>
  );
}
