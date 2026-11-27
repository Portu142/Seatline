import { useQuery } from '@tanstack/react-query'
import { apiFetch, type AreaResponse } from '../lib/api'

export function AdminPage() {
  const overview = useQuery({
    queryKey: ['admin-overview'],
    queryFn: () => apiFetch<AreaResponse>('/api/admin/overview'),
  })

  return (
    <div className="mx-auto max-w-2xl py-16">
      <p className="text-sm font-semibold tracking-widest text-gold-400 uppercase">Admin only</p>
      <h1 className="mt-2 text-3xl font-bold text-white">Box office</h1>

      {overview.isPending && (
        <p role="status" className="mt-6 text-ink-400">
          Checking access…
        </p>
      )}
      {overview.isError && (
        <p role="alert" className="mt-6 text-velvet-400">
          {overview.error.message}
        </p>
      )}
      {overview.data && (
        <div className="mt-8 rounded-xl border border-ink-800 bg-ink-900 p-5">
          <p className="font-medium text-white">{overview.data.message}</p>
          <p className="mt-1 text-sm text-ink-400">
            The API confirmed your admin role for {overview.data.user}. Shows, sales and occupancy will live here.
          </p>
        </div>
      )}
    </div>
  )
}
