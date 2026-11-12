import { useAuth } from '../auth/useAuth'
import { Avatar, ScreenTitle } from '../components/Ui'
import { darkButtonClass } from '../components/styles'
import { useTheme } from '../theme/useTheme'
import type { ThemeChoice } from '../theme/useTheme'

const themeOptions: { value: ThemeChoice; label: string }[] = [
  { value: 'light', label: 'Claro' },
  { value: 'dark', label: 'Oscuro' },
  { value: 'system', label: 'Automático' },
]

export function ProfilePage() {
  const { email, logout } = useAuth()
  const { choice, setChoice } = useTheme()

  return (
    <div>
      <ScreenTitle title="Perfil" />

      <div className="space-y-8 px-5">
        <div className="flex flex-col items-center gap-3 pt-2">
          <Avatar name={email} size="lg" />
          <p className="text-muted">{email}</p>
        </div>

        <section>
          <h2 className="mb-2 font-medium">Apariencia</h2>
          <div className="grid grid-cols-3 gap-2">
            {themeOptions.map((option) => (
              <button
                key={option.value}
                type="button"
                onClick={() => setChoice(option.value)}
                className={
                  'rounded-xl border border-ink py-2.5 font-medium ' +
                  (choice === option.value ? 'bg-ink text-surface' : 'bg-surface text-ink')
                }
              >
                {option.label}
              </button>
            ))}
          </div>
          <p className="mt-2 text-sm text-muted">"Automático" sigue el modo claro u oscuro del celular.</p>
        </section>

        <button type="button" onClick={logout} className={darkButtonClass}>
          Cerrar sesión
        </button>
      </div>
    </div>
  )
}
