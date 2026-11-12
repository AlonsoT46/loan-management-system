import { Link, useLocation, useParams } from 'react-router'
import { api } from '../api/client'
import type { LoanSummary } from '../api/types'
import { PhoneIcon } from '../components/Icons'
import { ListCard, ListRow, LoadError, Message, ScreenTitle } from '../components/Ui'
import { primaryButtonClass, smallActionClass } from '../components/styles'
import { useLoad } from '../hooks/useLoad'
import { formatDate, formatMoney } from '../utils/format'
import { loanStatus, toneClass } from '../utils/loans'

/** /clientes/:id — a client with all their loans, grouped by situation. */
export function ClientDetailPage() {
  const { id = '' } = useParams()
  // key: when the id changes, the screen starts from zero and loads the other client.
  return <ClientDetail key={id} id={id} />
}

function ClientDetail({ id }: { id: string }) {
  const location = useLocation()
  const duplicates = (location.state as { possibleDuplicates?: number } | null)?.possibleDuplicates ?? 0

  const { data, error, loading, retry } = useLoad(() =>
    Promise.all([api.getClient(id), api.getLoans({ clientId: id })]),
  )

  if (loading || error || !data) {
    return (
      <div>
        <ScreenTitle title="Cliente" backTo="/clientes" />
        <div className="px-5">
          {loading ? <Message>Cargando...</Message> : <LoadError message={error ?? 'Error'} onRetry={retry} />}
        </div>
      </div>
    )
  }

  const [client, loans] = data
  const overdue = loans.filter((l) => l.isOverdue)
  const active = loans.filter((l) => l.status === 'Active' && !l.isOverdue)
  const paid = loans.filter((l) => l.status === 'Paid')
  const cancelled = loans.filter((l) => l.status === 'Cancelled')
  const archived = loans.filter((l) => l.status === 'Archived')
  const owesToday = loans.filter((l) => l.status === 'Active').reduce((sum, l) => sum + l.remainingBalance, 0)

  return (
    <div>
      <ScreenTitle
        title={client.name}
        backTo="/clientes"
        action={
          <Link to={`/clientes/${client.id}/editar`} className={smallActionClass}>
            Editar
          </Link>
        }
        subtitle={
          client.phone ? (
            <a href={`tel:${client.phone}`} className="inline-flex items-center gap-1 text-ink underline">
              <PhoneIcon className="size-4" />
              {client.phone}
            </a>
          ) : (
            'Sin teléfono'
          )
        }
      />

      <div className="space-y-5 px-5">
        {duplicates > 0 && (
          <p className="rounded-2xl bg-accent/30 p-4 text-sm">
            Ojo: ya tenías otro cliente con este mismo nombre. Se guardó igual; revisa que no sea la misma persona.
          </p>
        )}

        <section>
          <p className="text-muted">Le debe hoy</p>
          <p className="text-4xl font-light tracking-tight">{formatMoney(owesToday)}</p>
          {overdue.length > 0 && (
            <p className="mt-1 text-sm text-loss">
              {overdue.length} {overdue.length === 1 ? 'préstamo vencido' : 'préstamos vencidos'}
            </p>
          )}
        </section>

        <Link to={`/prestamos/nuevo?cliente=${client.id}`} className={primaryButtonClass}>
          + Nuevo préstamo
        </Link>

        {client.notes && <p className="rounded-2xl bg-line/60 p-4 text-sm">{client.notes}</p>}

        {loans.length === 0 && <Message>Todavía no tiene préstamos.</Message>}

        <LoanGroup title="Vencidos" loans={overdue} />
        <LoanGroup title="Activos" loans={active} />
        <LoanGroup title="Pagados" loans={paid} />
        <LoanGroup title="Archivados (dados por perdidos)" loans={archived} />
        <LoanGroup title="Cancelados" loans={cancelled} />
      </div>
    </div>
  )
}

function LoanGroup({ title, loans }: { title: string; loans: LoanSummary[] }) {
  if (loans.length === 0)
    return null

  return (
    <ListCard title={`${title} (${loans.length})`}>
      {loans.map((loan) => {
        const status = loanStatus(loan)
        return (
          <ListRow
            key={loan.id}
            to={`/prestamos/${loan.id}`}
            leading={
              <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-line text-lg text-ink">₡</div>
            }
            title={`Prestó ${formatMoney(loan.principalAmount)}`}
            subtitle={`${formatDate(loan.loanDate)} · vence ${formatDate(loan.dueDate)}`}
            amount={
              loan.status === 'Archived' ? (
                <span className="text-muted line-through">{formatMoney(loan.remainingBalance)}</span>
              ) : (
                formatMoney(loan.status === 'Paid' ? loan.totalPaid : loan.remainingBalance)
              )
            }
            detail={<span className={toneClass[status.tone]}>{loan.status === 'Active' ? `${status.label} · falta` : status.label}</span>}
          />
        )
      })}
    </ListCard>
  )
}
