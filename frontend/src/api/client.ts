const BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '')

/** RFC 9457 problem details, as returned by the API for every error. */
interface ProblemDetails {
  title?: string
  status?: number
  detail?: string
  /** Stable business error code, e.g. "event_full". */
  code?: string
  /** Model-validation errors keyed by property name. */
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly code?: string
  /** Field errors keyed by camelCase property name (e.g. "maxCapacity"). */
  readonly fieldErrors: Record<string, string>

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.code = problem.code
    this.fieldErrors = normalizeFieldErrors(problem.errors)
  }
}

// ASP.NET Core reports keys as "Title" (model binding) or "$.date" (JSON parsing).
function normalizeFieldErrors(errors: ProblemDetails['errors']): Record<string, string> {
  const result: Record<string, string> = {}
  for (const [key, messages] of Object.entries(errors ?? {})) {
    const name = key.replace(/^\$\./, '')
    if (!name || !messages.length) continue
    result[name.charAt(0).toLowerCase() + name.slice(1)] = messages[0]
  }
  return result
}

export async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${BASE_URL}${path}`, {
      ...init,
      headers: {
        Accept: 'application/json',
        ...(init.body ? { 'Content-Type': 'application/json' } : {}),
        ...init.headers,
      },
    })
  } catch {
    throw new ApiError(0, { detail: 'Unable to reach the server. Is the API running?' })
  }

  if (response.status === 204) return undefined as T

  const body = await response.json().catch(() => ({}))
  if (!response.ok) throw new ApiError(response.status, body as ProblemDetails)
  return body as T
}

export const json = (data: unknown) => JSON.stringify(data)
