import { useState } from 'react'
import { Navigate, useNavigate } from 'react-router'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/useAuth'
import { Field } from '../components/Ui'
import { inputClass, primaryButtonClass } from '../components/styles'

export function LoginPage() {
  const { isLoggedIn, login } = useAuth()
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [sending, setSending] = useState(false)

  // Already logged in: no need to see this screen.
  if (isLoggedIn)
    return <Navigate to="/inicio" replace />

  async function submit() {
    setError(null)
    setSending(true)
    try {
      await login(email, password)
      navigate('/inicio', { replace: true })
    } catch (e) {
      if (e instanceof ApiError && e.status === 401)
        setError('Correo o contraseña incorrectos.')
      else
        setError(e instanceof Error ? e.message : 'Algo salió mal. Intenta de nuevo.')
      setSending(false)
    }
  }

  return (
    <main className="mx-auto flex min-h-dvh max-w-md flex-col bg-surface px-6 pb-10">
      {/* Decorative block on top, in the spirit of the reference design (same in light and dark). */}
      <div className="relative -mx-6 mb-10 h-56 overflow-hidden">
        <div className="absolute -left-12 top-24 h-36 w-64 rotate-[-12deg] rounded-3xl bg-neutral-800 shadow-xl" />
        <div className="absolute -right-10 top-8 flex h-40 w-72 rotate-[-12deg] flex-col justify-between rounded-3xl bg-neutral-900 p-6 shadow-xl ring-2 ring-accent">
          <span className="text-4xl font-light text-accent">₡</span>
          <span className="text-xs tracking-[0.3em] text-neutral-400">PRÉSTAMOS · COBROS</span>
        </div>
      </div>

      <h1 className="text-3xl font-bold tracking-tight">
        <span className="border-l-4 border-accent pl-2">Control</span> de Dinero
      </h1>
      <p className="mt-3 text-lg">Tus préstamos y cobros, en un solo lugar.</p>
      <p className="mt-1 text-muted">Inicia sesión para continuar.</p>

      <form
        className="mt-8 space-y-4"
        onSubmit={(event) => {
          event.preventDefault()
          void submit()
        }}
      >
        <Field label="Correo">
          <input
            type="email"
            autoComplete="username"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            className={inputClass}
          />
        </Field>

        <Field label="Contraseña">
          <input
            type="password"
            autoComplete="current-password"
            required
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            className={inputClass}
          />
        </Field>

        {error && (
          <p role="alert" className="rounded-xl bg-loss/10 px-4 py-3 text-loss">
            {error}
          </p>
        )}

        <button type="submit" disabled={sending} className={primaryButtonClass}>
          {sending ? 'Entrando...' : 'Entrar'}
        </button>
      </form>
    </main>
  )
}
