import { useSyncExternalStore } from 'react'

// The browser fires "online" / "offline" when the phone gains or loses its network.
function subscribe(onChange: () => void) {
  window.addEventListener('online', onChange)
  window.addEventListener('offline', onChange)
  return () => {
    window.removeEventListener('online', onChange)
    window.removeEventListener('offline', onChange)
  }
}

/**
 * True while the device has a network. Careful: "online" means connected to a wifi or mobile
 * network, not that internet really works; slow or dead connections are caught by the request
 * timeout in api/client.ts.
 */
export function useOnlineStatus(): boolean {
  return useSyncExternalStore(subscribe, () => navigator.onLine, () => true)
}
