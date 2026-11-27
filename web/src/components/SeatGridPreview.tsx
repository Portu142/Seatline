const ROWS = 'ABCDEFGHIJ'.split('')
const SEATS_PER_ROW = 20

// A few seats shown as taken / selected so the preview reads as a real seat map.
const TAKEN = new Set(['C7', 'C8', 'D12', 'E3', 'E4', 'E5', 'F14', 'F15', 'G10', 'H1', 'H2', 'I18'])
const SELECTED = new Set(['E10', 'E11'])

/** Decorative preview of the 200-seat hall (10 rows × 20 seats). The interactive map arrives in block 3. */
export function SeatGridPreview() {
  const size = 14
  const gap = 4
  const aisle = 12
  const width = SEATS_PER_ROW * (size + gap) + aisle
  const height = ROWS.length * (size + gap) + 36

  return (
    <svg
      viewBox={`0 0 ${width} ${height}`}
      className="w-full max-w-xl"
      role="img"
      aria-label="Preview of the 200-seat hall"
    >
      <path
        d={`M 20 10 Q ${width / 2} -4 ${width - 20} 10`}
        stroke="#fbbf24"
        strokeWidth="2"
        fill="none"
        opacity="0.7"
      />
      <text x={width / 2} y="24" textAnchor="middle" fontSize="8" fill="#8b8ba3" letterSpacing="3">
        SCREEN
      </text>
      {ROWS.map((row, r) =>
        Array.from({ length: SEATS_PER_ROW }, (_, s) => {
          const id = `${row}${s + 1}`
          const x = s * (size + gap) + (s >= SEATS_PER_ROW / 2 ? aisle : 0)
          const y = 36 + r * (size + gap)
          const fill = SELECTED.has(id) ? '#fbbf24' : TAKEN.has(id) ? '#2a2a38' : '#be123c'
          return <rect key={id} x={x} y={y} width={size} height={size - 2} rx="3" fill={fill} />
        }),
      )}
    </svg>
  )
}
