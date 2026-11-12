import { useNavigate } from 'react-router'
import { api } from '../api/client'
import { ClientForm } from '../components/ClientForm'
import type { ClientFormValues } from '../components/ClientForm'
import { ScreenTitle } from '../components/Ui'

/** /clientes/nuevo */
export function NewClientPage() {
  const navigate = useNavigate()

  async function save(values: ClientFormValues) {
    const result = await api.createClient({
      name: values.name.trim(),
      phone: values.phone.trim() || null,
      notes: values.notes.trim() || null,
    })
    // The backend saves it even if the name already exists, and tells us; the detail screen shows a notice.
    navigate(`/clientes/${result.client.id}`, {
      replace: true,
      state: { possibleDuplicates: result.possibleDuplicates.length },
    })
  }

  return (
    <div>
      <ScreenTitle title="Nuevo cliente" backTo="/clientes" />
      <div className="px-5">
        <ClientForm initial={{ name: '', phone: '', notes: '' }} submitLabel="Guardar cliente" onSubmit={save} />
      </div>
    </div>
  )
}
