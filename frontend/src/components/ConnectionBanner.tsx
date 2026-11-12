import { useEffect, useState } from 'react'
import { useOnlineStatus } from '../hooks/useOnlineStatus'

const RECONNECTED_MS = 4000

/** A bar at the top of every screen while there is no connection, and a short "back online" after. */
export function ConnectionBanner() {
  const online = useOnlineStatus()
  const [justReconnected, setJustReconnected] = useState(false)

  useEffect(() => {
    let timer: number | undefined

    function handleOnline() {
      setJustReconnected(true)
      window.clearTimeout(timer)
      timer = window.setTimeout(() => setJustReconnected(false), RECONNECTED_MS)
    }

    window.addEventListener('online', handleOnline)
    return () => {
      window.removeEventListener('online', handleOnline)
      window.clearTimeout(timer)
    }
  }, [])

  if (!online) {
    return (
      <div role="status" className="sticky top-0 z-50 bg-loss px-4 py-2 pt-[max(0.5rem,env(safe-area-inset-top))] text-center text-sm text-white">
        Sin conexión a internet. Lo que ves puede no estar actualizado.
      </div>
    )
  }

  if (justReconnected) {
    return (
      <div role="status" className="sticky top-0 z-50 bg-gain px-4 py-2 pt-[max(0.5rem,env(safe-area-inset-top))] text-center text-sm text-white">
        Conexión recuperada.
      </div>
    )
  }

  return null
}
