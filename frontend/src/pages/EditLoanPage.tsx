import { useNavigate, useParams } from 'react-router'
import { api } from '../api/client'
import { LoanForm } from '../components/LoanForm'
import type { LoanFormValues } from '../components/LoanForm'
import { LoadError, Message, ScreenTitle } from '../components/Ui'
import { useLoad } from '../hooks/useLoad'

/** /prestamos/:id/editar — correct a loan registered with a mistake. */
export function EditLoanPage() {
  const { id = '' } = useParams()
  return <EditLoan key={id} id={id} />
}

function EditLoan({ id }: { id: string }) {
  const navigate = useNavigate()
  const { data, error, loading, retry } = useLoad(() => Promise.all([api.getLoan(id), api.getClients()]))

  async function save(values: LoanFormValues) {
    await api.updateLoan(id, {
      clientId: values.clientId,
      principalAmount: Number(values.amount),
      loanDate: values.loanDate,
      dueDate: values.dueDate,
      notes: values.notes.trim() || null,
    })
    navigate(`/prestamos/${id}`, { replace: true })
  }

  if (loading || error || !data) {
    return (
      <div>
        <ScreenTitle title="Corregir préstamo" backTo={`/prestamos/${id}`} />
        <div className="px-5">
          {loading ? <Message>Cargando...</Message> : <LoadError message={error ?? 'Error'} onRetry={retry} />}
        </div>
      </div>
    )
  }

  const [{ loan, payments }, clients] = data
  // Amount, dates and client only change while no payment counts (the backend checks it too).
  const lockTerms = loan.status !== 'Active' || payments.some((p) => !p.isVoided)
  // The list has active clients only; keep the current one even if it was deactivated.
  const options = clients.some((c) => c.id === loan.clientId)
    ? clients
    : [{ id: loan.clientId, name: loan.clientName }, ...clients]

  return (
    <div>
      <ScreenTitle title="Corregir préstamo" backTo={`/prestamos/${id}`} />
      <div className="px-5">
        <LoanForm
          clients={options}
          initial={{
            clientId: loan.clientId,
            amount: String(loan.principalAmount),
            loanDate: loan.loanDate.slice(0, 10),
            dueDate: loan.dueDate.slice(0, 10),
            notes: loan.notes ?? '',
          }}
          lockTerms={lockTerms}
          submitLabel="Guardar cambios"
          onSubmit={save}
        />
      </div>
    </div>
  )
}
