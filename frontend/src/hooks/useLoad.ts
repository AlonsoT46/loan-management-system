import { useCallback, useEffect, useRef, useState } from 'react'

export interface LoadState<T> {
  data: T | null
  error: string | null
  loading: boolean
  retry: () => void
  /** Replace the data after a change (for example, the loan the API returns after a payment). */
  setData: (data: T) => void
}

/**
 * Loads data when a screen opens, with "loading", "error" and "retry" ready to use.
 * To load something else (another id, another month), give the component a new `key`:
 * React builds it again from zero and this runs again.
 */
export function useLoad<T>(load: () => Promise<T>): LoadState<T> {
  const [data, setData] = useState<T | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [attempt, setAttempt] = useState(0)

  // Always call the latest "load" without restarting the request on every render.
  const loadRef = useRef(load)
  useEffect(() => {
    loadRef.current = load
  })

  useEffect(() => {
    let ignore = false // if the screen closes before the answer arrives, the answer is ignored

    loadRef.current()
      .then((result) => {
        if (!ignore) {
          setData(result)
          setError(null)
        }
      })
      .catch((e: unknown) => {
        if (!ignore)
          setError(e instanceof Error ? e.message : 'No se pudo cargar la información.')
      })
      .finally(() => {
        if (!ignore)
          setLoading(false)
      })

    return () => {
      ignore = true
    }
  }, [attempt])

  const retry = useCallback(() => {
    setLoading(true)
    setAttempt((value) => value + 1)
  }, [])

  return { data, error, loading, retry, setData }
}

export interface LoadForState<T> {
  /** The last data that arrived. While another key loads, it is still the previous one. */
  data: T | null
  error: string | null
  /** True while the data for the current key has not arrived yet. */
  loading: boolean
  retry: () => void
}

/**
 * Like useLoad, but for something that changes while the screen stays open (the month of the
 * statistics). It keeps showing the previous data while the new one loads, instead of replacing
 * everything with "Cargando...": the page keeps its height, so it does not jump while scrolling.
 */
export function useLoadFor<T>(key: string, load: (key: string) => Promise<T>): LoadForState<T> {
  const [result, setResult] = useState<{ key: string; data: T } | null>(null)
  const [failure, setFailure] = useState<{ key: string; message: string } | null>(null)
  const [attempt, setAttempt] = useState(0)

  const loadRef = useRef(load)
  useEffect(() => {
    loadRef.current = load
  })

  useEffect(() => {
    let ignore = false

    loadRef.current(key)
      .then((data) => {
        if (!ignore) {
          setResult({ key, data })
          setFailure(null)
        }
      })
      .catch((e: unknown) => {
        if (!ignore)
          setFailure({ key, message: e instanceof Error ? e.message : 'No se pudo cargar la información.' })
      })

    return () => {
      ignore = true
    }
  }, [key, attempt])

  const retry = useCallback(() => {
    setFailure(null)
    setAttempt((value) => value + 1)
  }, [])

  // Derived, not stored: "loading" is simply "what we have is not for this key yet".
  const error = failure?.key === key ? failure.message : null
  const loading = result?.key !== key && error === null

  return { data: result?.data ?? null, error, loading, retry }
}
