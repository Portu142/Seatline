export const API_URL: string = import.meta.env.VITE_API_URL ?? 'http://localhost:8080'

export type Role = 'customer' | 'admin' | 'validator'

export interface User {
  id: string
  email: string
  displayName: string
  roles: Role[]
}

export interface AuthResponse {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
  user: User
}

export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

const REFRESH_KEY = 'seatline.refreshToken'

/**
 * Token storage. The short-lived access token lives only in memory; the refresh token is kept
 * in localStorage so a reload keeps you signed in. (Moving it to an httpOnly cookie is on the
 * hardening list once the API and web share a domain.)
 */
let accessToken: string | null = null
let refreshInFlight: Promise<AuthResponse | null> | null = null
let onSessionChange: (session: AuthResponse | null) => void = () => {}

export const tokens = {
  get refreshToken() {
    return localStorage.getItem(REFRESH_KEY)
  },
  store(session: AuthResponse) {
    accessToken = session.accessToken
    localStorage.setItem(REFRESH_KEY, session.refreshToken)
  },
  clear() {
    accessToken = null
    localStorage.removeItem(REFRESH_KEY)
  },
  subscribe(listener: (session: AuthResponse | null) => void) {
    onSessionChange = listener
  },
}

async function parseError(response: Response): Promise<ApiError> {
  let message = response.statusText || 'Request failed'
  try {
    const body = (await response.json()) as { title?: string; detail?: string }
    message = body.detail ?? body.title ?? message
  } catch {
    // Body was not JSON (e.g. 429 from the rate limiter); keep the status text.
  }
  if (response.status === 429) message = 'Too many attempts. Wait a minute and try again.'
  return new ApiError(response.status, message)
}

async function rawRequest<T>(path: string, init: RequestInit = {}, token?: string | null): Promise<T> {
  const headers = new Headers(init.headers)
  if (init.body !== undefined) headers.set('Content-Type', 'application/json')
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const response = await fetch(`${API_URL}${path}`, { ...init, headers })
  if (!response.ok) throw await parseError(response)
  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

/** Exchanges the stored refresh token for a new pair. Concurrent callers share one request. */
export function refreshSession(): Promise<AuthResponse | null> {
  const refreshToken = tokens.refreshToken
  if (!refreshToken) return Promise.resolve(null)

  refreshInFlight ??= rawRequest<AuthResponse>('/api/auth/refresh', {
    method: 'POST',
    body: JSON.stringify({ refreshToken }),
  })
    .then((session) => {
      tokens.store(session)
      onSessionChange(session)
      return session
    })
    .catch(() => {
      tokens.clear()
      onSessionChange(null)
      return null
    })
    .finally(() => {
      refreshInFlight = null
    })

  return refreshInFlight
}

/** Authenticated request: retries once with a refreshed access token on 401. */
export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  try {
    return await rawRequest<T>(path, init, accessToken)
  } catch (error) {
    if (!(error instanceof ApiError) || error.status !== 401 || !tokens.refreshToken) throw error
    const session = await refreshSession()
    if (!session) throw error
    return rawRequest<T>(path, init, session.accessToken)
  }
}

export const authApi = {
  login: (email: string, password: string) =>
    rawRequest<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
  logout: (refreshToken: string) =>
    rawRequest<void>('/api/auth/logout', {
      method: 'POST',
      body: JSON.stringify({ refreshToken }),
    }),
  me: () => apiFetch<User>('/api/auth/me'),
}

export interface AreaResponse {
  area: string
  message: string
  user: string
}
