export type Difficulty = 'Easy' | 'Medium' | 'Hard'
export type Verdict = 'Accepted' | 'WrongAnswer' | 'CompileError' | 'RuntimeError' | 'TimeLimitExceeded' | 'JudgeError'

export interface ProblemSummary {
  id: number
  slug: string
  title: string
  difficulty: Difficulty
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

export interface Problem {
  id: number
  slug: string
  title: string
  description: string
  difficulty: Difficulty
  starterCode: string
  className: string
  methodName: string
  timeLimitMs: number
  testCases: TestCase[]
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
  starterCode: string
  methodName: string
  className?: string
  timeLimitMs?: number
  testCases: TestCaseInput[]
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

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`/api${path}`, {
    ...init,
    headers: { 'content-type': 'application/json', ...(init?.headers ?? {}) },
  })
  if (!res.ok) {
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

export const api = {
  listProblems: () => request<ProblemSummary[]>('/problems'),
  getProblem: (idOrSlug: string | number) => request<Problem>(`/problems/${idOrSlug}`),
  getProblemFull: (idOrSlug: string | number) => request<Problem>(`/problems/${idOrSlug}/full`),
  createProblem: (input: ProblemInput) => request<Problem>('/problems', { method: 'POST', body: JSON.stringify(input) }),
  updateProblem: (id: number, input: ProblemInput) =>
    request<Problem>(`/problems/${id}`, { method: 'PUT', body: JSON.stringify(input) }),
  deleteProblem: (id: number) => request<void>(`/problems/${id}`, { method: 'DELETE' }),
  run: (id: number, code: string) => request<JudgeResult>(`/problems/${id}/run`, { method: 'POST', body: JSON.stringify({ code }) }),
  submit: (id: number, code: string) =>
    request<Submission>(`/problems/${id}/submit`, { method: 'POST', body: JSON.stringify({ code }) }),
  listSubmissions: (id: number) => request<Submission[]>(`/problems/${id}/submissions`),
  getSubmission: (id: number) => request<Submission>(`/submissions/${id}`),
}

export const verdictLabel: Record<Verdict, string> = {
  Accepted: 'Accepted',
  WrongAnswer: 'Wrong Answer',
  CompileError: 'Compile Error',
  RuntimeError: 'Runtime Error',
  TimeLimitExceeded: 'Time Limit Exceeded',
  JudgeError: 'Judge Error',
}
