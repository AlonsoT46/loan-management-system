import type { LoanSummary } from '../api/types'

export const DEFAULT_RATE_PERCENT = 20
const DAYS_PER_QUINCENA = 15

export interface LoanEstimate {
  quincenas: number
  interest: number
  total: number
}

/**
 * Preview of a new loan while filling the form: 20% of the principal per quincena (15 days),
 * quincenas rounded up, minimum 1. It repeats the rule of Loan.Create in the backend only to show
 * the numbers before saving; the backend calculates the real values. If the rule changes, change both.
 */
export function estimateLoan(principal: number, loanDate: string, dueDate: string): LoanEstimate | null {
  if (!(principal > 0) || !loanDate || !dueDate)
    return null

  const days = Math.round((Date.parse(dueDate) - Date.parse(loanDate)) / 86_400_000)
  if (Number.isNaN(days) || days < 0)
    return null

  const quincenas = Math.max(1, Math.ceil(days / DAYS_PER_QUINCENA))
  const interest = Math.round(principal * (DEFAULT_RATE_PERCENT / 100) * quincenas * 100) / 100
  return { quincenas, interest, total: principal + interest }
}

export type StatusTone = 'loss' | 'gain' | 'muted' | 'ink'

/** The word and color used everywhere for a loan's situation. */
export function loanStatus(loan: LoanSummary): { label: string; tone: StatusTone } {
  if (loan.isOverdue)
    return { label: 'Vencido', tone: 'loss' }
  if (loan.status === 'Paid')
    return { label: 'Pagado', tone: 'gain' }
  if (loan.status === 'Cancelled')
    return { label: 'Cancelado', tone: 'muted' }
  if (loan.status === 'Archived')
    return { label: 'Archivado', tone: 'muted' }
  return { label: 'Activo', tone: 'ink' }
}

export const toneClass: Record<StatusTone, string> = {
  loss: 'text-loss',
  gain: 'text-gain',
  muted: 'text-muted',
  ink: 'text-ink',
}
