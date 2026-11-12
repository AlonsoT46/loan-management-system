import type {
  Client,
  CreateClientRequest,
  CreateClientResult,
  CreateLoanRequest,
  LoanDetail,
  LoanSummary,
  LoanStatus,
  LoginResponse,
  MonthlyPoint,
  RegisterPaymentRequest,
  UpdateLoanRequest,
  Summary,
} from './types'

// The only place that talks to the backend, like the repositories are the only place that talks to MySQL.

const baseUrl: string = import.meta.env.VITE_API_URL ?? ''

/** After this long without an answer, stop waiting (fetch alone would wait for minutes). */
const timeoutMs = Number(import.meta.env.VITE_API_TIMEOUT_MS) || 15_000
const TOKEN_KEY = 'control-de-dinero.token'

/** An error answered by the API (or a connection problem, with status 0). */
export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

/** The token lives in the browser, so the app stays logged in after closing it. */
export const tokenStorage = {
  get(): string | null {
    try {
      return localStorage.getItem(TOKEN_KEY)
    } catch {
      return null
    }
  },
  set(token: string) {
    try {
      localStorage.setItem(TOKEN_KEY, token)
    } catch {
      // Private mode or storage blocked: the session just will not survive a reload.
    }
  },
  clear() {
    try {
      localStorage.removeItem(TOKEN_KEY)
    } catch {
      // Nothing to clear.
    }
  },
}

// Who to tell when the API says the session is no longer valid (AuthProvider registers itself).
let unauthorizedHandler: (() => void) | null = null

export function onUnauthorized(handler: (() => void) | null) {
  unauthorizedHandler = handler
}

interface FetchOptions {
  /** For login: a 401 there means wrong credentials, not an expired session. */
  anonymous?: boolean
}

async function apiFetch<T>(path: string, init: RequestInit = {}, options: FetchOptions = {}): Promise<T> {
  const headers = new Headers(init.headers)

  const token = tokenStorage.get()
  if (token && !options.anonymous)
    headers.set('Authorization', `Bearer ${token}`)

  if (init.body && !headers.has('Content-Type'))
    headers.set('Content-Type', 'application/json')

  // AbortController = a "cancel" button for fetch; the timer presses it if the answer takes too long.
  const controller = new AbortController()
  const timer = setTimeout(() => controller.abort(), timeoutMs)

  let response: Response
  try {
    response = await fetch(`${baseUrl}${path}`, { ...init, headers, signal: controller.signal })
  } catch {
    // fetch only fails like this when no answer arrived at all. Say why, as precisely as we can.
    if (controller.signal.aborted)
      throw new ApiError(0, 'El servidor está tardando demasiado. Revisa tu conexión e intenta de nuevo.')
    if (!navigator.onLine)
      throw new ApiError(0, 'No tienes conexión a internet. Revisa el wifi o los datos e intenta de nuevo.')
    throw new ApiError(0, 'No se pudo conectar con el servidor. Intenta de nuevo en un momento.')
  } finally {
    clearTimeout(timer)
  }

  if (response.status === 401 && !options.anonymous) {
    unauthorizedHandler?.()
    throw new ApiError(401, 'Tu sesión terminó. Vuelve a iniciar sesión.')
  }

  if (response.status === 404)
    throw new ApiError(404, 'No se encontró lo que buscabas.')

  if (!response.ok)
    throw new ApiError(response.status, await readProblem(response))

  if (response.status === 204)
    return undefined as T

  return (await response.json()) as T
}

/** The backend answers errors as ProblemDetails: the message is in "detail" or "title". */
async function readProblem(response: Response): Promise<string> {
  try {
    const body = await response.json()
    return translate(body.detail ?? body.title ?? `Error ${response.status}`)
  } catch {
    return `Error ${response.status}`
  }
}

