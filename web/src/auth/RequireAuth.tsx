import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router'
import type { Role } from '../lib/api'
import { useAuth } from './useAuth'

interface Props {
  children: ReactNode
  /** If set, the user needs at least one of these roles. */
  roles?: Role[]
}

export function RequireAuth({ children, roles }: Props) {
  const { status, hasRole } = useAuth()
  const location = useLocation()

  if (status === 'loading') {
    return (
      <p role="status" className="py-24 text-center text-ink-400">
        Restoring your session…
      </p>
    )
  }

  if (status === 'anonymous') {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  if (roles && !hasRole(...roles)) {
    return (
      <section className="mx-auto max-w-md py-24 text-center">
        <p className="text-sm font-semibold tracking-widest text-velvet-400 uppercase">403</p>
        <h1 className="mt-2 text-2xl font-bold text-white">This area is for staff only</h1>
        <p className="mt-3 text-ink-400">Your account doesn&apos;t have the {roles.join(' or ')} role.</p>
      </section>
    )
  }

  return children
}
