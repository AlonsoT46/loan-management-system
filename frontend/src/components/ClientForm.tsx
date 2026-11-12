import { useState } from 'react'
import { Field } from './Ui'
import { inputClass, primaryButtonClass } from './styles'

export interface ClientFormValues {
  name: string
  phone: string
  notes: string
}

/** The client form, shared by "Nuevo cliente" and "Editar cliente". */
export function ClientForm(props: {
  initial: ClientFormValues
  submitLabel: string
  onSubmit: (values: ClientFormValues) => Promise<void>
}) {
  const [values, setValues] = useState(props.initial)
  const [error, setError] = useState<string | null>(null)
  const [sending, setSending] = useState(false)

  function set<K extends keyof ClientFormValues>(key: K, value: string) {
    setValues((current) => ({ ...current, [key]: value }))
  }

  async function submit() {
    setError(null)
    setSending(true)
    try {
      await props.onSubmit(values)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'No se pudo guardar el cliente.')
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
      <Field label="Nombre">
        <input required maxLength={150} value={values.name} onChange={(e) => set('name', e.target.value)} className={inputClass} />
      </Field>

      <Field label="Teléfono (opcional)">
        <input
          type="tel"
          inputMode="tel"
          maxLength={30}
          value={values.phone}
          onChange={(e) => set('phone', e.target.value)}
          className={inputClass}
        />
      </Field>

      <Field label="Notas (opcional)">
        <textarea rows={3} maxLength={500} value={values.notes} onChange={(e) => set('notes', e.target.value)} className={inputClass} />
      </Field>

      {error && <p role="alert" className="rounded-xl bg-loss/10 px-4 py-3 text-loss">{error}</p>}

      <button type="submit" disabled={sending} className={primaryButtonClass}>
        {sending ? 'Guardando...' : props.submitLabel}
      </button>
    </form>
  )
}
