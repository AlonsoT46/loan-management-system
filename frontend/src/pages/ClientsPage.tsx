import { Link } from 'react-router'
import { api } from '../api/client'
import { ListCard, ListRow, LoadError, Message, ScreenTitle } from '../components/Ui'
import { smallActionClass } from '../components/styles'
import { useLoad } from '../hooks/useLoad'

export function ClientsPage() {
  const { data: clients, error, loading, retry } = useLoad(() => api.getClients())

  return (
    <div>
      <ScreenTitle
        title="Clientes"
        subtitle={clients ? `${clients.length} ${clients.length === 1 ? 'cliente' : 'clientes'}` : undefined}
        action={
          <Link to="/clientes/nuevo" className={smallActionClass}>
            + Nuevo
          </Link>
        }
      />

      <div className="px-5">
        {loading && <Message>Cargando...</Message>}
        {!loading && error && <LoadError message={error} onRetry={retry} />}
        {!loading && !error && clients?.length === 0 && <Message>Todavía no hay clientes.</Message>}

        {!loading && !error && clients && clients.length > 0 && (
          <ListCard>
            {clients.map((client) => (
              <ListRow
                key={client.id}
                to={`/clientes/${client.id}`}
                title={client.name}
                subtitle={client.phone ?? 'Sin teléfono'}
              />
            ))}
          </ListCard>
        )}
      </div>
    </div>
  )
}
