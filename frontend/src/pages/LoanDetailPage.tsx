import { useState } from 'react'
import { Link, useParams } from 'react-router'
import { api } from '../api/client'
import type { LoanDetail, LoanSummary, Payment } from '../api/types'
import { ArrowDownLeftIcon } from '../components/Icons'
import { Field, InfoRow, ListCard, ListRow, LoadError, Message, ScreenTitle } from '../components/Ui'
import { darkButtonClass, inputClass, outlineButtonClass, primaryButtonClass, smallActionClass } from '../components/styles'
import { useLoad } from '../hooks/useLoad'
import { formatDate, formatMoney, todayIso } from '../utils/format'
import { loanStatus, toneClass } from '../utils/loans'

/** /prestamos/:id — one loan: its numbers, its payments, and the form to register a new payment. */
export function LoanDetailPage() {
  const { id = '' } = useParams()
  return <LoanDetailView key={id} id={id} />
}

function LoanDetailView({ id }: { id: string }) {
  const { data, error, loading, retry, setData } = useLoad(() => api.getLoan(id))

  if (loading || error || !data) {
    return (
      <div>
        <ScreenTitle title="Préstamo" backTo="/prestamos" />
        <div className="px-5">
          {loading ? <Message>Cargando...</Message> : <LoadError message={error ?? 'Error'} onRetry={retry} />}
        </div>
      </div>
    )
  }

  const { loan, payments } = data
  const status = loanStatus(loan)
  const paidPercent = loan.totalAmount > 0 ? Math.min(100, (loan.totalPaid / loan.totalAmount) * 100) : 0

  return (
    <div>
      <ScreenTitle
        title={loan.clientName}
        backTo="/prestamos"
        subtitle={
          <Link to={`/clientes/${loan.clientId}`} className="underline">
            Ver cliente
          </Link>
        }
        action={
          <span className={'rounded-full border border-current px-3 py-1 text-sm font-medium ' + toneClass[status.tone]}>
            {status.label}
          </span>
        }
      />

      <div className="space-y-5 px-5">
        <section>
          <p className="text-muted">{loan.status === 'Archived' ? 'Quedó sin pagar (dado por perdido)' : 'Falta por pagar'}</p>
          <p className={'text-4xl font-light tracking-tight ' + (loan.status === 'Archived' ? 'text-muted line-through' : '')}>
            {formatMoney(loan.remainingBalance)}
          </p>
          <div className="mt-3 h-2 overflow-hidden rounded-full bg-line">
            <div className="h-full rounded-full bg-gain" style={{ width: `${paidPercent}%` }} />
          </div>
          <p className="mt-1 text-sm text-muted">
            Pagado {formatMoney(loan.totalPaid)} de {formatMoney(loan.totalAmount)}
          </p>
        </section>

        {loan.status === 'Archived' && <ArchivedNotice loan={loan} onChanged={setData} />}
        {loan.status === 'Active' && <PaymentForm loan={loan} onSaved={setData} />}
        {loan.status === 'Active' && loan.isOverdue && <ArchiveOffer loan={loan} onChanged={setData} />}
        {loan.status === 'Paid' && (
          <p className="rounded-2xl bg-gain/15 p-4 text-center font-medium text-gain">Este préstamo ya está pagado.</p>
        )}

        <section className="divide-y divide-line rounded-2xl border border-ink px-4 py-1">
          <InfoRow label="Fecha del préstamo" value={formatDate(loan.loanDate)} />
          <InfoRow label="Fecha límite" value={formatDate(loan.dueDate)} className={loan.isOverdue ? 'text-loss' : ''} />
          <InfoRow label="Capital" value={formatMoney(loan.principalAmount)} />
          <InfoRow
            label={`Interés (${loan.quincenas} ${loan.quincenas === 1 ? 'quincena' : 'quincenas'} al ${loan.interestRatePerQuincena}%)`}
            value={formatMoney(loan.interestAmount)}
            className="text-gain"
          />
          <InfoRow label="Total" value={formatMoney(loan.totalAmount)} strong />
        </section>

        {loan.notes && <p className="rounded-2xl bg-line/60 p-4 text-sm">{loan.notes}</p>}

        {loan.status !== 'Cancelled' && (
          <Link to={`/prestamos/${loan.id}/editar`} className={outlineButtonClass}>
            Corregir préstamo
          </Link>
        )}

        <PaymentList loan={loan} payments={payments} onChanged={setData} />
      </div>
    </div>
  )
}

