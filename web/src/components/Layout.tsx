import { Link, NavLink, Outlet, useNavigate } from 'react-router'
import { useAuth } from '../auth/useAuth'
import { Logo } from './Logo'

const navLink = ({ isActive }: { isActive: boolean }) =>
  `rounded-md px-3 py-2 text-sm font-medium transition-colors ${
    isActive ? 'bg-ink-800 text-white' : 'text-ink-400 hover:text-white'
  }`

export function Layout() {
  const { user, hasRole, logout } = useAuth()
  const navigate = useNavigate()

  return (
    <div className="flex min-h-screen flex-col">
      <a href="#main" className="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-10">
        Skip to content
      </a>
      <header className="border-b border-ink-800">
        <nav aria-label="Main" className="mx-auto flex max-w-5xl items-center gap-2 px-4 py-3">
          <Link to="/" className="mr-auto flex items-center gap-2 text-lg font-bold tracking-tight text-white">
            <Logo className="size-7" />
            Seatline
          </Link>
          {user ? (
            <>
              <NavLink to="/account" className={navLink}>
                My account
              </NavLink>
              {hasRole('admin') && (
                <NavLink to="/admin" className={navLink}>
                  Box office
                </NavLink>
              )}
              <button
                type="button"
                onClick={() => void logout().then(() => navigate('/'))}
                className="rounded-md px-3 py-2 text-sm font-medium text-ink-400 hover:text-white"
              >
                Sign out
              </button>
            </>
          ) : (
            <NavLink to="/login" className={navLink}>
              Sign in
            </NavLink>
          )}
        </nav>
      </header>

      <main id="main" className="mx-auto w-full max-w-5xl flex-1 px-4">
        <Outlet />
      </main>

      <footer className="border-t border-ink-800 py-6 text-center text-xs text-ink-400">
        Seatline is a portfolio project. Payments run in Stripe test mode, so no real money moves.
      </footer>
    </div>
  )
}
