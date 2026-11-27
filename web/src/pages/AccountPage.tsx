import { useQuery } from '@tanstack/react-query'
import { authApi, apiFetch, type AreaResponse } from '../lib/api'
import { useAuth } from '../auth/useAuth'

const roleLabels: Record<string, string> = {
  customer: 'Customer',
  admin: 'Box office (admin)',
  validator: 'Door staff (validator)',
}

export function AccountPage() {
  const { hasRole } = useAuth()
  const me = useQuery({ queryKey: ['me'], queryFn: authApi.me })
  const door = useQuery({
    queryKey: ['door-status'],
    queryFn: () => apiFetch<AreaResponse>('/api/door/status'),
    enabled: hasRole('validator', 'admin'),
  })

  return (
    <div className="mx-auto max-w-2xl py-16">
      <h1 className="text-3xl font-bold text-white">My account</h1>

      {me.isPending && (
        <p role="status" className="mt-6 text-ink-400">
          Loading your profile…
        </p>
      )}
      {me.isError && (
        <p role="alert" className="mt-6 text-velvet-400">
          Could not load your profile: {me.error.message}
        </p>
      )}

      {me.data && (
        <dl className="mt-8 divide-y divide-ink-800 rounded-xl border border-ink-800 bg-ink-900">
          <div className="flex justify-between gap-4 px-5 py-4">
            <dt className="text-ink-400">Name</dt>
            <dd className="font-medium text-white">{me.data.displayName}</dd>
          </div>
          <div className="flex justify-between gap-4 px-5 py-4">
            <dt className="text-ink-400">Email</dt>
            <dd className="font-medium text-white">{me.data.email}</dd>
          </div>
          <div className="flex justify-between gap-4 px-5 py-4">
            <dt className="text-ink-400">Role</dt>
            <dd className="flex flex-wrap justify-end gap-2">
              {me.data.roles.map((role) => (
                <span key={role} className="rounded-full bg-ink-800 px-3 py-1 text-xs font-medium text-gold-400">
                  {roleLabels[role] ?? role}
                </span>
              ))}
            </dd>
          </div>
        </dl>
      )}

      {door.data && (
        <p className="mt-6 rounded-xl border border-ink-800 bg-ink-900 px-5 py-4 text-sm">
          <span className="font-semibold text-white">Door area:</span> {door.data.message}
        </p>
      )}

      <p className="mt-8 text-sm text-ink-400">Your tickets will show up here once ticket sales open.</p>
    </div>
  )
}
