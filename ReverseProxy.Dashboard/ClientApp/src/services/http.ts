interface AntiforgeryToken {
  requestToken: string
  headerName: string
}

interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  errorCode?: string
}

interface ApiRequestOptions extends Omit<RequestInit, 'body'> {
  body?: unknown
  csrf?: boolean
}

export interface ApiResult<T> {
  data: T
  response: Response
}

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly code?: string
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

let antiforgeryToken: AntiforgeryToken | null = null

export function resetAntiforgeryToken() {
  antiforgeryToken = null
}

async function getAntiforgeryToken(): Promise<AntiforgeryToken> {
  if (antiforgeryToken) return antiforgeryToken

  const response = await fetch('/api/v1/auth/antiforgery', { credentials: 'include' })
  if (!response.ok) throw new ApiError('antiforgery_unavailable', response.status, 'antiforgery_unavailable')
  antiforgeryToken = await response.json() as AntiforgeryToken
  return antiforgeryToken
}

export async function apiRequest<T>(path: string, options: ApiRequestOptions = {}): Promise<ApiResult<T>> {
  const headers = new Headers(options.headers)
  const body = options.body === undefined ? undefined : JSON.stringify(options.body)
  if (body !== undefined) headers.set('Content-Type', 'application/json')

  if (options.csrf) {
    const token = await getAntiforgeryToken()
    headers.set(token.headerName, token.requestToken)
  }

  const response = await fetch(path, {
    ...options,
    headers,
    body,
    credentials: 'include'
  })

  if (!response.ok) {
    let problem: ProblemDetails = {}
    try {
      problem = await response.json() as ProblemDetails
    } catch {
      problem = {}
    }
    throw new ApiError(problem.detail ?? problem.title ?? `http_${response.status}`, response.status, problem.errorCode)
  }

  const data = response.status === 204 ? undefined : await response.json()
  return { data: data as T, response }
}