/** My father types what he received and the day. Never more than what is left. */
function PaymentForm({ loan, onSaved }: { loan: LoanSummary; onSaved: (detail: LoanDetail) => void }) {
  const [amount, setAmount] = useState('')
  const [paymentDate, setPaymentDate] = useState(todayIso)
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [sending, setSending] = useState(false)

  const value = Number(amount)
  const tooMuch = value > loan.remainingBalance
  const minDate = loan.loanDate.slice(0, 10) // a payment cannot be dated before the loan

  async function submit() {
    setError(null)
    setSending(true)
    try {
      const updated = await api.registerPayment(loan.id, {
        amount: value,
        paymentDate,
        notes: notes.trim() || null,
      })
      onSaved(updated)
      setAmount('')
      setNotes('')
      setPaymentDate(todayIso())
    } catch (e) {
      setError(e instanceof Error ? e.message : 'No se pudo registrar el pago.')
    } finally {
      setSending(false)
    }
  }

  return (
    <form
      className="space-y-3 rounded-2xl border border-ink p-4"
      onSubmit={(event) => {
        event.preventDefault()
        void submit()
      }}
    >
      <h2 className="text-lg font-medium">Registrar pago</h2>

      <Field label="¿Cuánto le pagaron? (₡)">
        <input
          type="number"
          inputMode="decimal"
          min="0.01"
          max={loan.remainingBalance}
          step="any"
          required
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
          className={inputClass}
        />
      </Field>

      <button type="button" onClick={() => setAmount(String(loan.remainingBalance))} className={smallActionClass}>
        Pagó todo ({formatMoney(loan.remainingBalance)})
      </button>

      {tooMuch && <p className="text-sm text-loss">Es más de lo que falta ({formatMoney(loan.remainingBalance)}).</p>}
      {value > 0 && !tooMuch && (
        <p className="text-sm text-muted">
          {value === loan.remainingBalance
            ? 'Con este pago el préstamo queda pagado.'
            : `Después de este pago faltarían ${formatMoney(loan.remainingBalance - value)}.`}
        </p>
      )}

      <Field label="¿Qué día?">
        <input
          type="date"
          required
          min={minDate}
          max={todayIso()}
          value={paymentDate}
          onChange={(e) => setPaymentDate(e.target.value)}
          className={inputClass}
        />
      </Field>

      <Field label="Notas (opcional)">
        <input maxLength={500} value={notes} onChange={(e) => setNotes(e.target.value)} className={inputClass} />
      </Field>

      {error && <p role="alert" className="rounded-xl bg-loss/10 px-4 py-3 text-loss">{error}</p>}

      <button type="submit" disabled={sending || tooMuch || !(value > 0)} className={primaryButtonClass}>
        {sending ? 'Guardando...' : 'Guardar pago'}
      </button>
    </form>
  )
}

