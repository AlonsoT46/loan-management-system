import { Link, useNavigate, useSearchParams } from 'react-router'
import { api } from '../api/client'
import { LoanForm } from '../components/LoanForm'
import type { LoanFormValues } from '../components/LoanForm'
import { LoadError, Message, ScreenTitle } from '../components/Ui'
import { useLoad } from '../hooks/useLoad'
import { todayIso } from '../utils/format'

/** /prestamos/nuevo, or /prestamos/nuevo?cliente=<id> to start with a client already chosen. */
export function NewLoanPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const { data: clients, error, loading, retry } = useLoad(() => api.getClients())

  const preselected = searchParams.get('cliente') ?? ''

  async function save(values: LoanFormValues) {
    const created = await api.createLoan({
      clientId: values.clientId,
      principalAmount: Number(values.amount),
      loanDate: values.loanDate,
      dueDate: values.dueDate,
      notes: values.notes.trim() || null,
    })
    navigate(`/prestamos/${created.loan.id}`, { replace: true })
  }

  return (
    <div>
      <ScreenTitle title="Nuevo préstamo" backTo="/prestamos" />
      <div className="px-5">
        {loading && <Message>Cargando...</Message>}
        {!loading && error && <LoadError message={error} onRetry={retry} />}
        {!loading && !error && clients?.length === 0 && (
          <Message>
            <p>Primero agrega un cliente.</p>
            <Link to="/clientes/nuevo" className="mt-3 inline-block font-medium text-ink underline">
              + Nuevo cliente
            </Link>
          </Message>
        )}
        {!loading && !error && clients && clients.length > 0 && (
          <LoanForm
            clients={clients}
            initial={{
              clientId: clients.some((c) => c.id === preselected) ? preselected : '',
              amount: '',
              loanDate: todayIso(),
              dueDate: '',
              notes: '',
            }}
            submitLabel="Guardar préstamo"
            onSubmit={save}
          />
        )}
      </div>
    </div>
  )
}
