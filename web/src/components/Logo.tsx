export function Logo({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 32 32" className={className} aria-hidden="true">
      <path d="M9 9a3 3 0 0 1 3-3h8a3 3 0 0 1 3 3v8H9z" fill="#e11d48" />
      <rect x="6" y="15" width="20" height="6" rx="2" fill="#f43f5e" />
      <rect x="8" y="21" width="3" height="5" rx="1" fill="#fbbf24" />
      <rect x="21" y="21" width="3" height="5" rx="1" fill="#fbbf24" />
    </svg>
  )
}
