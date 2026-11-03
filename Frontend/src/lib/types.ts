// TypeScript mirrors of the C# DTOs in Backend/LearnBackend.Api/Contracts/Dtos.cs.
// ASP.NET Core serializes PascalCase C# properties as camelCase JSON by default.

export interface LocalizedText {
  ar: string;
  en?: string | null;
}

export interface PhaseStatus {
  id: number;
  unlocked: boolean;
  lessonsCompleted: number;
  lessonsTotal: number;
  exercisesSolved: number;
  exercisesTotal: number;
  projectSolved: boolean;
  percent: number;
}

export interface ExerciseSummary {
  id: string;
  title: LocalizedText;
  kind: 'exercise' | 'project';
  difficulty: 'easy' | 'medium' | 'hard';
  solved: boolean;
}

export interface LessonSummary {
  id: string;
  order: number;
  title: LocalizedText;
  summary: LocalizedText;
  minutes: number;
  completed: boolean;
  quizBestScore: number;
  exercises: ExerciseSummary[];
}

export interface Phase {
  id: number;
  slug: string;
  title: LocalizedText;
  description: LocalizedText;
  status: PhaseStatus;
  lessons: LessonSummary[];
  project: ExerciseSummary | null;
}

export interface Curriculum {
  phases: Phase[];
}

export interface QuizQuestion {
  question: LocalizedText;
  options: LocalizedText[];
  code?: string | null;
}

// ---------- Lesson blocks (see content/lessons/*.json) ----------
type Localized = LocalizedText;

export type Block =
  | ({ type: 'text' } & Localized)
  | ({ type: 'heading' } & Localized)
  | ({ type: 'analogy' } & Localized)
  | ({ type: 'callout'; kind: 'tip' | 'warning' | 'info' | 'dotnet' } & Localized)
  | { type: 'code'; lang: string; code: string; title?: Localized; runnable?: boolean }
  | { type: 'compare'; title: Localized; items: { lang: string; label: string; code: string }[]; note?: Localized }
  | { type: 'annotated'; lang: string; title: Localized; code: string; notes: Record<string, Localized> }
  | { type: 'source'; path: string; find: string; context: number; note: Localized }
  | { type: 'mistakes'; items: ({ title: Localized; wrong: string; right: string } & Localized)[] }
  | { type: 'visual'; name: string; caption?: Localized }
  | { type: 'table'; headers: Localized[]; rows: string[][] };

export interface Lesson {
  id: string;
  phase: number;
  order: number;
  title: LocalizedText;
  summary: LocalizedText;
  minutes: number;
  terms: string[];
  sections: Block[];
  quiz: QuizQuestion[];
  exercises: ExerciseSummary[];
  completed: boolean;
  quizBestScore: number;
  previousLessonId: string | null;
  nextLessonId: string | null;
}

export interface QuizResult {
  score: number;
  total: number;
  passed: boolean;
  lessonCompleted: boolean;
  questions: { selected: number; correct: number; isCorrect: boolean; explanation: LocalizedText }[];
}

export interface Exercise {
  id: string;
  title: LocalizedText;
  kind: 'exercise' | 'project';
  difficulty: 'easy' | 'medium' | 'hard';
  phase: number;
  lessonId: string;
  prompt: LocalizedText;
  terms: string[];
  starterCode: string;
  savedCode: string | null;
  hintsTotal: number;
  revealedHints: LocalizedText[];
  solved: boolean;
  attempts: number;
  bestPassed: number;
  totalTests: number;
  canViewSolution: boolean;
}

export type DiagnosticSource = 'User' | 'Tests' | 'Safety';

export interface CodeDiagnostic {
  id: string;
  severity: 'error' | 'warning';
  source: DiagnosticSource;
  line: number;
  column: number;
  endLine: number;
  endColumn: number;
  message: string;
  messageAr: string | null;
  tipAr: string | null;
  tipEn: string | null;
}

export type TestStatus = 'Passed' | 'Failed' | 'Error' | 'Timeout' | 'NotRun';
export type RunOutcome = 'Passed' | 'Failed' | 'CompileError' | 'Blocked' | 'Timeout' | 'MemoryLimit' | 'Crash' | 'Completed';

export interface TestCaseResult {
  name: string;
  ar: string;
  en: string;
  status: TestStatus;
  message: string | null;
  expected: string | null;
  actual: string | null;
  exceptionType: string | null;
  line: number | null;
  output: string | null;
  ms: number;
}

export interface SubmissionResult {
  outcome: RunOutcome;
  diagnostics: CodeDiagnostic[];
  tests: TestCaseResult[];
  passedCount: number;
  totalCount: number;
  elapsedMs: number;
  error: string | null;
}

export interface ProgramRunResult {
  outcome: RunOutcome;
  diagnostics: CodeDiagnostic[];
  output: string;
  error: string | null;
  truncated: boolean;
  elapsedMs: number;
}

export interface GlossaryTerm {
  id: string;
  en: string;
  ar: string;
  definition: LocalizedText;
  snippet: string | null;
  related: string[];
  lesson: string | null;
  category: 'csharp' | 'dotnet' | 'aspnet' | null;
}

export interface Flashcard {
  term: GlossaryTerm;
  isNew: boolean;
  repetitions: number;
  intervalDays: number;
  easeFactor: number;
  correctCount: number;
  wrongCount: number;
}

export interface ReviewResult {
  termId: string;
  dueAt: string;
  intervalDays: number;
  easeFactor: number;
  mastered: boolean;
}

export interface Dashboard {
  phases: PhaseStatus[];
  streak: number;
  termsMastered: number;
  termsStudied: number;
  termsTotal: number;
  termsDue: number;
  exercisesSolved: number;
  exercisesTotal: number;
  lessonsCompleted: number;
  lessonsTotal: number;
  continue: { lessonId: string; title: LocalizedText; phase: number } | null;
  weakTerms: { id: string; en: string; ar: string; wrong: number; correct: number }[];
  activity: { date: string; actions: number }[];
}

export interface SourceSnippet {
  path: string;
  language: string;
  line: number;
  startLine: number;
  lines: string[];
  totalLines: number;
}

export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}
