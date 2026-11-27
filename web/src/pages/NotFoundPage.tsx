import { Link } from 'react-router'

export function NotFoundPage() {
  return (
    <section className="py-24 text-center">
      <p className="text-sm font-semibold tracking-widest text-velvet-400 uppercase">404</p>
      <h1 className="mt-2 text-2xl font-bold text-white">This row doesn&apos;t exist</h1>
      <Link to="/" className="mt-6 inline-block text-gold-400 underline-offset-4 hover:underline">
        Back to the lobby
      </Link>
    </section>
  )
}
