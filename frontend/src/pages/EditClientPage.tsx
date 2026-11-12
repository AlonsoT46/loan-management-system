import { useNavigate, useParams } from 'react-router'
import { api } from '../api/client'
import { ClientForm } from '../components/ClientForm'
import type { ClientFormValues } from '../components/ClientForm'
import { LoadError, Message, ScreenTitle } from '../components/Ui'
import { useLoad } from '../hooks/useLoad'

/** /clientes/:id/editar — for example, when a client changes phone number. */
export function EditClientPage() {
  const { id = '' } = useParams()
  return <EditClient key={id} id={id} />
}

function EditClient({ id }: { id: string }) {
  const navigate = useNavigate()
  const { data: client, error, loading, retry } = useLoad(() => api.getClient(id))

  async function save(values: ClientFormValues) {
    await api.updateClient(id, {
      name: values.name.trim(),
      phone: values.phone.trim() || null,
      notes: values.notes.trim() || null,
    })
    navigate(`/clientes/${id}`, { replace: true })
  }

  return (
    <div>
      <ScreenTitle title="Editar cliente" backTo={`/clientes/${id}`} />
      <div className="px-5">
        {loading && <Message>Cargando...</Message>}
        {!loading && error && <LoadError message={error} onRetry={retry} />}
        {!loading && !error && client && (
          <ClientForm
            initial={{ name: client.name, phone: client.phone ?? '', notes: client.notes ?? '' }}
            submitLabel="Guardar cambios"
            onSubmit={save}
          />
        )}
      </div>
    </div>
  )
}
