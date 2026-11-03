import type {
  Curriculum,
  Dashboard,
  Exercise,
  Flashcard,
  GlossaryTerm,
  Lesson,
  ProblemDetails,
  ProgramRunResult,
  QuizResult,
  ReviewResult,
  SourceSnippet,
  SubmissionResult,
} from './types';

/** Error carrying the RFC 9457 ProblemDetails body the API returns for 4xx/5xx. */
export class ApiError extends Error {
  constructor(
    public status: number,
    public problem: ProblemDetails | null,
  ) {
    super(problem?.detail ?? problem?.title ?? `HTTP ${status}`);
  }
}

async function request<T>(method: string, url: string, body?: unknown): Promise<T> {
  const response = await fetch(url, {
    method,
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!response.ok) {
    let problem: ProblemDetails | null = null;
    try {
      problem = await response.json();
    } catch {
      /* empty body */
    }
    throw new ApiError(response.status, problem);
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export const api = {
  curriculum: () => request<Curriculum>('GET', '/api/curriculum'),
  lesson: (id: string) => request<Lesson>('GET', `/api/lessons/${id}`),
  submitQuiz: (id: string, answers: number[]) => request<QuizResult>('POST', `/api/lessons/${id}/quiz`, { answers }),

  exercise: (id: string) => request<Exercise>('GET', `/api/exercises/${id}`),
  saveCode: (id: string, code: string) => request<void>('PUT', `/api/exercises/${id}/code`, { code }),
  revealHint: (id: string) => request<Exercise>('POST', `/api/exercises/${id}/hints`),
  submit: (id: string, code: string) => request<SubmissionResult>('POST', `/api/exercises/${id}/submit`, { code }),
  solution: (id: string) => request<{ code: string }>('GET', `/api/exercises/${id}/solution`),

  runPlayground: (code: string) => request<ProgramRunResult>('POST', '/api/playground/run', { code }),
  source: (path: string, find?: string, context?: number) => {
    const params = new URLSearchParams({ path });
    if (find) params.set('find', find);
    if (context !== undefined) params.set('context', String(context));
    return request<SourceSnippet>('GET', `/api/source?${params}`);
  },

  glossary: () => request<GlossaryTerm[]>('GET', '/api/glossary'),
  flashcards: (limit = 20, phase?: number) =>
    request<Flashcard[]>('GET', `/api/flashcards/session?limit=${limit}${phase === undefined ? '' : `&phase=${phase}`}`),
  review: (termId: string, quality: number) => request<ReviewResult>('POST', `/api/flashcards/${termId}/review`, { quality }),

  dashboard: () => request<Dashboard>('GET', '/api/dashboard'),
};
