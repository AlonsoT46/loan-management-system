import { useState } from 'react'
import type { ReactNode } from 'react'
import { api } from '../api/client'
import { ChevronLeftIcon, ChevronRightIcon } from '../components/Icons'
import { StackedBarChart } from '../components/StackedBarChart'
import { InfoRow, LoadError, Message, ScreenTitle } from '../components/Ui'
import { useLoad, useLoadFor } from '../hooks/useLoad'
import { currentMonth, formatMoney, formatMonth, formatMonthShort, shiftMonth } from '../utils/format'

const CHART_MONTHS = 6

/** /estadistica — charts of the last months, the situation today, and the detail of one month. */
export function StatisticsPage() {
  const thisMonth = currentMonth()
  const [month, setMonth] = useState(thisMonth)

  return (
    <div>
      <ScreenTitle title="Estadística" />

      <div className="space-y-5 px-5">
        <MonthlyCharts />

        <SectionHeading title="Hoy" subtitle="Lo que te deben ahora mismo" />
        <TodayStats />

        <SectionHeading title="Detalle por mes" subtitle="Lo que prestaste y lo que cobraste en el mes." />
        <div className="flex items-center justify-between rounded-2xl border border-ink px-2 py-2">
          <button type="button" aria-label="Mes anterior" onClick={() => setMonth((m) => shiftMonth(m, -1))} className="rounded-full p-2">
            <ChevronLeftIcon />
          </button>
          <span className="text-lg font-medium">{formatMonth(month)}</span>
          <button
            type="button"
            aria-label="Mes siguiente"
            disabled={month >= thisMonth}
            onClick={() => setMonth((m) => shiftMonth(m, 1))}
            className="rounded-full p-2 disabled:opacity-30"
          >
            <ChevronRightIcon />
          </button>
        </div>

        {/* No key here: the same cards stay on screen and update, so the page does not jump. */}
        <MonthStats month={month} />
      </div>
    </div>
  )
}

/** A line plus a title: separates parts of the page that answer different questions. */
function SectionHeading({ title, subtitle }: { title: string; subtitle: string }) {
  return (
    <div className="border-t border-line pt-6">
      <h2 className="text-xl font-semibold">{title}</h2>
      <p className="mt-1 text-sm text-muted">{subtitle}</p>
    </div>
  )
}

/** Today's situation. Loads once; changing the month does not touch it. */
function TodayStats() {
  const { data: s, error, loading, retry } = useLoad(() => api.getSummary(currentMonth()))

  if (loading)
    return <Message>Cargando...</Message>
  if (error || !s)
    return <LoadError message={error ?? 'Error'} onRetry={retry} />

  return (
    <section className="divide-y divide-line rounded-2xl border border-ink px-4 py-1">
      <InfoRow label="Por cobrar" value={formatMoney(s.outstandingBalance)} strong />
      <InfoRow label="Vencido" value={formatMoney(s.overdueBalance)} className={s.overdueBalance > 0 ? 'text-loss' : ''} />
      <InfoRow label="Préstamos activos" value={s.activeLoans} />
      <InfoRow label="Préstamos vencidos" value={s.overdueLoans} />
    </section>
  )
}

