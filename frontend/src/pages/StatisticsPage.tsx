import { useState } from 'react'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { api } from '../api/client'
import type { MonthlyPoint, Summary } from '../api/types'
import { ChevronLeftIcon, ChevronRightIcon } from '../components/Icons'
import { StackedBarChart } from '../components/StackedBarChart'
import { InfoRow, LoadError, Message, ScreenTitle } from '../components/Ui'
import { useLoad, useLoadFor } from '../hooks/useLoad'
import { formatMoney, formatMonth, formatMonthShort } from '../utils/format'

/** How far back the month selector can go (the most the backend gives). */
const HISTORY_MONTHS = 24
/** How many columns the chart shows at once. */
const CHART_MONTHS = 6

type ChartTab = 'lent' | 'income'

const chartTabs: { value: ChartTab; label: string }[] = [
  { value: 'lent', label: 'Dinero prestado' },
  { value: 'income', label: 'Dinero que entró' },
]

/**
 * /estadistica — one month drives the whole screen: the arrows and the chart bars both choose it,
 * and the four numbers, the chart highlight and the detail all follow it.
 */
export function StatisticsPage() {
  // The last 24 months, oldest first. Loaded once; moving between months does not ask again.
  const { data: history, error, loading, retry } = useLoad(() => api.getMonthly(HISTORY_MONTHS))

  return (
    <div>
      <ScreenTitle title="Estadística" />
      <div className="px-5">
        {loading && <Message>Cargando...</Message>}
        {!loading && (error || !history) && <LoadError message={error ?? 'Error'} onRetry={retry} />}
        {!loading && history && <MonthView history={history} />}
      </div>
    </div>
  )
}

