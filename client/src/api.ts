import { locale } from './locale'
import { adminToken, clearAdminToken } from './admin'

export type Difficulty = 'Easy' | 'Medium' | 'Hard'
export type Category = 'School' | 'Gym'
export type Verdict = 'Accepted' | 'WrongAnswer' | 'CompileError' | 'RuntimeError' | 'TimeLimitExceeded' | 'JudgeError'

export interface ProblemSummary {
  id: number
  slug: string
  title: string
  difficulty: Difficulty
  category: Category
  testCaseCount: number
  solved: boolean
}

export interface TestCase {
  id: number
  input: string
  expected: string
  isHidden: boolean
  order: number
}

export interface ProblemTranslation {
  title: string
  description: string
}

export interface ProblemTranslations {
  es?: ProblemTranslation
}

export interface Problem {
  id: number
  slug: string
  title: string
  description: string
  difficulty: Difficulty
  category: Category
  starterCode: string
  className: string
  methodName: string
  timeLimitMs: number
  testCases: TestCase[]
  /** Only present on the admin `/full` endpoint. */
  translations?: ProblemTranslations
}

export interface TestCaseInput {
  input: string
  expected: string
  isHidden: boolean
}

export interface ProblemInput {
  title: string
  slug?: string
  description: string
  difficulty: Difficulty
  category: Category
  starterCode: string
  methodName: string
  className?: string
  timeLimitMs?: number
  testCases: TestCaseInput[]
  translations?: ProblemTranslations
}

export interface CompileDiagnostic {
  line: number
  column: number
  code: string
  message: string
  isError: boolean
}

export interface TestCaseResult {
  testCaseId: number
  passed: boolean
  isHidden: boolean
  input?: string
  expected?: string
  actual?: string
  stdout?: string
  error?: string
  durationMs: number
}

export interface JudgeResult {
  verdict: Verdict
  message: string
  diagnostics: CompileDiagnostic[]
  results: TestCaseResult[]
  passedCount: number
  totalCount: number
  totalDurationMs: number
}

export interface Submission {
  id: number
  problemId: number
  verdict: Verdict
  passedCount: number
  totalCount: number
  createdAt: string
  code: string
  result?: JudgeResult
}

export interface CompletionItem {
  label: string
  kind: string
  detail?: string
}

export interface CompletionResponse {
  from: number
  items: CompletionItem[]
}

export type SandboxStatus = 'Ok' | 'CompileError' | 'RuntimeError' | 'TimeLimitExceeded'

export interface SandboxResult {
  status: SandboxStatus
  diagnostics: CompileDiagnostic[]
  stdout: string
  stderr: string
  exitCode: number | null
  timedOut: boolean
  durationMs: number
}

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

interface RequestOptions extends RequestInit {
  /** Attach the admin token; a 401 clears the stored token. */
  admin?: boolean
}

async function request<T>(path: string, init?: RequestOptions): Promise<T> {
  const headers: Record<string, string> = { 'content-type': 'application/json' }
  if (init?.admin && adminToken.value) headers['X-Admin-Token'] = adminToken.value
  const { admin, ...rest } = init ?? {}
  const res = await fetch(`/api${path}`, { ...rest, headers: { ...headers, ...(rest.headers as Record<string, string> | undefined) } })
  if (!res.ok) {
    if (admin && res.status === 401) clearAdminToken()
    let message = `${res.status} ${res.statusText}`
    try {
      const body = await res.json()
      if (body?.error) message = body.error
      else if (body?.title) message = body.title
    } catch {
      /* no body */
    }
    throw new ApiError(res.status, message)
  }
  if (res.status === 204) return undefined as T
  return (await res.json()) as T
}

const lang = () => `lang=${encodeURIComponent(locale.value)}`

export const api = {
  // Localized reads
  listProblems: () => request<ProblemSummary[]>(`/problems?${lang()}`),
  getProblem: (idOrSlug: string | number) => request<Problem>(`/problems/${idOrSlug}?${lang()}`),

  // Admin
  checkAdmin: (token: string) =>
    request<{ ok: boolean }>('/admin/check', { headers: { 'X-Admin-Token': token } }),
  getProblemFull: (idOrSlug: string | number) => request<Problem>(`/problems/${idOrSlug}/full`, { admin: true }),
  createProblem: (input: ProblemInput) =>
    request<Problem>('/problems', { method: 'POST', body: JSON.stringify(input), admin: true }),
  updateProblem: (id: number, input: ProblemInput) =>
    request<Problem>(`/problems/${id}`, { method: 'PUT', body: JSON.stringify(input), admin: true }),
  deleteProblem: (id: number) => request<void>(`/problems/${id}`, { method: 'DELETE', admin: true }),

  // Judge
  run: (id: number, code: string) => request<JudgeResult>(`/problems/${id}/run`, { method: 'POST', body: JSON.stringify({ code }) }),
  submit: (id: number, code: string) =>
    request<Submission>(`/problems/${id}/submit`, { method: 'POST', body: JSON.stringify({ code }) }),
  listSubmissions: (id: number) => request<Submission[]>(`/problems/${id}/submissions`),
  getSubmission: (id: number) => request<Submission>(`/submissions/${id}`),

  // Editor support
  complete: (code: string, position: number, signal?: AbortSignal) =>
    request<CompletionResponse>('/complete', { method: 'POST', body: JSON.stringify({ code, position }), signal }),

  // Sandbox
  sandboxRun: (code: string, stdin: string) =>
    request<SandboxResult>('/sandbox/run', { method: 'POST', body: JSON.stringify({ code, stdin }) }),
}

/** i18n key for a verdict, e.g. `verdict.Accepted`. */
export const verdictKey = (v: Verdict) => `verdict.${v}`
