import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import clsx from 'clsx';
import { BookOpen, CheckCircle2, Eye, KeyRound, Lightbulb, Loader2, Lock, Play, RotateCcw, Trophy } from 'lucide-react';
import { lazy, Suspense, useCallback, useEffect, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import type { CodeEditorHandle } from '../components/CodeEditor';
import { CodeView } from '../components/CodeView';
import { RichText } from '../components/RichText';
import { DiagnosticsList, OutcomeBanner, TestList } from '../components/RunResults';
import { TermChip, useLessonTitles } from '../components/TermProvider';
import { Badge, Button, Card, DifficultyBadge, ErrorState, Spinner } from '../components/ui';
import { useI18n } from '../i18n/I18nProvider';
import { api, ApiError } from '../lib/api';
import type { Exercise, SubmissionResult } from '../lib/types';

const CodeEditor = lazy(() => import('../components/CodeEditor'));

const storageKey = (id: string) => `lb-code-${id}`;

export function ExercisePage() {
  const { id = '' } = useParams();
  const { t } = useI18n();
  const { data, isLoading, error, refetch } = useQuery({ queryKey: ['exercise', id], queryFn: () => api.exercise(id), retry: false });

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
  if (error || !data) return <ErrorState error={error} onRetry={refetch} />;
  return <ExerciseWorkspace key={data.id} exercise={data} />;
}

function ExerciseWorkspace({ exercise }: { exercise: Exercise }) {
  const { t, pick, lang } = useI18n();
  const queryClient = useQueryClient();
  const lessons = useLessonTitles();
  const editorRef = useRef<CodeEditorHandle>(null);

  const [code, setCode] = useState(() => {
    if (exercise.savedCode) return exercise.savedCode;
    try {
      return localStorage.getItem(storageKey(exercise.id)) ?? exercise.starterCode;
    } catch {
      return exercise.starterCode;
    }
  });
  const [saveState, setSaveState] = useState<'saved' | 'saving'>('saved');
  const [result, setResult] = useState<SubmissionResult | null>(null);
  const [solution, setSolution] = useState<string | null>(null);

  // Autosave: localStorage immediately, the server after 1.2 s without typing.
  const firstRender = useRef(true);
  useEffect(() => {
    if (firstRender.current) {
      firstRender.current = false;
      return;
    }
    try {
      localStorage.setItem(storageKey(exercise.id), code);
    } catch {
      /* private mode */
    }
    if (!code.trim()) return;
    setSaveState('saving');
    const timer = setTimeout(() => {
      api.saveCode(exercise.id, code).finally(() => setSaveState('saved'));
    }, 1200);
    return () => clearTimeout(timer);
  }, [code, exercise.id]);

  const submit = useMutation({
    mutationFn: () => api.submit(exercise.id, code),
    onSuccess: (r) => {
      setResult(r);
      queryClient.invalidateQueries({ queryKey: ['exercise', exercise.id] });
      queryClient.invalidateQueries({ queryKey: ['curriculum'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });
  const run = useCallback(() => {
    if (!submit.isPending && code.trim()) submit.mutate();
  }, [submit, code]);

  const hint = useMutation({
    mutationFn: () => api.revealHint(exercise.id),
    onSuccess: (updated) => queryClient.setQueryData(['exercise', exercise.id], updated),
  });
  const showSolution = useMutation({
    mutationFn: () => api.solution(exercise.id),
    onSuccess: (s) => setSolution(s.code),
  });

  const jump = (line: number) => editorRef.current?.revealLine(line);
  const lesson = exercise.lessonId ? lessons.get(exercise.lessonId) : undefined;
  const hintsLeft = exercise.hintsTotal - exercise.revealedHints.length;

  return (
    <div className="mx-auto grid max-w-[1600px] gap-4 px-4 py-4 lg:h-[calc(100dvh-3.5rem)] lg:grid-cols-[minmax(0,0.85fr)_minmax(0,1.15fr)] lg:overflow-hidden">
      {/* ---------- Left: explanation ---------- */}
      <section className="space-y-5 lg:overflow-y-auto lg:pe-2">
        <div>
          <div className="mb-2 flex flex-wrap items-center gap-2 text-sm">
            {exercise.kind === 'project' && (
              <Badge tone="warn">
                <Trophy className="size-3" /> {t('learn.project')}
              </Badge>
            )}
            <DifficultyBadge difficulty={exercise.difficulty} />
            {exercise.solved && (
              <Badge tone="ok">
                <CheckCircle2 className="size-3" /> {t('exercise.solved')}
              </Badge>
            )}
            {exercise.attempts > 0 && (
              <span className="text-xs text-muted">
                {exercise.attempts} {t('exercise.attempts')}
              </span>
            )}
          </div>
          <h1 className="text-2xl font-bold">{pick(exercise.title)}</h1>
          {lesson && (
            <Link to={`/lessons/${exercise.lessonId}`} className="mt-1 inline-flex items-center gap-1.5 text-sm text-muted hover:text-accent">
              <BookOpen className="size-4" /> {t('exercise.lesson')}: {pick(lesson.title)}
            </Link>
          )}
        </div>

        <Card className="p-5">
          <h2 className="mb-3 text-sm font-semibold text-muted">{t('exercise.instructions')}</h2>
          <div className="leading-8" dir={lang === 'en' && exercise.prompt.en ? 'ltr' : 'rtl'}>
            <RichText text={pick(exercise.prompt)} />
          </div>
          {exercise.terms.length > 0 && (
            <div className="mt-4 flex flex-wrap gap-1.5 border-t border-line pt-4">
              {exercise.terms.map((term) => (
                <TermChip key={term} id={term} className="text-xs" />
              ))}
            </div>
          )}
        </Card>

        <Card className="p-5">
          <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold text-muted">
            <Lightbulb className="size-4 text-warn" /> {t('exercise.hints')}
            <span className="tabular-nums">
              ({exercise.revealedHints.length}/{exercise.hintsTotal})
            </span>
          </h2>
          <ol className="space-y-3">
            {exercise.revealedHints.map((h, i) => (
              <li key={i} className="animate-fade-up rounded-xl border border-warn/30 bg-warn-soft/50 p-3 text-[15px]">
                <span className="mb-1 block text-xs font-semibold text-warn">
                  {t('exercise.hint')} {i + 1}
                </span>
                <RichText text={pick(h)} />
              </li>
            ))}
          </ol>
          <div className="mt-3 flex flex-wrap gap-2">
            {hintsLeft > 0 && (
              <Button size="sm" onClick={() => hint.mutate()} disabled={hint.isPending}>
                <Lightbulb className="size-4" /> {t('exercise.showHint')}
              </Button>
            )}
            {exercise.canViewSolution ? (
              <Button size="sm" variant="ghost" onClick={() => showSolution.mutate()} disabled={showSolution.isPending}>
                <Eye className="size-4" /> {t('exercise.solution')}
              </Button>
            ) : (
              <span className="inline-flex items-center gap-1.5 text-xs text-muted">
                <KeyRound className="size-3.5" /> {t('exercise.solutionLocked')}
              </span>
            )}
          </div>
          {solution && (
            <div className="mt-4">
              <p className="mb-2 text-sm font-semibold">{t('exercise.solutionTitle')}</p>
              <CodeView code={solution} lang="csharp" maxHeight={360} />
              <Button size="sm" className="mt-2" onClick={() => setCode(solution)}>
                {t('exercise.useSolution')}
              </Button>
            </div>
          )}
        </Card>
      </section>

      {/* ---------- Right: editor + results ---------- */}
      <section className="flex min-h-0 flex-col gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <Button variant="primary" onClick={run} disabled={submit.isPending}>
            {submit.isPending ? <Loader2 className="size-4 animate-spin" /> : <Play className="size-4" />}
            {submit.isPending ? t('exercise.running') : t('exercise.run')}
          </Button>
          <Button
            variant="ghost"
            onClick={() => {
              if (confirm(t('exercise.resetConfirm'))) setCode(exercise.starterCode);
            }}
          >
            <RotateCcw className="size-4" />
            <span className="hidden sm:inline">{t('exercise.reset')}</span>
          </Button>
          <span className="ms-auto hidden text-xs text-muted sm:inline">{t('exercise.shortcut')}</span>
          <span className={clsx('text-xs', saveState === 'saved' ? 'text-ok' : 'text-muted')}>
            {saveState === 'saved' ? `✓ ${t('exercise.saved')}` : t('exercise.saving')}
          </span>
        </div>

        <div className="h-[55vh] min-h-[320px] overflow-hidden rounded-2xl border border-line lg:h-auto lg:flex-[1.4]">
          <Suspense fallback={<Spinner />}>
            <CodeEditor ref={editorRef} value={code} onChange={setCode} diagnostics={result?.diagnostics} onRun={run} />
          </Suspense>
        </div>

        <div className="min-h-0 overflow-y-auto rounded-2xl border border-line bg-surface p-4 lg:flex-1">
          <h2 className="mb-3 text-sm font-semibold text-muted">{t('exercise.results')}</h2>
          {submit.error && <p className="text-sm text-bad">{String(submit.error)}</p>}
          {!result && !submit.isPending && <p className="text-sm text-muted">{t('exercise.noRunYet')}</p>}
          {submit.isPending && (
            <div className="flex items-center gap-2 text-sm text-muted">
              <Loader2 className="size-4 animate-spin" /> {t('exercise.running')}
            </div>
          )}
          {result && !submit.isPending && (
            <div className="animate-fade-up space-y-3">
              <OutcomeBanner outcome={result.outcome} passed={result.passedCount} total={result.totalCount} elapsedMs={result.elapsedMs} />
              {result.error && (
                <pre className="overflow-x-auto rounded-lg bg-code p-3 font-mono text-xs whitespace-pre-wrap text-bad" dir="ltr">
                  {result.error}
                </pre>
              )}
              <DiagnosticsList diagnostics={result.diagnostics} onJump={jump} />
              <TestList tests={result.tests} onJump={jump} />
            </div>
          )}
        </div>
      </section>
    </div>
  );
}
