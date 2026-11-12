import { useState } from 'react'
import { addDaysIso, formatMoney } from '../utils/format'
import { DEFAULT_RATE_PERCENT, estimateLoan } from '../utils/loans'
import { Field, InfoRow } from './Ui'
import { inputClass, primaryButtonClass, smallActionClass } from './styles'

export interface LoanFormValues {
  clientId: string
  amount: string
  loanDate: string
  dueDate: string
  notes: string
}

/**
 * The loan form, shared by "Nuevo préstamo" and "Corregir préstamo".
 * With lockTerms, only the notes can change (the loan already has payments).
 */
export function LoanForm(props: {
  clients: { id: string; name: string }[]
  initial: LoanFormValues
  lockTerms?: boolean
  submitLabel: string
  onSubmit: (values: LoanFormValues) => Promise<void>
}) {
  const [values, setValues] = useState(props.initial)
  const [error, setError] = useState<string | null>(null)
  const [sending, setSending] = useState(false)

  const locked = props.lockTerms ?? false
  const principal = Number(values.amount)
  const estimate = estimateLoan(principal, values.loanDate, values.dueDate)
  const dueBeforeLoan = values.dueDate !== '' && values.dueDate < values.loanDate // "yyyy-MM-dd" compare like dates

  function set<K extends keyof LoanFormValues>(key: K, value: LoanFormValues[K]) {
    setValues((current) => ({ ...current, [key]: value }))
  }

  async function submit() {
    setError(null)
    setSending(true)
    try {
      await props.onSubmit(values)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'No se pudo guardar el préstamo.')
      setSending(false)
    }
  }

  return (
    <form
      className="space-y-4"
      onSubmit={(event) => {
        event.preventDefault()
        void submit()
      }}
    >
      {locked && (
        <p className="rounded-2xl bg-line/60 p-4 text-sm">
          Este préstamo ya tiene pagos, así que solo se pueden cambiar las notas. Para corregir el monto o las fechas,
          primero anula sus pagos.
        </p>
      )}

      <Field label="Cliente">
        <select
          required
          disabled={locked}
          value={values.clientId}
          onChange={(e) => set('clientId', e.target.value)}
          className={inputClass + ' disabled:opacity-60'}
        >
          <option value="" disabled>
            Elige un cliente
          </option>
          {props.clients.map((client) => (
            <option key={client.id} value={client.id}>
              {client.name}
            </option>
          ))}
        </select>
      </Field>

      <Field label="Monto prestado (₡)" hint={principal > 0 ? formatMoney(principal) : undefined}>
        <input
          type="number"
          inputMode="decimal"
          min="1"
          step="any"
          required
          disabled={locked}
          value={values.amount}
          onChange={(e) => set('amount', e.target.value)}
          className={inputClass + ' disabled:opacity-60'}
        />
      </Field>

      <Field label="Fecha del préstamo">
        <input
          type="date"
          required
          disabled={locked}
          value={values.loanDate}
          onChange={(e) => set('loanDate', e.target.value)}
          className={inputClass + ' disabled:opacity-60'}
        />
      </Field>

      <Field label="Fecha límite para pagar">
        <input
          type="date"
          required
          disabled={locked}
          value={values.dueDate}
          onChange={(e) => set('dueDate', e.target.value)}
          className={inputClass + ' disabled:opacity-60'}
        />
      </Field>

      {!locked && (
        // Shortcuts: the due date counted in quincenas from the loan date.
        <div className="flex flex-wrap gap-2">
          {[1, 2, 3, 4].map((n) => (
            <button
              key={n}
              type="button"
              onClick={() => set('dueDate', addDaysIso(values.loanDate, n * 15))}
              className={smallActionClass}
            >
              {n} {n === 1 ? 'quincena' : 'quincenas'}
            </button>
          ))}
        </div>
      )}

      {dueBeforeLoan && <p className="text-sm text-loss">La fecha límite no puede ser antes del préstamo.</p>}

      {estimate && !dueBeforeLoan && (
        <section className="divide-y divide-line rounded-2xl border border-ink px-4 py-1">
          <InfoRow label="Quincenas" value={estimate.quincenas} />
          <InfoRow label={`Interés (${DEFAULT_RATE_PERCENT}% por quincena)`} value={formatMoney(estimate.interest)} className="text-gain" />
          <InfoRow label="Total a cobrar" value={formatMoney(estimate.total)} strong />
        </section>
      )}

      <Field label="Notas (opcional)">
        <textarea rows={2} maxLength={500} value={values.notes} onChange={(e) => set('notes', e.target.value)} className={inputClass} />
      </Field>

      {error && <p role="alert" className="rounded-xl bg-loss/10 px-4 py-3 text-loss">{error}</p>}

      <button type="submit" disabled={sending || dueBeforeLoan || !values.clientId} className={primaryButtonClass}>
        {sending ? 'Guardando...' : props.submitLabel}
      </button>
    </form>
  )
}
