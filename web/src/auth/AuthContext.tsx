import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { authApi, refreshSession, tokens, type AuthResponse, type Role, type User } from '../lib/api'
import { AuthContext, type AuthStatus } from './context'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [status, setStatus] = useState<AuthStatus>(tokens.refreshToken ? 'loading' : 'anonymous')

  const applySession = useCallback((session: AuthResponse | null) => {
    setUser(session?.user ?? null)
    setStatus(session ? 'authenticated' : 'anonymous')
  }, [])

  // Restore the session after a reload using the stored refresh token.
  useEffect(() => {
    tokens.subscribe(applySession)
    if (tokens.refreshToken) void refreshSession().then(applySession)
  }, [applySession])

  const login = useCallback(
    async (email: string, password: string) => {
      const session = await authApi.login(email, password)
      tokens.store(session)
      applySession(session)
      return session.user
    },
    [applySession],
  )

  const logout = useCallback(async () => {
    const refreshToken = tokens.refreshToken
    tokens.clear()
    applySession(null)
    if (refreshToken) await authApi.logout(refreshToken).catch(() => undefined)
  }, [applySession])

  const hasRole = useCallback((...roles: Role[]) => !!user && roles.some((r) => user.roles.includes(r)), [user])

  const value = useMemo(() => ({ status, user, login, logout, hasRole }), [status, user, login, logout, hasRole])

  return <AuthContext value={value}>{children}</AuthContext>
}
