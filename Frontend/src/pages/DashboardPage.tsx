import { useQuery } from '@tanstack/react-query';
import clsx from 'clsx';
import { ArrowLeft, ArrowRight, BookCheck, Brain, Code2, Flame, Layers, Lock } from 'lucide-react';
import { Link } from 'react-router-dom';
import { TermChip } from '../components/TermProvider';
import { Card, ErrorState, ProgressBar, Spinner } from '../components/ui';
import { useI18n } from '../i18n/I18nProvider';
import { api } from '../lib/api';

export function DashboardPage() {
  const { t, pick, lang } = useI18n();
  const dashboard = useQuery({ queryKey: ['dashboard'], queryFn: api.dashboard });
  const curriculum = useQuery({ queryKey: ['curriculum'], queryFn: api.curriculum });
  const Arrow = lang === 'ar' ? ArrowLeft : ArrowRight;

  if (dashboard.isLoading || curriculum.isLoading) return <Spinner />;
  if (dashboard.error || curriculum.error) return <ErrorState error={dashboard.error ?? curriculum.error} onRetry={() => dashboard.refetch()} />;
  const d = dashboard.data!;
  const phases = curriculum.data!.phases;
  const started = d.lessonsCompleted > 0 || d.exercisesSolved > 0 || d.termsStudied > 0;

  const stats = [
    { icon: Flame, value: d.streak, label: t('dashboard.streak'), tone: 'text-warn bg-warn-soft' },
    { icon: Brain, value: `${d.termsMastered}`, sub: `/ ${d.termsTotal}`, label: t('dashboard.terms'), tone: 'text-accent bg-accent-soft' },
    { icon: Code2, value: `${d.exercisesSolved}`, sub: `/ ${d.exercisesTotal}`, label: t('dashboard.exercises'), tone: 'text-ok bg-ok-soft' },
    { icon: BookCheck, value: `${d.lessonsCompleted}`, sub: `/ ${d.lessonsTotal}`, label: t('dashboard.lessons'), tone: 'text-info bg-info-soft' },
  ];

  return (
    <div className="mx-auto max-w-6xl px-4 py-8">
      <div className="mb-8">
        <h1 className="text-2xl font-bold sm:text-3xl">{started ? t('dashboard.welcome') : t('dashboard.start')}</h1>
        <p className="mt-1 text-muted">{t('app.tagline')}</p>
      </div>

      {d.continue ? (
        <Link
          to={`/lessons/${d.continue.lessonId}`}
          className="group mb-8 flex items-center gap-4 rounded-2xl bg-linear-to-br from-accent to-accent-2 p-5 text-white shadow-lg shadow-accent/20 transition hover:brightness-110 sm:p-6"
        >
          <div className="min-w-0 flex-1">
            <p className="text-sm opacity-85">
              {t('dashboard.continue')} · {t('learn.phase')} {d.continue.phase}
            </p>
            <p className="mt-1 truncate text-xl font-bold sm:text-2xl">{pick(d.continue.title)}</p>
          </div>
          <span className="grid size-11 shrink-0 place-items-center rounded-full bg-white/20 transition group-hover:scale-110">
            <Arrow className="size-5" />
          </span>
        </Link>
      ) : (
        <Card className="mb-8 p-5 text-muted">{t('dashboard.allDone')}</Card>
      )}

      <div className="mb-8 grid grid-cols-2 gap-3 lg:grid-cols-4">
        {stats.map(({ icon: Icon, value, sub, label, tone }) => (
          <Card key={label} className="p-4">
            <span className={clsx('mb-3 grid size-9 place-items-center rounded-xl', tone)}>
              <Icon className="size-5" />
            </span>
            <p className="text-2xl font-bold tabular-nums" dir="ltr">
              {value}
              {sub && <span className="text-base font-normal text-muted"> {sub}</span>}
            </p>
            <p className="text-sm text-muted">{label}</p>
          </Card>
        ))}
      </div>

      <div className="grid gap-6 lg:grid-cols-[minmax(0,1.6fr)_minmax(0,1fr)]">
        <Card className="p-5">
          <h2 className="mb-4 font-semibold">{t('dashboard.phases')}</h2>
          <ul className="space-y-4">
            {phases.map((p) => (
              <li key={p.id}>
                <Link to={`/learn#phase-${p.id}`} className={clsx('block', !p.status.unlocked && 'opacity-55')}>
                  <div className="mb-1.5 flex items-center gap-2 text-sm">
                    <span className="grid size-6 place-items-center rounded-md bg-surface-2 text-xs font-bold tabular-nums">{p.id}</span>
                    <span className="flex-1 truncate font-medium">{pick(p.title)}</span>
                    {p.lessons.length === 0 ? (
                      <span className="text-xs text-muted">{t('common.comingSoon')}</span>
                    ) : !p.status.unlocked ? (
                      <Lock className="size-3.5 text-muted" />
                    ) : (
                      <span className="text-xs tabular-nums text-muted">{p.status.percent}%</span>
                    )}
                  </div>
                  <ProgressBar value={p.status.percent} tone={p.status.percent === 100 ? 'ok' : 'accent'} />
                </Link>
              </li>
            ))}
          </ul>
        </Card>

        <div className="space-y-6">
          <Card className="p-5">
            <div className="mb-3 flex items-center gap-2">
              <Layers className="size-5 text-accent" />
              <p className="flex-1">
                <span className="text-2xl font-bold tabular-nums">{d.termsDue}</span> <span className="text-sm text-muted">{t('dashboard.due')}</span>
              </p>
            </div>
            <Link to="/flashcards" className="inline-flex h-10 w-full items-center justify-center rounded-xl bg-accent font-medium text-white hover:brightness-110">
              {t('dashboard.review')}
            </Link>
            {d.weakTerms.length > 0 && (
              <>
                <p className="mt-5 mb-2 text-sm font-medium text-muted">{t('dashboard.weak')}</p>
                <div className="flex flex-wrap gap-2">
                  {d.weakTerms.map((w) => (
                    <TermChip key={w.id} id={w.id} />
                  ))}
                </div>
              </>
            )}
          </Card>
          <Card className="p-5">
            <p className="mb-3 text-sm font-medium text-muted">{t('dashboard.activity')}</p>
            <ActivityGrid activity={d.activity} />
          </Card>
        </div>
      </div>
    </div>
  );
}

function ActivityGrid({ activity }: { activity: { date: string; actions: number }[] }) {
  const byDate = new Map(activity.map((a) => [a.date, a.actions]));
  const today = new Date();
  const days = Array.from({ length: 84 }, (_, i) => {
    const d = new Date(today);
    d.setDate(today.getDate() - (83 - i));
    const key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    return { key, count: byDate.get(key) ?? 0 };
  });
  const level = (n: number) => (n === 0 ? 'bg-surface-3' : n < 3 ? 'bg-accent/35' : n < 8 ? 'bg-accent/65' : 'bg-accent');
  return (
    <div dir="ltr" className="grid grid-flow-col grid-rows-7 gap-1">
      {days.map((d) => (
        <span key={d.key} title={`${d.key}: ${d.count}`} className={clsx('aspect-square rounded-[3px]', level(d.count))} />
      ))}
    </div>
  );
}
