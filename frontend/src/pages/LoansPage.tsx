import { Link, useSearchParams } from 'react-router'
import { api } from '../api/client'
import type { LoanSummary } from '../api/types'
import { ListCard, ListRow, LoadError, Message, ScreenTitle } from '../components/Ui'
import { smallActionClass } from '../components/styles'
import { useLoad } from '../hooks/useLoad'
import { formatDate, formatMoney } from '../utils/format'
import { loanStatus, toneClass } from '../utils/loans'

type Tab = 'activos' | 'vencidos' | 'archivados'

const tabs: { value: Tab; label: string; empty: string }[] = [
  { value: 'activos', label: 'Activos', empty: 'No hay préstamos al día.' },
  { value: 'vencidos', label: 'Vencidos', empty: 'Nadie está atrasado.' },
  { value: 'archivados', label: 'Archivados', empty: 'No hay préstamos dados por perdidos.' },
]

/** What each tab asks the API for. */
async function loadTab(tab: Tab): Promise<LoanSummary[]> {
  if (tab === 'vencidos')
    return api.getLoans({ onlyOverdue: true })
  if (tab === 'archivados')
    return api.getLoans({ status: 'Archived' })

  // Active and on time: the late ones have their own tab.
  const active = await api.getLoans({ status: 'Active' })
  return active.filter((loan) => !loan.isOverdue)
}

export function LoansPage() {
  // The tab lives in the address (/prestamos?filtro=vencidos), so other screens can link to it.
  const [searchParams, setSearchParams] = useSearchParams()
  const filter = searchParams.get('filtro')
  const tab: Tab = filter === 'vencidos' || filter === 'archivados' ? filter : 'activos'

  return (
    <div>
      <ScreenTitle
        title="Préstamos"
        action={
          <Link to="/prestamos/nuevo" className={smallActionClass}>
            + Nuevo
          </Link>
        }
      />

      <div className="space-y-4 px-5">
        <div className="grid grid-cols-3 gap-2">
          {tabs.map((t) => (
            <button
              key={t.value}
              type="button"
              onClick={() => setSearchParams(t.value === 'activos' ? {} : { filtro: t.value }, { replace: true })}
              className={
                'rounded-xl border border-ink py-2.5 text-sm font-medium ' +
                (tab === t.value ? 'bg-ink text-surface' : 'bg-surface text-ink')
              }
            >
              {t.label}
            </button>
          ))}
        </div>

        {/* A new key when the tab changes = a fresh list that loads again. */}
        <LoanList key={tab} tab={tab} />

        <p className="text-center text-sm text-muted">Los préstamos pagados quedan en el historial de cada cliente.</p>
      </div>
    </div>
  )
}

function LoanList({ tab }: { tab: Tab }) {
  const { data: loans, error, loading, retry } = useLoad(() => loadTab(tab))

  if (loading)
    return <Message>Cargando...</Message>
  if (error)
    return <LoadError message={error} onRetry={retry} />
  if (!loans || loans.length === 0)
    return <Message>{tabs.find((t) => t.value === tab)?.empty}</Message>

  return (
    <ListCard>
      {loans.map((loan) => {
        const status = loanStatus(loan)
        return (
          <ListRow
            key={loan.id}
            to={`/prestamos/${loan.id}`}
            title={loan.clientName}
            subtitle={`Vence ${formatDate(loan.dueDate)}`}
            amount={
              loan.status === 'Archived' ? (
                <span className="text-muted line-through">{formatMoney(loan.remainingBalance)}</span>
              ) : (
                formatMoney(loan.remainingBalance)
              )
            }
            detail={<span className={toneClass[status.tone]}>{status.label}</span>}
          />
        )
      })}
    </ListCard>
  )
}
