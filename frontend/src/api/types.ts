// The same shapes as the backend DTOs. ASP.NET sends them in camelCase;
// Guids and dates arrive as text, decimals arrive as numbers.

/** Archived = given up for lost: it no longer counts as money to collect. */
export type LoanStatus = 'Active' | 'Paid' | 'Cancelled' | 'Archived'

/** LoginResponse */
export interface LoginResponse {
  token: string
  expiresAtUtc: string
}

/** LoanSummaryDto: a loan without its payments, used for lists. */
export interface LoanSummary {
  id: string
  clientId: string
  clientName: string
  principalAmount: number
  interestRatePerQuincena: number
  quincenas: number
  interestAmount: number
  totalAmount: number
  loanDate: string
  dueDate: string
  totalPaid: number
  remainingBalance: number
  status: LoanStatus
  isOverdue: boolean
  notes: string | null
}

/** PaymentDto */
export interface Payment {
  id: string
  paymentDate: string
  amount: number
  notes: string | null
  principalPaid: number
  interestPaid: number
  isVoided: boolean
  voidedAtUtc: string | null
  voidReason: string | null
}

/** LoanDetailDto: a loan with its payments. */
export interface LoanDetail {
  loan: LoanSummary
  payments: Payment[]
}

/** CreateLoanRequest. Dates as "yyyy-MM-dd"; without interest rate the backend uses 20%. */
export interface CreateLoanRequest {
  clientId: string
  principalAmount: number
  loanDate: string
  dueDate: string
  notes: string | null
}

/** SummaryDto: totals for a month (lent, collected) and for today (balances). */
export interface Summary {
  month: string | null
  totalLent: number
  expectedInterest: number
  expectedTotal: number
  principalRecovered: number
  interestCollected: number
  totalCollected: number
  outstandingBalance: number
  activeLoans: number
  overdueLoans: number
  overdueBalance: number
  /** Active loans due from today to the end of this month (overdue ones not included). */
  dueThisMonth: number
  dueThisMonthLoans: number
  archivedLoans: number
  /** What archived loans still owed: money given up for lost. */
  archivedBalance: number
}

/** ClientDto */
export interface Client {
  id: string
  name: string
  phone: string | null
  notes: string | null
  isActive: boolean
  createdAtUtc: string
}

/** CreateClientRequest */
export interface CreateClientRequest {
  name: string
  phone: string | null
  notes: string | null
}

/** CreateClientResult: the new client, plus other clients that already had the same name. */
export interface CreateClientResult {
  client: Client
  possibleDuplicates: Client[]
}

/** RegisterPaymentRequest. paymentDate as "yyyy-MM-dd". */
export interface RegisterPaymentRequest {
  amount: number
  paymentDate: string
  notes: string | null
}

/** MonthlyPointDto: one month of the charts. */
export interface MonthlyPoint {
  month: string
  lent: number
  principalStillOut: number
  principalWrittenOff: number
  principalCollected: number
  interestCollected: number
}

/** UpdateLoanRequest. Client, amount and dates only change while the loan has no payments. */
export interface UpdateLoanRequest {
  clientId: string
  principalAmount: number
  loanDate: string
  dueDate: string
  notes: string | null
}
