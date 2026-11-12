const money = new Intl.NumberFormat('es-CR', {
  style: 'currency',
  currency: 'CRC',
  minimumFractionDigits: 0,
  maximumFractionDigits: 2,
})

/** 70000 -> "₡70 000" (Costa Rica format). */
export function formatMoney(amount: number): string {
  return money.format(amount)
}

// Loan and due dates are calendar days that the API sends as midnight UTC. They are shown in UTC
// on purpose: in Costa Rica's time zone (UTC-6) midnight UTC is still the previous day.
const calendarDay = new Intl.DateTimeFormat('es-CR', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  timeZone: 'UTC',
})

/** "2026-11-30T00:00:00Z" -> "30 nov 2026" */
export function formatDate(isoDate: string): string {
  return calendarDay.format(new Date(isoDate))
}

/** This month on the phone's calendar as "yyyy-MM", for the summary. */
export function currentMonth(): string {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
}

/** How many days ago a calendar day was (0 = today). Used for "venció hace 3 días". */
export function daysSince(isoDate: string): number {
  const now = new Date()
  const today = Date.UTC(now.getFullYear(), now.getMonth(), now.getDate())
  const day = new Date(isoDate)
  const then = Date.UTC(day.getUTCFullYear(), day.getUTCMonth(), day.getUTCDate())
  return Math.round((today - then) / 86_400_000)
}

/** First letter of a name, for the round avatars. */
export function initial(name: string | null | undefined): string {
  return (name?.trim().charAt(0) || '?').toUpperCase()
}

/** "2026-11" -> "Noviembre 2026" */
export function formatMonth(month: string): string {
  const [year, monthNumber] = month.split('-').map(Number)
  const text = new Intl.DateTimeFormat('es-CR', { month: 'long', timeZone: 'UTC' }).format(
    new Date(Date.UTC(year, monthNumber - 1, 1)),
  )
  return `${text.charAt(0).toUpperCase()}${text.slice(1)} ${year}`
}

/** shiftMonth("2026-01", -1) -> "2025-12" */
export function shiftMonth(month: string, delta: number): string {
  const [year, monthNumber] = month.split('-').map(Number)
  const date = new Date(Date.UTC(year, monthNumber - 1 + delta, 1))
  return `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, '0')}`
}

/** Today on the phone's calendar as "yyyy-MM-dd" (the format of <input type="date">). */
export function todayIso(): string {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`
}

/** addDaysIso("2026-11-01", 15) -> "2026-11-16" */
export function addDaysIso(isoDay: string, days: number): string {
  const date = new Date(`${isoDay}T00:00:00Z`)
  date.setUTCDate(date.getUTCDate() + days)
  return date.toISOString().slice(0, 10)
}

/** "2026-11" -> "nov" ("ene 27" in January, so the year change is visible). */
export function formatMonthShort(month: string): string {
  const [year, monthNumber] = month.split('-').map(Number)
  const text = new Intl.DateTimeFormat('es-CR', { month: 'short', timeZone: 'UTC' })
    .format(new Date(Date.UTC(year, monthNumber - 1, 1)))
    .replace('.', '')
  return monthNumber === 1 ? `${text} ${String(year).slice(2)}` : text
}
