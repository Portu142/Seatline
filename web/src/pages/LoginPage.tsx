import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Navigate, useLocation, useNavigate } from 'react-router'
import { z } from 'zod'
import { useAuth } from '../auth/useAuth'
import { ApiError } from '../lib/api'

const loginSchema = z.object({
  email: z.email('Enter a valid email address'),
  password: z.string().min(1, 'Enter your password'),
})

type LoginForm = z.infer<typeof loginSchema>

const DEMO_PASSWORD: string = import.meta.env.VITE_DEMO_PASSWORD ?? 'Seatline2026!'

const demoAccounts = [
  { label: 'Customer', email: 'customer@seatline.dev', hint: 'buys tickets' },
  { label: 'Box office', email: 'admin@seatline.dev', hint: 'manages shows' },
  { label: 'Door staff', email: 'validator@seatline.dev', hint: 'scans tickets' },
]

export function LoginPage() {
  const { status, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [serverError, setServerError] = useState<string | null>(null)
  const from = (location.state as { from?: string } | null)?.from ?? '/account'

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({ resolver: zodResolver(loginSchema), defaultValues: { email: '', password: '' } })

  if (status === 'authenticated') return <Navigate to={from} replace />

  const onSubmit = async ({ email, password }: LoginForm) => {
    setServerError(null)
    try {
      await login(email, password)
      navigate(from, { replace: true })
    } catch (error) {
      setServerError(error instanceof ApiError ? error.message : 'Could not reach the server. Is the API running?')
    }
  }

  const fillDemo = (email: string) => {
    setValue('email', email, { shouldValidate: true })
    setValue('password', DEMO_PASSWORD, { shouldValidate: true })
  }

  const inputClass =
    'mt-1 block w-full rounded-lg border border-ink-700 bg-ink-950 px-3 py-2 text-white placeholder:text-ink-400 focus:border-gold-400 focus:outline-none'

  return (
    <div className="mx-auto max-w-md py-16">
      <h1 className="text-3xl font-bold text-white">Sign in</h1>
      <p className="mt-2 text-ink-400">Use your account or one of the demo accounts below.</p>

      <form onSubmit={handleSubmit(onSubmit)} noValidate className="mt-8 space-y-5">
        <div>
          <label htmlFor="email" className="text-sm font-medium">
            Email
          </label>
          <input
            id="email"
            type="email"
            autoComplete="email"
            className={inputClass}
            aria-invalid={!!errors.email}
            aria-describedby={errors.email ? 'email-error' : undefined}
            {...register('email')}
          />
          {errors.email && (
            <p id="email-error" className="mt-1 text-sm text-velvet-400">
              {errors.email.message}
            </p>
          )}
        </div>

        <div>
          <label htmlFor="password" className="text-sm font-medium">
            Password
          </label>
          <input
            id="password"
            type="password"
            autoComplete="current-password"
            className={inputClass}
            aria-invalid={!!errors.password}
            aria-describedby={errors.password ? 'password-error' : undefined}
            {...register('password')}
          />
          {errors.password && (
            <p id="password-error" className="mt-1 text-sm text-velvet-400">
              {errors.password.message}
            </p>
          )}
        </div>

        {serverError && (
          <p
            role="alert"
            className="rounded-lg border border-velvet-600/50 bg-velvet-600/10 px-3 py-2 text-sm text-velvet-400"
          >
            {serverError}
          </p>
        )}

        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded-lg bg-velvet-500 px-4 py-2.5 font-semibold text-white hover:bg-velvet-600 disabled:opacity-60"
        >
          {isSubmitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>

      <section aria-labelledby="demo-accounts" className="mt-10 rounded-xl border border-ink-800 bg-ink-900 p-5">
        <h2 id="demo-accounts" className="text-sm font-semibold text-white">
          Demo accounts
        </h2>
        <p className="mt-1 text-xs text-ink-400">Fills in the form. Password for all three: {DEMO_PASSWORD}</p>
        <div className="mt-4 grid grid-cols-3 gap-2">
          {demoAccounts.map((account) => (
            <button
              key={account.email}
              type="button"
              onClick={() => fillDemo(account.email)}
              className="rounded-lg border border-ink-700 px-2 py-2 text-left hover:border-gold-400"
            >
              <span className="block text-sm font-medium text-white">{account.label}</span>
              <span className="block text-xs text-ink-400">{account.hint}</span>
            </button>
          ))}
        </div>
      </section>
    </div>
  )
}