// The backend writes its messages in English. These are the ones a user can actually meet.
// startsWith because some end with " (Parameter 'x')".
const spanishMessages: [string, string][] = [
  ['Payment exceeds the remaining balance', 'El pago es mayor que lo que falta por pagar.'],
  ['Only active loans accept payments', 'Este préstamo ya no recibe pagos.'],
  ['Payment must be greater than zero', 'El pago debe ser mayor que cero.'],
  ['Payment date cannot be before the loan date', 'La fecha del pago no puede ser antes del préstamo.'],
  ['The payment is already voided', 'Ese pago ya estaba anulado.'],
  ['A cancelled loan cannot be changed', 'Un préstamo cancelado no se puede cambiar.'],
  ['Principal must be greater than zero', 'El monto debe ser mayor que cero.'],
  ['Due date cannot be before the loan date', 'La fecha límite no puede ser antes del préstamo.'],
  ['Due date is required', 'Falta la fecha límite.'],
  ['Client not found', 'No se encontró el cliente.'],
  ['The client is inactive', 'Ese cliente está desactivado.'],
  ['Name is required', 'Falta el nombre.'],
  ['Only active loans can be archived', 'Solo se puede archivar un préstamo activo.'],
  ['Only archived loans can be unarchived', 'Ese préstamo no está archivado.'],
  ['A loan with payments cannot change its amounts or dates', 'Este préstamo ya tiene pagos: para cambiar el monto o las fechas, primero anula los pagos.'],
  ['Only active loans can be edited', 'Solo se puede corregir un préstamo activo.'],
]

function translate(message: string): string {
  return spanishMessages.find(([english]) => message.startsWith(english))?.[1] ?? message
}

/** One function per endpoint the screens use. */
export const api = {
  login(email: string, password: string) {
    return apiFetch<LoginResponse>(
      '/api/auth/login',
      { method: 'POST', body: JSON.stringify({ email, password }) },
      { anonymous: true },
    )
  },

  getLoans(filters: { onlyOverdue?: boolean; clientId?: string; status?: LoanStatus } = {}) {
    const query = new URLSearchParams()
    if (filters.onlyOverdue)
      query.set('onlyOverdue', 'true')
    if (filters.status)
      query.set('status', filters.status)
    if (filters.clientId)
      query.set('clientId', filters.clientId)

    const text = query.toString()
    return apiFetch<LoanSummary[]>(`/api/loans${text ? `?${text}` : ''}`)
  },

  getLoan(id: string) {
    return apiFetch<LoanDetail>(`/api/loans/${encodeURIComponent(id)}`)
  },

  createLoan(request: CreateLoanRequest) {
    return apiFetch<LoanDetail>('/api/loans', { method: 'POST', body: JSON.stringify(request) })
  },

  /** Correct a loan. Amount, dates and client only while it has no payments; notes always. */
  updateLoan(loanId: string, request: UpdateLoanRequest) {
    return apiFetch<LoanDetail>(`/api/loans/${encodeURIComponent(loanId)}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    })
  },

  /** The backend refuses more than what is left; when the balance reaches zero the loan becomes "Paid". */
  registerPayment(loanId: string, request: RegisterPaymentRequest) {
    return apiFetch<LoanDetail>(`/api/loans/${encodeURIComponent(loanId)}/payments`, {
      method: 'POST',
      body: JSON.stringify(request),
    })
  },

  /** The payment is not deleted: it stays in the history, flagged, and stops counting. */
  voidPayment(loanId: string, paymentId: string, reason: string | null) {
    return apiFetch<LoanDetail>(
      `/api/loans/${encodeURIComponent(loanId)}/payments/${encodeURIComponent(paymentId)}/void`,
      { method: 'POST', body: JSON.stringify({ reason }) },
    )
  },

  /** Give a loan up for lost: it stops counting as money to collect. It can be undone. */
  archiveLoan(loanId: string, reason: string | null) {
    return apiFetch<LoanDetail>(`/api/loans/${encodeURIComponent(loanId)}/archive`, {
      method: 'POST',
      body: JSON.stringify({ reason }),
    })
  },

  unarchiveLoan(loanId: string) {
    return apiFetch<LoanDetail>(`/api/loans/${encodeURIComponent(loanId)}/unarchive`, { method: 'POST' })
  },

  /** The last N months, oldest first, for the charts. */
  getMonthly(months: number) {
    return apiFetch<MonthlyPoint[]>(`/api/summary/monthly?months=${months}`)
  },

  /** month: "yyyy-MM". Lent and collected are limited to that month; balances are always as of today. */
  getSummary(month: string) {
    return apiFetch<Summary>(`/api/summary?month=${encodeURIComponent(month)}`)
  },

  getClients() {
    return apiFetch<Client[]>('/api/clients')
  },

  getClient(id: string) {
    return apiFetch<Client>(`/api/clients/${encodeURIComponent(id)}`)
  },

  updateClient(id: string, request: CreateClientRequest) {
    return apiFetch<Client>(`/api/clients/${encodeURIComponent(id)}`, { method: 'PUT', body: JSON.stringify(request) })
  },

  createClient(request: CreateClientRequest) {
    return apiFetch<CreateClientResult>('/api/clients', { method: 'POST', body: JSON.stringify(request) })
  },
}