function MonthStats({ month }: { month: string }) {
  // Keeps the previous month on screen (dimmed) while the new one arrives.
  const { data: s, error, loading, retry } = useLoadFor(month, (m) => api.getSummary(m))

  if (error)
    return <LoadError message={error} onRetry={retry} />
  if (!s)
    return <Message>Cargando...</Message>

  const principalShare = s.expectedTotal > 0 ? (s.totalLent / s.expectedTotal) * 100 : 0

  return (
    <div className={'space-y-5 transition-opacity ' + (loading ? 'opacity-50' : '')} aria-busy={loading}>
      <StatCard title="Préstamos hechos este mes">
        {s.totalLent === 0 ? (
          <p className="py-3 text-muted">No hubo préstamos este mes.</p>
        ) : (
          <>
            <InfoRow label="Prestado" value={formatMoney(s.totalLent)} />
            <InfoRow label="Interés pactado" value={formatMoney(s.expectedInterest)} className="text-gain" />
            <InfoRow label="Total a recibir" value={formatMoney(s.expectedTotal)} strong />
            <div className="py-3">
              <div className="flex h-3 gap-[2px] overflow-hidden rounded-full">
                <div className="bg-chart-1" style={{ width: `${principalShare}%` }} />
                <div className="flex-1 bg-chart-2" />
              </div>
              <div className="mt-2 flex justify-between text-xs text-muted">
                <span className="inline-flex items-center gap-1.5">
                  <span className="size-2 rounded-sm bg-chart-1" />
                  Capital
                </span>
                <span className="inline-flex items-center gap-1.5">
                  <span className="size-2 rounded-sm bg-chart-2" />
                  Interés
                </span>
              </div>
            </div>
          </>
        )}
      </StatCard>

      <StatCard title="Cobrado este mes">
        <InfoRow label="Total cobrado" value={formatMoney(s.totalCollected)} strong />
        <InfoRow label="Capital recuperado" value={formatMoney(s.principalRecovered)} />
        <InfoRow label="Interés cobrado" value={formatMoney(s.interestCollected)} className="text-gain" />
      </StatCard>

      <p className="pb-2 text-sm text-muted">
        <strong className="font-medium text-ink">Prestado</strong> Es todo el dinero que realmente ha prestado{' '}
              <strong className="font-medium text-ink">Interés</strong> es la ganancia que espera recibir de esos préstamos. <strong className="font-medium text-ink">Cobrado</strong> es lo que le han pagado este mes, capital más interés.
      </p>
    </div>
  )
}

type ChartTab = 'lent' | 'income'

const chartTabs: { value: ChartTab; label: string }[] = [
  { value: 'lent', label: 'Dinero prestado' },
  { value: 'income', label: 'Dinero que entró' },
]

/** The charts of the last months, one at a time: tap a tab to see the other one. */
function MonthlyCharts() {
  const [tab, setTab] = useState<ChartTab>('lent')
  const { data: months, error, loading, retry } = useLoad(() => api.getMonthly(CHART_MONTHS))

  return (
    <section className="space-y-3">
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

      {loading && <Message>Cargando gráficas...</Message>}
      {!loading && (error || !months) && <LoadError message={error ?? 'Error'} onRetry={retry} />}

      {!loading && months && tab === 'lent' && (
        <StackedBarChart
          title={`Dinero prestado · últimos ${CHART_MONTHS} meses`}
          series={[
            { key: 'back', label: 'Dinero recuperado', colorClass: 'bg-chart-1' },
            { key: 'out', label: 'Dinero pendiente', colorClass: 'bg-chart-2' },
            { key: 'lost', label: 'Dinero perdido', colorClass: 'bg-chart-3' },
          ]}
          points={months.map((m) => ({
            label: formatMonthShort(m.month),
            fullLabel: formatMonth(m.month),
            values: {
              back: m.lent - m.principalStillOut - m.principalWrittenOff,
              out: m.principalStillOut,
              lost: m.principalWrittenOff,
            },
          }))}
          formatValue={formatMoney}
          emptyText={`No hubo préstamos en los últimos ${CHART_MONTHS} meses.`}
          note="Cada barra es lo que prestó ese mes: lo que ya volvió, lo que todavía no le han devuelto y lo que dio por perdido (solo capital). Toca una barra para ver sus números."
        />
      )}

      {!loading && months && tab === 'income' && (
        <StackedBarChart
          title={`Dinero que entró · últimos ${CHART_MONTHS} meses`}
          series={[
            { key: 'principal', label: 'Capital', colorClass: 'bg-chart-1' },
            { key: 'interest', label: 'Interés (ganancia)', colorClass: 'bg-chart-2' },
          ]}
          points={months.map((m) => ({
            label: formatMonthShort(m.month),
            fullLabel: formatMonth(m.month),
            values: { principal: m.principalCollected, interest: m.interestCollected },
          }))}
          formatValue={formatMoney}
          emptyText={`No entraron pagos en los últimos ${CHART_MONTHS} meses.`}
          note="Cada barra es lo que le pagaron ese mes: lo que vuelve de lo prestado más la ganancia. Toca una barra para ver sus números."
        />
      )}
    </section>
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
