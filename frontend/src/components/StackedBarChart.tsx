import { useState } from 'react'

export interface ChartSeries {
  key: string
  label: string
  /** Background class of the series color, e.g. "bg-chart-1". */
  colorClass: string
}

export interface ChartPoint {
  /** Short label under the bar ("nov"). */
  label: string
  /** Full label in the readout ("Noviembre 2026"). */
  fullLabel: string
  values: Record<string, number>
}

/**
 * Columns stacked from the bottom (first series at the bottom).
 * - On its own, tapping or hovering a column shows its numbers in a readout above it.
 * - With selectedIndex + onSelect, the screen decides which column is selected (for example, the
 *   month chosen at the top); then there is no readout, because the screen shows the numbers.
 */
export function StackedBarChart(props: {
  title: string
  showTitle?: boolean
  series: ChartSeries[]
  points: ChartPoint[]
  formatValue: (value: number) => string
  emptyText: string
  note?: string
  selectedIndex?: number
  onSelect?: (index: number) => void
}) {
  const { series, points, formatValue } = props
  const [ownSelected, setOwnSelected] = useState(points.length - 1)
  const [showNote, setShowNote] = useState(false)

  const controlled = props.onSelect !== undefined
  const selected = props.selectedIndex ?? ownSelected
  const select = (index: number) => (props.onSelect ? props.onSelect(index) : setOwnSelected(index))

  const totals = points.map((point) => series.reduce((sum, s) => sum + Math.max(0, point.values[s.key] ?? 0), 0))
  const max = Math.max(0, ...totals)
  const current = points[selected]

  return (
    <section className="rounded-2xl border border-ink px-4 pt-3 pb-4">
      {props.showTitle !== false && <h2 className="text-lg font-medium">{props.title}</h2>}

      {/* Legend: color is never the only clue, every series has its name. */}
      <div className="mt-1 flex items-start justify-between gap-2">
        <div className="flex flex-wrap gap-x-4 gap-y-1 text-sm text-muted">
          {series.map((s) => (
            <span key={s.key} className="inline-flex items-center gap-1.5">
              <span className={'size-2.5 rounded-sm ' + s.colorClass} />
              {s.label}
            </span>
          ))}
        </div>
        {props.note && (
          <button
            type="button"
            aria-label="Qué muestra esta gráfica"
            aria-expanded={showNote}
            onClick={() => setShowNote((value) => !value)}
            className="flex size-6 shrink-0 items-center justify-center rounded-full border border-muted text-xs text-muted"
          >
            i
          </button>
        )}
      </div>
      {props.note && showNote && <p className="mt-2 rounded-xl bg-line/50 px-3 py-2 text-xs text-muted">{props.note}</p>}

      {max === 0 ? (
        <p className="py-8 text-center text-muted">{props.emptyText}</p>
      ) : (
        <>
          {current && !controlled && (
            <div className="mt-3 rounded-xl bg-line/50 px-3 py-2 text-sm" aria-live="polite">
              <p className="flex justify-between font-medium">
                <span>{current.fullLabel}</span>
                <span>{formatValue(totals[selected])}</span>
              </p>
              {series.map((s) => (
                <p key={s.key} className="flex justify-between text-muted">
                  <span className="inline-flex items-center gap-1.5">
                    <span className={'size-2 rounded-sm ' + s.colorClass} />
                    {s.label}
                  </span>
                  <span className="text-ink">{formatValue(current.values[s.key] ?? 0)}</span>
                </p>
              ))}
            </div>
          )}

          <div className="mt-4 flex h-40 items-end gap-1 border-b border-line" aria-hidden="true">
            {points.map((point, index) => {
              const share = (totals[index] / max) * 100
              const height = share > 0 ? Math.max(share, 2) : 0
              const visible = series.filter((s) => (point.values[s.key] ?? 0) > 0)
              return (
                <button
                  key={point.label + index}
                  type="button"
                  tabIndex={-1}
                  onClick={() => select(index)}
                  onMouseEnter={controlled ? undefined : () => select(index)}
                  className="flex h-full flex-1 flex-col items-center justify-end"
                >
                  <div
                    className={'flex w-6 flex-col-reverse gap-[2px] ' + (index === selected ? '' : 'opacity-50')}
                    style={{ height: `${height}%` }}
                  >
                    {visible.map((s, i) => (
                      <div
                        key={s.key}
                        className={s.colorClass + (i === visible.length - 1 ? ' rounded-t-[4px]' : '')}
                        style={{ flexGrow: point.values[s.key] }}
                      />
                    ))}
                  </div>
                </button>
              )
            })}
          </div>

          <div className="mt-1 flex gap-1" aria-hidden="true">
            {points.map((point, index) => (
              <span
                key={point.label + index}
                className={'flex-1 text-center text-xs ' + (index === selected ? 'font-medium text-ink' : 'text-muted')}
              >
                {point.label}
              </span>
            ))}
          </div>
        </>
      )}


      {/* The same numbers as a table, for screen readers. The wrapper hides it entirely: with "sr-only"
          on the table itself, the browser still draws the caption outside the table. */}
      <div className="sr-only">
        <table>
        <caption>{props.title}</caption>
        <thead>
          <tr>
            <th>Mes</th>
            {series.map((s) => (
              <th key={s.key}>{s.label}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {points.map((point, index) => (
            <tr key={point.label + index}>
              <td>{point.fullLabel}</td>
              {series.map((s) => (
                <td key={s.key}>{formatValue(point.values[s.key] ?? 0)}</td>
              ))}
            </tr>
          ))}
        </tbody>
        </table>
      </div>
    </section>
  )
}