/** For an overdue loan: give it up for lost. It moves to "Archivados" and stops counting. */
function ArchiveOffer({ loan, onChanged }: { loan: LoanSummary; onChanged: (detail: LoanDetail) => void }) {
  const [confirming, setConfirming] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function archive() {
    setError(null)
    setBusy(true)
    try {
      onChanged(await api.archiveLoan(loan.id, null))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'No se pudo archivar el préstamo.')
      setBusy(false)
    }
  }

  return (
    <section className="space-y-3 rounded-2xl border border-dashed border-muted p-4">
      <h2 className="font-medium">¿Ya no te va a pagar?</h2>
      <p className="text-sm text-muted">
        Si lo das por perdido, los {formatMoney(loan.remainingBalance)} que faltan dejan de contar en lo que te deben y el
        préstamo pasa a Archivados. Si un día te vuelve a pagar, lo puedes desarchivar.
      </p>

      {error && <p role="alert" className="text-sm text-loss">{error}</p>}

      {confirming ? (
        <div className="grid grid-cols-2 gap-2">
          <button type="button" disabled={busy} onClick={() => void archive()} className={darkButtonClass}>
            Sí, archivar
          </button>
          <button type="button" onClick={() => setConfirming(false)} className={outlineButtonClass}>
            No
          </button>
        </div>
      ) : (
        <button type="button" onClick={() => setConfirming(true)} className={outlineButtonClass}>
          Dar por perdido (archivar)
        </button>
      )}
    </section>
  )
}

/** An archived loan: say so, and allow bringing it back. */
function ArchivedNotice({ loan, onChanged }: { loan: LoanSummary; onChanged: (detail: LoanDetail) => void }) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function unarchive() {
    setError(null)
    setBusy(true)
    try {
      onChanged(await api.unarchiveLoan(loan.id))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'No se pudo desarchivar el préstamo.')
      setBusy(false)
    }
  }

  return (
    <section className="space-y-3 rounded-2xl bg-line/60 p-4">
      <p>
        <span className="font-medium">Archivado:</span> se dio por perdido con {formatMoney(loan.remainingBalance)} sin
        pagar. No cuenta en lo que te deben.
      </p>
      {error && <p role="alert" className="text-sm text-loss">{error}</p>}
      <button type="button" disabled={busy} onClick={() => void unarchive()} className={outlineButtonClass}>
        Desarchivar (me volvió a pagar)
      </button>
    </section>
  )
}

/** The payments, newest last. A wrong one can be voided: it stays in the list, crossed out. */
function PaymentList(props: { loan: LoanSummary; payments: Payment[]; onChanged: (detail: LoanDetail) => void }) {
  const [confirmingId, setConfirmingId] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const canVoid = props.loan.status !== 'Cancelled'

  async function voidPayment(paymentId: string) {
    setError(null)
    setBusy(true)
    try {
      props.onChanged(await api.voidPayment(props.loan.id, paymentId, null))
      setConfirmingId(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'No se pudo anular el pago.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <ListCard title={`Pagos (${props.payments.length})`}>
      {error && (
        <li className="px-4 py-3 text-sm text-loss" role="alert">
          {error}
        </li>
      )}
      {props.payments.length === 0 && (
        <li>
          <Message>Todavía no hay pagos.</Message>
        </li>
      )}
      {props.payments.map((payment) => (
        <ListRow
          key={payment.id}
          leading={
            <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-gain/15 text-gain">
              <ArrowDownLeftIcon className="size-5" />
            </div>
          }
          title={<span className={payment.isVoided ? 'text-muted line-through' : ''}>{formatMoney(payment.amount)}</span>}
          subtitle={
            <>
              <div>{formatDate(payment.paymentDate)}</div>
              {!payment.isVoided && (
                <div>
                  Capital {formatMoney(payment.principalPaid)} · Interés {formatMoney(payment.interestPaid)}
                </div>
              )}
            </>
          }
          detail={
            payment.isVoided ? (
              <span className="text-loss">{payment.voidReason ? `Anulado: ${payment.voidReason}` : 'Anulado'}</span>
            ) : !canVoid ? undefined : confirmingId === payment.id ? (
              <span className="flex flex-col items-end gap-1">
                <button type="button" disabled={busy} onClick={() => void voidPayment(payment.id)} className="font-medium text-loss">
                  Sí, anular
                </button>
                <button type="button" onClick={() => setConfirmingId(null)} className="text-muted">
                  No
                </button>
              </span>
            ) : (
              <button type="button" onClick={() => setConfirmingId(payment.id)} className="text-muted underline">
                Anular
              </button>
            )
          }
        />
      ))}
    </ListCard>
  )
}
