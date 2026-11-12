import type { ReactNode } from 'react'
import { Link, useLocation, useNavigate } from 'react-router'
import { initial } from '../utils/format'
import { ChevronLeftIcon } from './Icons'

// Small building blocks shared by the screens, so they all look the same.

export function ScreenTitle(props: { title: string; subtitle?: ReactNode; action?: ReactNode; backTo?: string }) {
  return (
    <header className="px-5 pt-6 pb-4">
      {props.backTo && <BackButton fallback={props.backTo} />}
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl font-semibold">{props.title}</h1>
        {props.action}
      </div>
      {props.subtitle && <div className="mt-1 text-sm text-muted">{props.subtitle}</div>}
    </header>
  )
}

/** Goes back to the previous screen; if the page was opened directly, goes to "fallback". */
export function BackButton({ fallback }: { fallback: string }) {
  const navigate = useNavigate()
  const location = useLocation()

  return (
    <button
      type="button"
      onClick={() => (location.key === 'default' ? navigate(fallback) : navigate(-1))}
      className="-ml-1 mb-3 flex items-center gap-1 text-sm text-muted"
    >
      <ChevronLeftIcon className="size-4" />
      Volver
    </button>
  )
}

export function Avatar({ name, size = 'md' }: { name: string | null | undefined; size?: 'md' | 'lg' }) {
  const sizes = size === 'lg' ? 'size-20 text-3xl' : 'size-12 text-lg'
  return (
    <div className={'flex shrink-0 items-center justify-center rounded-full bg-accent font-semibold text-on-accent ' + sizes}>
      {initial(name)}
    </div>
  )
}

/** The bordered box with a title and rows, like the "watchlist" of the reference design. */
export function ListCard(props: { title?: ReactNode; action?: ReactNode; children: ReactNode }) {
  return (
    <section className="overflow-hidden rounded-2xl border border-ink bg-surface">
      {props.title && (
        <div className="flex items-center justify-between border-b border-ink px-4 py-3">
          <h2 className="text-lg font-medium">{props.title}</h2>
          {props.action}
        </div>
      )}
      <ul className="divide-y divide-line">{props.children}</ul>
    </section>
  )
}

/** One row of a ListCard. With "to" the whole row is a link. */
export function ListRow(props: {
  title: ReactNode
  subtitle?: ReactNode
  amount?: ReactNode
  detail?: ReactNode
  leading?: ReactNode
  to?: string
}) {
  const content = (
    <>
      {props.leading ?? (
        <div className="flex size-10 shrink-0 items-center justify-center rounded-full bg-ink font-semibold text-surface">
          {typeof props.title === 'string' ? initial(props.title) : '•'}
        </div>
      )}
      <div className="min-w-0 flex-1">
        <div className="truncate font-medium">{props.title}</div>
        {props.subtitle && <div className="truncate text-sm text-muted">{props.subtitle}</div>}
      </div>
      {(props.amount || props.detail) && (
        <div className="shrink-0 text-right">
          {props.amount && <div className="font-medium">{props.amount}</div>}
          {props.detail && <div className="text-sm">{props.detail}</div>}
        </div>
      )}
    </>
  )

  return (
    <li>
      {props.to ? (
        <Link to={props.to} className="flex items-center gap-3 px-4 py-3 active:bg-line">
          {content}
        </Link>
      ) : (
        <div className="flex items-center gap-3 px-4 py-3">{content}</div>
      )}
    </li>
  )
}

export function Message({ children, tone = 'muted' }: { children: ReactNode; tone?: 'muted' | 'error' }) {
  return (
    <div className={tone === 'error' ? 'rounded-2xl bg-loss/10 p-4 text-loss' : 'py-10 text-center text-muted'}>
      {children}
    </div>
  )
}

/** The error box with a "Reintentar" button, used by every screen that loads data. */
export function LoadError({ message, onRetry }: { message: string; onRetry: () => void }) {
  return (
    <Message tone="error">
      <p>{message}</p>
      <button type="button" onClick={onRetry} className="mt-2 font-medium underline">
        Reintentar
      </button>
    </Message>
  )
}

/** Label + value line, used in the loan detail and the statistics. */
export function InfoRow(props: { label: ReactNode; value: ReactNode; strong?: boolean; className?: string }) {
  return (
    <div className="flex items-baseline justify-between gap-3 py-2">
      <span className="text-muted">{props.label}</span>
      <span className={(props.strong ? 'text-lg font-semibold ' : 'font-medium ') + (props.className ?? '')}>
        {props.value}
      </span>
    </div>
  )
}

/** Label above a form control. */
export function Field({ label, children, hint }: { label: string; children: ReactNode; hint?: ReactNode }) {
  return (
    <label className="block">
      <span className="text-sm text-muted">{label}</span>
      {children}
      {hint && <span className="mt-1 block text-sm text-muted">{hint}</span>}
    </label>
  )
}
