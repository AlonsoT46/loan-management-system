import { Link } from 'react-router'
import { api } from '../api/client'
import type { LoanSummary, Summary } from '../api/types'
import { useAuth } from '../auth/useAuth'
import { ArrowDownLeftIcon, ArrowUpRightIcon } from '../components/Icons'
import { Avatar, ListCard, ListRow, LoadError, Message } from '../components/Ui'
import { outlineButtonClass } from '../components/styles'
import { useLoad } from '../hooks/useLoad'
import { currentMonth, daysSince, formatDate, formatMoney } from '../utils/format'

export function HomePage() {
  const { email } = useAuth()

  // Two requests at the same time: this month's totals and the active loans.
  const { data, error, loading, retry } = useLoad(() =>
    Promise.all([api.getSummary(currentMonth()), api.getLoans({ status: 'Active' })]),
  )

  return (
    <div className="space-y-6 px-5 pt-8">
      <header className="flex items-center gap-3">
        <Avatar name={email} />
        <div className="min-w-0">
          <p className="font-medium">Hola</p>
          <p className="truncate text-sm text-muted">{email}</p>
        </div>
      </header>

      {loading && <Message>Cargando...</Message>}
      {!loading && error && <LoadError message={error} onRetry={retry} />}

      {/* The API sorts by due date, so the overdue loans (older dates) come first. */}
      {!loading && !error && data && <HomeContent summary={data[0]} active={data[1]} />}
    </div>
  )
}

function HomeContent({ summary, active }: { summary: Summary; active: LoanSummary[] }) {
  // The two tiles split the big number in two: Vencidos + Al día = Por cobrar.
  // Archived loans (given up for lost) are not active, so they never count here.
  const onTime = active.filter((loan) => !loan.isOverdue)
  const onTimeBalance = onTime.reduce((sum, loan) => sum + loan.remainingBalance, 0)
  const nextDue = onTime[0] // the API sorts by due date, so the first one is the next to collect

  return (
    <>
      <section>
        <p className="text-muted">Por cobrar</p>
        <div className="mt-1 flex items-end justify-between gap-3">
          <p className="text-4xl font-light tracking-tight">{formatMoney(summary.outstandingBalance)}</p>
          <div className="shrink-0 text-right text-sm">
            <p className="text-gain">+{formatMoney(summary.totalCollected)}</p>
            <p className="text-muted">cobrado este mes</p>
          </div>
        </div>
        <p className="mt-1 text-sm text-muted">
          {summary.activeLoans === 1 ? '1 préstamo activo' : `${summary.activeLoans} préstamos activos`}
        </p>
      </section>

      <section className="grid grid-cols-2 gap-3">
        {/* Always a dark tile, also in dark mode (there it gets a thin border to stand out). */}
        <Link
          to="/prestamos?filtro=vencidos"
          className="flex min-h-32 flex-col justify-between gap-3 rounded-2xl bg-neutral-900 p-4 text-white dark:bg-neutral-800 dark:ring-1 dark:ring-neutral-600"
        >
          <ArrowDownLeftIcon className="size-5 text-accent" />
          <div>
            <p className="text-xl font-semibold">{formatMoney(summary.overdueBalance)}</p>
            <p className="text-sm">
              Vencidos · {summary.overdueLoans} {summary.overdueLoans === 1 ? 'préstamo' : 'préstamos'}
            </p>
          </div>
        </Link>
        <Link
          to="/prestamos"
          className="flex min-h-32 flex-col justify-between gap-3 rounded-2xl border border-ink bg-accent p-4 text-on-accent"
        >
          <ArrowUpRightIcon className="size-5" />
          <div>
            <p className="text-xl font-semibold">{formatMoney(onTimeBalance)}</p>
            <p className="text-sm">
              Al día · {onTime.length} {onTime.length === 1 ? 'préstamo' : 'préstamos'}
            </p>
            {nextDue && <p className="mt-1 text-xs">Próximo cobro: {formatDate(nextDue.dueDate)}</p>}
          </div>
        </Link>
      </section>

      <section className="grid grid-cols-2 gap-3">
        <Link to="/prestamos/nuevo" className={outlineButtonClass}>
          + Préstamo
        </Link>
        <Link to="/clientes/nuevo" className={outlineButtonClass}>
          + Cliente
        </Link>
      </section>

      <ListCard title="Por cobrar">
        {active.length === 0 && (
          <li>
            <Message>Nadie te debe ahora mismo.</Message>
          </li>
        )}
        {active.map((loan) => (
          <ListRow
            key={loan.id}
            to={`/prestamos/${loan.id}`}
            title={loan.clientName}
            subtitle={
              loan.isOverdue ? <span className="text-loss">{lateText(loan.dueDate)}</span> : dueText(loan.dueDate)
            }
            amount={formatMoney(loan.remainingBalance)}
            detail={<span className="text-muted">de {formatMoney(loan.totalAmount)}</span>}
          />
        ))}
      </ListCard>
    </>
  )
}

function lateText(dueDate: string): string {
  const days = daysSince(dueDate)
  return days === 1 ? 'Venció ayer' : `Venció hace ${days} días`
}

function dueText(dueDate: string): string {
  const daysAgo = daysSince(dueDate) // 0 = today, -1 = tomorrow
  if (daysAgo === 0)
    return 'Vence hoy'
  if (daysAgo === -1)
    return 'Vence mañana'
  return `Vence ${formatDate(dueDate)}`
}