function MonthView({ history }: { history: MonthlyPoint[] }) {
  const last = history.length - 1
  const [selectedIndex, setSelectedIndex] = useState(last)
  const [tab, setTab] = useState<ChartTab>('lent')

  // Do not offer empty months from before the first loan or payment.
  const firstActive = Math.max(
    0,
    history.findIndex((m) => m.lent > 0 || m.principalCollected > 0 || m.interestCollected > 0),
  )
  const earliest = Math.min(firstActive, last)

  // The chart shows up to 6 months: normally the last ones; if the chosen month is older, it moves.
  let start = Math.max(earliest, last - (CHART_MONTHS - 1))
  let end = last
  if (selectedIndex < start) {
    start = selectedIndex
    end = Math.min(last, selectedIndex + CHART_MONTHS - 1)
  }
  const visibleMonths = history.slice(start, end + 1)

  const month = history[selectedIndex]
  const collected = month.principalCollected + month.interestCollected

  return (
    <div className="space-y-5">
      {/* One selector for the whole screen. */}
      <div className="flex items-center justify-between rounded-2xl border border-ink px-2 py-2">
        <button
          type="button"
          aria-label="Mes anterior"
          disabled={selectedIndex <= earliest}
          onClick={() => setSelectedIndex((i) => i - 1)}
          className="rounded-full p-2 disabled:opacity-30"
        >
          <ChevronLeftIcon />
        </button>
        <span className="text-lg font-medium">{formatMonth(month.month)}</span>
        <button
          type="button"
          aria-label="Mes siguiente"
          disabled={selectedIndex >= last}
          onClick={() => setSelectedIndex((i) => i + 1)}
          className="rounded-full p-2 disabled:opacity-30"
        >
          <ChevronRightIcon />
        </button>
      </div>

      {/* The four numbers of the chosen month. */}
      <section className="grid grid-cols-2 gap-3">
        <Kpi label="Prestado" value={formatMoney(month.lent)} />
        <Kpi label="Cobrado" value={formatMoney(collected)} />
        <Kpi label="Ganancia" value={formatMoney(month.interestCollected)} className="text-gain" />
        <Kpi label="Pendiente" value={formatMoney(month.principalStillOut)} />
      </section>

      <div className="grid grid-cols-2 gap-2" role="tablist" aria-label="Gráficas">
        {chartTabs.map((t) => (
          <button
            key={t.value}
            type="button"
            role="tab"
            aria-selected={tab === t.value}
            onClick={() => setTab(t.value)}
            className={
              'rounded-xl border border-ink py-2.5 text-sm font-medium ' +
              (tab === t.value ? 'bg-ink text-surface' : 'bg-surface text-ink')
            }
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'lent' && (
        <StackedBarChart
          title="Dinero prestado"
          showTitle={false}
          series={[
            { key: 'back', label: 'Dinero recuperado', colorClass: 'bg-chart-1' },
            { key: 'out', label: 'Dinero pendiente', colorClass: 'bg-chart-2' },
            { key: 'lost', label: 'Dinero perdido', colorClass: 'bg-chart-3' },
          ]}
          points={visibleMonths.map((m) => ({
            label: formatMonthShort(m.month),
            fullLabel: formatMonth(m.month),
            values: {
              back: m.lent - m.principalStillOut - m.principalWrittenOff,
              out: m.principalStillOut,
              lost: m.principalWrittenOff,
            },
          }))}
          formatValue={formatMoney}
          emptyText="Todavía no hay préstamos."
          note="Cada barra es lo que prestó ese mes: lo que ya volvió, lo que todavía no le han devuelto y lo que dio por perdido (solo capital). Toca una barra para elegir ese mes."
          selectedIndex={selectedIndex - start}
          onSelect={(i) => setSelectedIndex(start + i)}
        />
      )}

      {tab === 'income' && (
        <StackedBarChart
          title="Dinero que entró"
          showTitle={false}
          series={[
            { key: 'principal', label: 'Capital', colorClass: 'bg-chart-1' },
            { key: 'interest', label: 'Interés (ganancia)', colorClass: 'bg-chart-2' },
          ]}
          points={visibleMonths.map((m) => ({
            label: formatMonthShort(m.month),
            fullLabel: formatMonth(m.month),
            values: { principal: m.principalCollected, interest: m.interestCollected },
          }))}
          formatValue={formatMoney}
          emptyText="Todavía no hay pagos."
          note="Cada barra es lo que le pagaron ese mes: lo que vuelve de lo prestado más la ganancia. Toca una barra para elegir ese mes."
          selectedIndex={selectedIndex - start}
          onSelect={(i) => setSelectedIndex(start + i)}
        />
      )}

      <MonthDetail month={month.month} />
    </div>
  )
}

function Kpi({ label, value, className = '' }: { label: string; value: string; className?: string }) {
  return (
    <div className="rounded-2xl border border-ink p-3">
      <p className="text-xs text-muted">{label}</p>
      <p className={'mt-1 text-xl font-semibold tracking-tight ' + className}>{value}</p>
    </div>
  )
}

/** The fine print of the month, folded away until it is needed. Also today's total, in one line. */
function MonthDetail({ month }: { month: string }) {
  // Keeps the previous month on screen (dimmed) while the new one arrives, so the page does not jump.
  const { data: s, error, loading, retry } = useLoadFor(month, (m) => api.getSummary(m))

  if (error)
    return <LoadError message={error} onRetry={retry} />
  if (!s)
    return <Message>Cargando...</Message>

  return (
    <div className={'space-y-4 pb-2 transition-opacity ' + (loading ? 'opacity-50' : '')} aria-busy={loading}>
      <details className="group rounded-2xl border border-ink px-4 py-3">
        <summary className="flex cursor-pointer list-none items-center justify-between font-medium [&::-webkit-details-marker]:hidden">
          Ver detalle del mes
          <ChevronRightIcon className="size-5 transition-transform group-open:rotate-90" />
        </summary>
        <MonthDetailRows s={s} />
      </details>

      <p className="text-sm text-muted">
        Lo que te deben ahora mismo: <span className="font-medium text-ink">{formatMoney(s.outstandingBalance)}</span>
        {' · '}
        <Link to="/inicio" className="underline">
          Ver en Inicio
        </Link>
      </p>
    </div>
  )
}

function MonthDetailRows({ s }: { s: Summary }) {
  return (
    <div className="mt-3 space-y-4">
      <StatCard title="Préstamos hechos este mes">
        {s.totalLent === 0 ? (
          <p className="py-3 text-muted">No hubo préstamos este mes.</p>
        ) : (
          <>
            <InfoRow label="Prestado" value={formatMoney(s.totalLent)} />
            <InfoRow label="Interés pactado" value={formatMoney(s.expectedInterest)} className="text-gain" />
            <InfoRow label="Total a recibir" value={formatMoney(s.expectedTotal)} strong />
          </>
        )}
      </StatCard>

      <StatCard title="Cobrado este mes">
        <InfoRow label="Total cobrado" value={formatMoney(s.totalCollected)} strong />
        <InfoRow label="Capital recuperado" value={formatMoney(s.principalRecovered)} />
        <InfoRow label="Interés cobrado" value={formatMoney(s.interestCollected)} className="text-gain" />
      </StatCard>

      <p className="text-sm text-muted">
        <strong className="font-medium text-ink">Prestado</strong> Es todo el dinero que realmente ha prestado{' '}
        <strong className="font-medium text-ink">Interés</strong> es la ganancia que espera recibir de esos préstamos.{' '}
        <strong className="font-medium text-ink">Cobrado</strong> es lo que le han pagado este mes, capital más interés.
      </p>
    </div>
  )
}

function StatCard({ title, note, children }: { title: string; note?: string; children: ReactNode }) {
  return (
    <section className="rounded-2xl border border-ink px-4 pt-3 pb-1">
      <h2 className="text-lg font-medium">{title}</h2>
      {note && <p className="text-xs text-muted">{note}</p>}
      <div className="mt-1 divide-y divide-line">{children}</div>
    </section>
  )
}
