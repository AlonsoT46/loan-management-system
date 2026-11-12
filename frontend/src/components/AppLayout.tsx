import { NavLink, Outlet } from 'react-router'
import { ChartIcon, ClientsIcon, HomeIcon, LoansIcon, ProfileIcon } from './Icons'

const tabs = [
  { to: '/inicio', label: 'Inicio', Icon: HomeIcon },
  { to: '/prestamos', label: 'Préstamos', Icon: LoansIcon },
  { to: '/clientes', label: 'Clientes', Icon: ClientsIcon },
  { to: '/estadistica', label: 'Estadística', Icon: ChartIcon },
  { to: '/perfil', label: 'Perfil', Icon: ProfileIcon },
]

/** The frame of the private screens: the page on top, the navigation bar fixed at the bottom. */
export function AppLayout() {
  return (
    <div className="mx-auto min-h-dvh max-w-md bg-surface">
      <div className="pb-28">
        <Outlet />
      </div>

      <nav className="fixed inset-x-0 bottom-0 border-t border-line bg-surface pb-[env(safe-area-inset-bottom)]">
        <div className="mx-auto grid max-w-md grid-cols-5">
          {tabs.map(({ to, label, Icon }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                'flex flex-col items-center gap-1 py-3 text-[11px] ' +
                (isActive ? 'font-medium text-ink' : 'text-muted')
              }
            >
              <Icon />
              {label}
            </NavLink>
          ))}
        </div>
      </nav>
    </div>
  )
}
