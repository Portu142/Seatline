import { Link } from 'react-router'
import { useAuth } from '../auth/useAuth'
import { SeatGridPreview } from '../components/SeatGridPreview'

const features = [
  {
    title: 'No double bookings',
    body: 'Every seat of every show is a unique row in PostgreSQL. Two people clicking the same seat in the same millisecond: exactly one wins.',
  },
  {
    title: 'Live seat map',
    body: '200 seats in 10 rows. Seats held by others turn grey on every screen instantly, and holds expire after 10 minutes.',
  },
  {
    title: 'Tickets you cannot fake',
    body: 'Each QR code carries an HMAC signature. Door staff scan it once; a copy or a forgery is rejected.',
  },
]

export function HomePage() {
  const { user } = useAuth()

  return (
    <div className="py-16">
      <section className="flex flex-col items-center text-center">
        <p className="text-sm font-semibold tracking-widest text-gold-400 uppercase">Cinema tickets, done right</p>
        <h1 className="mt-3 text-5xl font-extrabold tracking-tight text-white sm:text-6xl">Seatline</h1>
        <p className="mt-4 max-w-xl text-lg text-ink-400">
          Pick your seat on a live map of the hall, pay in seconds and walk in with a tamper-proof QR ticket.
        </p>
        <div className="mt-8 flex gap-3">
          {user ? (
            <Link
              to="/account"
              className="rounded-lg bg-velvet-500 px-5 py-2.5 font-semibold text-white hover:bg-velvet-600"
            >
              Go to my account
            </Link>
          ) : (
            <Link
              to="/login"
              className="rounded-lg bg-velvet-500 px-5 py-2.5 font-semibold text-white hover:bg-velvet-600"
            >
              Try the demo
            </Link>
          )}
        </div>
        <div className="mt-14 w-full rounded-2xl border border-ink-800 bg-ink-900 p-6 sm:p-10">
          <div className="flex justify-center">
            <SeatGridPreview />
          </div>
          <p className="mt-4 text-xs text-ink-400">Interactive seat selection is coming in the next release.</p>
        </div>
      </section>

      <section aria-labelledby="why" className="mt-16">
        <h2 id="why" className="sr-only">
          Why Seatline
        </h2>
        <ul className="grid gap-4 sm:grid-cols-3">
          {features.map((f) => (
            <li key={f.title} className="rounded-xl border border-ink-800 bg-ink-900 p-5">
              <h3 className="font-semibold text-white">{f.title}</h3>
              <p className="mt-2 text-sm leading-relaxed text-ink-400">{f.body}</p>
            </li>
          ))}
        </ul>
      </section>
    </div>
  )
}
